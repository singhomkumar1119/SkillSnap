using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProfileController(ApplicationDbContext db)
    {
        _db = db;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? string.Empty;

    // Public: anyone viewing the portfolio can see the profile.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var profile = await _db.Profiles.FirstOrDefaultAsync();
        if (profile is null) return NotFound();

        return Ok(new ProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            Bio = profile.Bio,
            PhotoUrl = profile.PhotoUrl,
        });
    }

    // Only the signed-in owner can edit their own profile.
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update(ProfileDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.OwnerId == CurrentUserId);
        if (profile is null) return NotFound();

        profile.Name = dto.Name;
        profile.Bio = dto.Bio;
        profile.PhotoUrl = dto.PhotoUrl;

        await _db.SaveChangesAsync();
        return Ok(dto);
    }
}
