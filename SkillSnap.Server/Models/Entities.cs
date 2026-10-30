using Microsoft.AspNetCore.Identity;

namespace SkillSnap.Server.Models;

// Extends IdentityUser so we get all of ASP.NET Identity's built-in
// password hashing, lockout, and token support for free, while still
// letting us add app-specific fields later if needed.
public class ApplicationUser : IdentityUser
{
}

public class Profile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    // Ties a profile to the Identity user who owns it, so editing
    // is restricted to the account that created it.
    public string OwnerId { get; set; } = string.Empty;
}

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TechStack { get; set; } = string.Empty;
    public string? ProjectUrl { get; set; }
    public string OwnerId { get; set; } = string.Empty;
}

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Proficiency { get; set; }
    public string OwnerId { get; set; } = string.Empty;
}
