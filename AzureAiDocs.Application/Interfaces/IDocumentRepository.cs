using AzureAiDocs.Domain.Entities;

namespace AzureAiDocs.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document);
    Task<Document?> GetByIdAsync(Guid id);
    Task<IEnumerable<ConsultationLog>> GetLogsAsync(Guid documentId);
    Task AddLogAsync(ConsultationLog log);
}