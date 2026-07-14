using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Server.Models;

namespace SkillSnap.Server.Data;

// Inheriting from IdentityDbContext gives us the AspNetUsers/Roles/Claims
// tables automatically, alongside our own Profiles/Projects/Skills tables,
// all in one SQLite database file for simplicity.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Indexing OwnerId speeds up the "give me this user's projects/skills"
        // queries that run on almost every page load.
        builder.Entity<Project>().HasIndex(p => p.OwnerId);
        builder.Entity<Skill>().HasIndex(s => s.OwnerId);
        builder.Entity<Profile>().HasIndex(p => p.OwnerId);
    }
}
