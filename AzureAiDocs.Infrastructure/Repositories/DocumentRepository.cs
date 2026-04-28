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

    public async Task<Document> AddAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id) =>
        await _context.Documents.FindAsync(id);

    public async Task<IEnumerable<ConsultationLog>> GetLogsAsync(Guid documentId) =>
        await _context.ConsultationLogs
            .Where(l => l.DocumentId == documentId)
            .OrderByDescending(l => l.AskedAt)
            .ToListAsync();

    public async Task AddLogAsync(ConsultationLog log)
    {
        _context.ConsultationLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}