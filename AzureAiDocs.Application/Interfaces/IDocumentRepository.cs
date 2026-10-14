using AzureAiDocs.Domain.Entities;

namespace AzureAiDocs.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationLog>> GetLogsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task AddLogAsync(ConsultationLog log, CancellationToken cancellationToken = default);
}
