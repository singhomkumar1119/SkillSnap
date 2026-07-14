using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Server.Data;
using SkillSnap.Server.Models;
using SkillSnap.Shared.Models;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "skills:all";

    public SkillsController(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? string.Empty;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SkillDto>>> GetAll()
    {
        if (_cache.TryGetValue(CacheKey, out List<SkillDto>? cached) && cached is not null)
        {
            return Ok(cached);
        }

        var skills = await _db.Skills
            .OrderByDescending(s => s.Proficiency)
            .Select(s => ToDto(s))
            .ToListAsync();

        _cache.Set(CacheKey, skills, TimeSpan.FromSeconds(60));
        return Ok(skills);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<SkillDto>> Create(SkillDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var skill = new Skill { Name = dto.Name, Proficiency = dto.Proficiency, OwnerId = CurrentUserId };
        _db.Skills.Add(skill);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);

        return CreatedAtAction(nameof(GetAll), ToDto(skill));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, SkillDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var skill = await _db.Skills.FindAsync(id);
        if (skill is null) return NotFound();
        if (skill.OwnerId != CurrentUserId) return Forbid();

        skill.Name = dto.Name;
        skill.Proficiency = dto.Proficiency;
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);

        return Ok(ToDto(skill));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var skill = await _db.Skills.FindAsync(id);
        if (skill is null) return NotFound();
        if (skill.OwnerId != CurrentUserId) return Forbid();

        _db.Skills.Remove(skill);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);

        return NoContent();
    }

    private static SkillDto ToDto(Skill s) => new() { Id = s.Id, Name = s.Name, Proficiency = s.Proficiency };
}
