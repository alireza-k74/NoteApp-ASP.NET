using Microsoft.EntityFrameworkCore;
using NoteApp.Api.Models;

namespace NoteApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .Property(x => x.Name)
            .HasMaxLength(50);

        modelBuilder.Entity<Tag>()
            .HasIndex(x => new { x.UserId, x.Name })
            .IsUnique();

        modelBuilder.Entity<Note>()
            .Property(x => x.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Notes)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Tags)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // SQL Server rejects multiple cascade paths (User→Notes→NoteTags and User→Tags→NoteTags).
        // Cascade from Notes; Restrict from Tags (DeleteTagAsync clears join rows first).
        modelBuilder.Entity<Note>()
            .HasMany(x => x.Tags)
            .WithMany(x => x.Notes)
            .UsingEntity<Dictionary<string, object>>(
                "NoteTag",
                l => l.HasOne<Tag>().WithMany().HasForeignKey("TagsId")
                    .OnDelete(DeleteBehavior.Restrict),
                r => r.HasOne<Note>().WithMany().HasForeignKey("NotesId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.ToTable("NoteTags"));
    }
}
