using AzureAiDocs.Application.DTOs;
using AzureAiDocs.Application.Exceptions;
using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Application.Options;
using AzureAiDocs.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAiDocs.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storage;
    private readonly IAiService _ai;
    private readonly DocumentUploadOptions _uploadOptions;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository repository,
        IStorageService storage,
        IAiService ai,
        IOptions<DocumentUploadOptions> uploadOptions,
        ILogger<DocumentService> logger)
    {
        _repository = repository;
        _storage = storage;
        _ai = ai;
        _uploadOptions = uploadOptions.Value;
        _logger = logger;
    }

    public async Task<UploadDocumentResponse> UploadAsync(
        Stream fileStream,
        string fileName,
        long fileLength,
        CancellationToken cancellationToken = default)
    {
        ValidateUpload(fileName, fileLength);

        _logger.LogInformation("Uploading document {FileName} ({Length} bytes)", fileName, fileLength);

        var (blobUrl, content) = await _storage.UploadAsync(fileStream, fileName, cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException(
                "Could not extract text from the uploaded file. Ensure the file is a valid .txt or .pdf with readable text.");
        }

        var document = new Document
        {
            FileName = fileName,
            BlobUrl = blobUrl,
            Content = content
        };

        var saved = await _repository.AddAsync(document, cancellationToken);

        _logger.LogInformation("Document {DocumentId} stored successfully", saved.Id);

        return new UploadDocumentResponse(saved.Id, saved.FileName, saved.UploadedAt);
    }

    public async Task<AskDocumentResponse> AskAsync(
        Guid documentId,
        AskDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ValidationException("Question is required.");

        var document = await _repository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            throw new NotFoundException($"Document '{documentId}' was not found.");

        _logger.LogInformation("Asking question on document {DocumentId}", documentId);

        var answer = await _ai.AskAsync(document.Content, request.Question.Trim(), cancellationToken);

        var log = new ConsultationLog
        {
            DocumentId = documentId,
            Question = request.Question.Trim(),
            Answer = answer
        };

        await _repository.AddLogAsync(log, cancellationToken);

        return new AskDocumentResponse(log.Question, log.Answer, log.AskedAt);
    }

    public async Task<IReadOnlyList<AskDocumentResponse>> GetHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.ExistsAsync(documentId, cancellationToken))
            throw new NotFoundException($"Document '{documentId}' was not found.");

        var logs = await _repository.GetLogsAsync(documentId, cancellationToken);

        return logs
            .Select(l => new AskDocumentResponse(l.Question, l.Answer, l.AskedAt))
            .ToList();
    }

    private void ValidateUpload(string fileName, long fileLength)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ValidationException("File name is required.");

        if (fileLength <= 0)
            throw new ValidationException("No file provided.");

        if (fileLength > _uploadOptions.MaxFileSizeBytes)
        {
            throw new ValidationException(
                $"File exceeds the maximum allowed size of {_uploadOptions.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) ||
            !_uploadOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            var allowed = string.Join(", ", _uploadOptions.AllowedExtensions);
            throw new ValidationException($"Unsupported file type '{extension}'. Allowed: {allowed}.");
        }
    }
}
