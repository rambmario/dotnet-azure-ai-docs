using AzureAiDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzureAiDocs.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ConsultationLog> ConsultationLogs => Set<ConsultationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(260).IsRequired();
            entity.Property(e => e.BlobUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.UploadedAt);
            entity.HasMany(e => e.Logs)
                .WithOne(l => l.Document)
                .HasForeignKey(l => l.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsultationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Question).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Answer).IsRequired();
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.AskedAt);
        });
    }
}
