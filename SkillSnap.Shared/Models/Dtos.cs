using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Shared.Models;

// Sharing these DTOs between the Blazor client and the API keeps both projects
// talking about the exact same shape of data, so a change on one side
// immediately shows up as a compile error on the other instead of a runtime bug.

public class ProfileDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bio is required")]
    [StringLength(500, ErrorMessage = "Bio must be 500 characters or fewer")]
    public string Bio { get; set; } = string.Empty;

    [Url(ErrorMessage = "Photo URL must be a valid URL")]
    public string? PhotoUrl { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tech stack is required")]
    public string TechStack { get; set; } = string.Empty;

    [Url(ErrorMessage = "Project URL must be a valid URL")]
    public string? ProjectUrl { get; set; }
}

public class SkillDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Skill name is required")]
    [StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Proficiency must be between 1 and 5")]
    public int Proficiency { get; set; } = 1;
}

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Email { get; set; } = string.Empty;
}
