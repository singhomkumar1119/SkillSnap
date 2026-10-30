using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SkillSnap.Server.Models;

namespace SkillSnap.Server.Data;

public static class DbSeeder
{
    // Runs once on startup. Creates the database file if it doesn't exist,
    // applies pending migrations, and seeds a demo account + sample
    // portfolio data so the app isn't an empty shell the first time it runs.
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        const string demoEmail = "demo@skillsnap.dev";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);

        if (demoUser is null)
        {
            demoUser = new ApplicationUser { UserName = demoEmail, Email = demoEmail, EmailConfirmed = true };
            await userManager.CreateAsync(demoUser, "Demo@Pass123");
        }

        if (!context.Profiles.Any())
        {
            context.Profiles.Add(new Profile
            {
                Name = "Demo Developer",
                Bio = "Full-stack developer who loves building clean, well-tested apps.",
                PhotoUrl = "https://placehold.co/200x200",
                OwnerId = demoUser.Id,
            });
        }

        if (!context.Projects.Any())
        {
            context.Projects.AddRange(
                new Project
                {
                    Title = "SkillSnap Portfolio App",
                    Description = "A full-stack portfolio builder using Blazor WASM and ASP.NET Core.",
                    TechStack = "Blazor WebAssembly, ASP.NET Core, EF Core, SQLite",
                    ProjectUrl = "https://github.com/example/skillsnap",
                    OwnerId = demoUser.Id,
                },
                new Project
                {
                    Title = "User Management API",
                    Description = "A CRUD API for managing users with validation and middleware.",
                    TechStack = "Node.js, Express",
                    ProjectUrl = "https://github.com/example/user-management-api",
                    OwnerId = demoUser.Id,
                }
            );
        }

        if (!context.Skills.Any())
        {
            context.Skills.AddRange(
                new Skill { Name = "C# / .NET", Proficiency = 4, OwnerId = demoUser.Id },
                new Skill { Name = "Blazor", Proficiency = 3, OwnerId = demoUser.Id },
                new Skill { Name = "JavaScript", Proficiency = 4, OwnerId = demoUser.Id }
            );
        }

        await context.SaveChangesAsync();
    }
}
