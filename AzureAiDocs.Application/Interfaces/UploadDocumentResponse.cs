namespace AzureAiDocs.Application.DTOs;

public record UploadDocumentResponse(Guid DocumentId, string FileName, DateTime UploadedAt);
