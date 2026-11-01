using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CodeQuest.Models;

namespace CodeQuest.Data;

public class GameDbContext : IdentityDbContext<IdentityUser>
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // must call for Identity tables

        // Prevent duplicate lesson completions
        builder.Entity<LessonProgress>()
            .HasIndex(lp => new { lp.PlayerId, lp.LessonId })
            .IsUnique();
    }
}
