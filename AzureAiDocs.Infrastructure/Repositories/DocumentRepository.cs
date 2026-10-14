using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Domain.Entities;
using AzureAiDocs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AzureAiDocs.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Documents.AnyAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ConsultationLog>> GetLogsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.ConsultationLogs
            .Where(l => l.DocumentId == documentId)
            .OrderByDescending(l => l.AskedAt)
            .ToListAsync(cancellationToken);

    public async Task AddLogAsync(ConsultationLog log, CancellationToken cancellationToken = default)
    {
        _context.ConsultationLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
