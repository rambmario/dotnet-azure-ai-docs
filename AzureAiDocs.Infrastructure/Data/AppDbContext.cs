using AzureAiDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzureAiDocs.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ConsultationLog> ConsultationLogs => Set<ConsultationLog>();
}
