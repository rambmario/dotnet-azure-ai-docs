using AzureAiDocs.Application.DTOs;
using AzureAiDocs.Application.Exceptions;
using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Application.Options;
using AzureAiDocs.Application.Services;
using AzureAiDocs.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureAiDocs.Tests.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _repository = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<IAiService> _ai = new();
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        var uploadOptions = Options.Create(new DocumentUploadOptions
        {
            MaxFileSizeBytes = 5 * 1024 * 1024,
            AllowedExtensions = [".txt", ".pdf"]
        });

        _sut = new DocumentService(
            _repository.Object,
            _storage.Object,
            _ai.Object,
            uploadOptions,
            NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_ValidTxt_PersistsDocument()
    {
        await using var stream = new MemoryStream("hello"u8.ToArray());
        _storage
            .Setup(s => s.UploadAsync(stream, "notes.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://blob/notes.txt", "hello"));

        var saved = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "notes.txt",
            BlobUrl = "https://blob/notes.txt",
            Content = "hello",
            UploadedAt = DateTime.UtcNow
        };

        _repository
            .Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var result = await _sut.UploadAsync(stream, "notes.txt", stream.Length);

        Assert.Equal(saved.Id, result.DocumentId);
        Assert.Equal("notes.txt", result.FileName);
        _repository.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_UnsupportedExtension_ThrowsValidationException()
    {
        await using var stream = new MemoryStream("data"u8.ToArray());

        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UploadAsync(stream, "notes.docx", stream.Length));
    }

    [Fact]
    public async Task UploadAsync_EmptyFile_ThrowsValidationException()
    {
        await using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UploadAsync(stream, "notes.txt", 0));
    }

    [Fact]
    public async Task AskAsync_MissingDocument_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.AskAsync(id, new AskDocumentRequest("Hello?")));
    }

    [Fact]
    public async Task AskAsync_ExistingDocument_PersistsLogAndReturnsAnswer()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Document
            {
                Id = id,
                FileName = "doc.txt",
                BlobUrl = "https://blob/doc.txt",
                Content = "The sky is blue."
            });

        _ai
            .Setup(a => a.AskAsync("The sky is blue.", "What color?", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Blue.");

        _repository
            .Setup(r => r.AddLogAsync(It.IsAny<ConsultationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.AskAsync(id, new AskDocumentRequest("What color?"));

        Assert.Equal("What color?", result.Question);
        Assert.Equal("Blue.", result.Answer);
        _repository.Verify(r => r.AddLogAsync(It.Is<ConsultationLog>(l =>
            l.DocumentId == id &&
            l.Question == "What color?" &&
            l.Answer == "Blue."), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_MissingDocument_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.ExistsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetHistoryAsync(id));
    }

    [Fact]
    public async Task GetHistoryAsync_ExistingDocument_MapsLogs()
    {
        var id = Guid.NewGuid();
        var askedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        _repository
            .Setup(r => r.ExistsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repository
            .Setup(r => r.GetLogsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsultationLog
                {
                    DocumentId = id,
                    Question = "What is AI?",
                    Answer = "AI is...",
                    AskedAt = askedAt
                }
            ]);

        var result = await _sut.GetHistoryAsync(id);

        Assert.Single(result);
        Assert.Equal("What is AI?", result[0].Question);
        Assert.Equal("AI is...", result[0].Answer);
        Assert.Equal(askedAt, result[0].AskedAt);
    }
}
