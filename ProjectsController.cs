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
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "projects:all";

    public ProjectsController(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? string.Empty;

    // GET /api/projects - public, cached for 60 seconds.
    // Portfolio pages are read far more often than projects are edited, so
    // caching the list avoids hitting the database on every visitor's page load.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
    {
        if (_cache.TryGetValue(CacheKey, out List<ProjectDto>? cached) && cached is not null)
        {
            return Ok(cached);
        }

        var projects = await _db.Projects
            .OrderByDescending(p => p.Id)
            .Select(p => ToDto(p))
            .ToListAsync();

        _cache.Set(CacheKey, projects, TimeSpan.FromSeconds(60));
        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProjectDto>> GetById(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        return project is null ? NotFound() : Ok(ToDto(project));
    }

    // POST /api/projects - requires a valid JWT (set via [Authorize] below).
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProjectDto>> Create(ProjectDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var project = new Project
        {
            Title = dto.Title,
            Description = dto.Description,
            TechStack = dto.TechStack,
            ProjectUrl = dto.ProjectUrl,
            OwnerId = CurrentUserId,
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey); // invalidate so the next GET reflects this change

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, ToDto(project));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, ProjectDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound();

        // Ownership check: you can only edit your own projects, even though
        // you're authenticated. This is what stops User A from editing User B's data.
        if (project.OwnerId != CurrentUserId) return Forbid();

        project.Title = dto.Title;
        project.Description = dto.Description;
        project.TechStack = dto.TechStack;
        project.ProjectUrl = dto.ProjectUrl;

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);

        return Ok(ToDto(project));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound();
        if (project.OwnerId != CurrentUserId) return Forbid();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);

        return NoContent();
    }

    private static ProjectDto ToDto(Project p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Description = p.Description,
        TechStack = p.TechStack,
        ProjectUrl = p.ProjectUrl,
    };
}
