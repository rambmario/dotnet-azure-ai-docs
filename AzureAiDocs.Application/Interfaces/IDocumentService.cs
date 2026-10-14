using AzureAiDocs.Application.DTOs;

namespace AzureAiDocs.Application.Interfaces;

public interface IDocumentService
{
    Task<UploadDocumentResponse> UploadAsync(
        Stream fileStream,
        string fileName,
        long fileLength,
        CancellationToken cancellationToken = default);

    Task<AskDocumentResponse> AskAsync(
        Guid documentId,
        AskDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AskDocumentResponse>> GetHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
