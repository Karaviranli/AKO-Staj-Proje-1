using DataFlow.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.DataAccess.Context;

/// <summary>
/// EF Core Code-First context. Provider SQLite'tır; PostgreSQL/MSSQL'e geçiş
/// yalnızca Program.cs'teki UseSqlite çağrısının değişmesini gerektirir.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<ProcessedDataset> ProcessedDatasets => Set<ProcessedDataset>();
    public DbSet<RulePreset> RulePresets => Set<RulePreset>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.Email).HasMaxLength(160).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32);
        });

        modelBuilder.Entity<UploadedFile>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.SourceType).HasMaxLength(16);
            e.HasIndex(x => x.UploadedAt);

            e.HasOne(x => x.User)
             .WithMany(u => u.UploadedFiles)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedDataset>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => x.ProcessedAt);

            e.HasOne(x => x.UploadedFile)
             .WithMany(f => f.ProcessedDatasets)
             .HasForeignKey(x => x.UploadedFileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RulePreset>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Category).HasMaxLength(32);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(32);
            e.Property(x => x.Username).HasMaxLength(64);
            e.HasIndex(x => x.CreatedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}
