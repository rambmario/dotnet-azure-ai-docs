using AzureAiDocs.Api.Controllers;
using AzureAiDocs.Application.DTOs;
using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AzureAiDocs.Tests.Controllers;

public class DocumentsControllerTests
{
    private readonly Mock<IDocumentRepository> _repositoryMock;
    private readonly Mock<IStorageService> _storageMock;
    private readonly Mock<IAiService> _aiMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _repositoryMock = new Mock<IDocumentRepository>();
        _storageMock = new Mock<IStorageService>();
        _aiMock = new Mock<IAiService>();

        _controller = new DocumentsController(
            _repositoryMock.Object,
            _storageMock.Object,
            _aiMock.Object);
    }

    // =====================================================================
    // UPLOAD
    // =====================================================================

    [Fact]
    public async Task Upload_ValidFile_ReturnsOkWithResponse()
    {
        // Arrange
        var fileContent = "Hello, this is a document."u8.ToArray();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var blobUrl = "https://storage.blob.core.windows.net/docs/test.txt";
        var content = "Hello, this is a document.";

        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "test.txt"))
            .ReturnsAsync((blobUrl, content));

        var savedDocument = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "test.txt",
            BlobUrl = blobUrl,
            Content = content,
            UploadedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Document>()))
            .ReturnsAsync(savedDocument);

        // Act
        var result = await _controller.Upload(fileMock.Object);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UploadDocumentResponse>(ok.Value);
        Assert.Equal(savedDocument.Id, response.DocumentId);
        Assert.Equal("test.txt", response.FileName);
    }

    [Fact]
    public async Task Upload_NullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Upload(null!);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No file provided.", bad.Value);
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.Upload(fileMock.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Upload_CallsStorageAndRepository()
    {
        // Arrange
        var fileContent = "content"u8.ToArray();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("doc.pdf");
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(("https://blob/doc.pdf", "content"));

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Document>()))
            .ReturnsAsync(new Document
            {
                Id = Guid.NewGuid(),
                FileName = "doc.pdf",
                BlobUrl = "https://blob/doc.pdf",
                Content = "content",
                UploadedAt = DateTime.UtcNow
            });

        // Act
        await _controller.Upload(fileMock.Object);

        // Assert
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), "doc.pdf"), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>()), Times.Once);
    }

    // =====================================================================
    // ASK
    // =====================================================================

    [Fact]
    public async Task Ask_ExistingDocument_ReturnsOkWithAnswer()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            FileName = "test.txt",
            Content = "The sky is blue.",
            BlobUrl = "https://blob/test.txt",
            UploadedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(docId))
            .ReturnsAsync(document);

        _aiMock
            .Setup(a => a.AskAsync("The sky is blue.", "What color is the sky?"))
            .ReturnsAsync("The sky is blue.");

        _repositoryMock
            .Setup(r => r.AddLogAsync(It.IsAny<ConsultationLog>()))
            .Returns(Task.CompletedTask);

        var request = new AskDocumentRequest("What color is the sky?");

        // Act
        var result = await _controller.Ask(docId, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AskDocumentResponse>(ok.Value);
        Assert.Equal("What color is the sky?", response.Question);
        Assert.Equal("The sky is blue.", response.Answer);
    }

    [Fact]
    public async Task Ask_DocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(docId))
            .ReturnsAsync((Document?)null);

        var request = new AskDocumentRequest("Any question?");

        // Act
        var result = await _controller.Ask(docId, request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Document not found.", notFound.Value);
    }

    [Fact]
    public async Task Ask_SavesConsultationLog()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(docId))
            .ReturnsAsync(new Document
            {
                Id = docId,
                Content = "Some content",
                FileName = "file.txt",
                BlobUrl = "https://blob/file.txt",
                UploadedAt = DateTime.UtcNow
            });

        _aiMock
            .Setup(a => a.AskAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Some answer");

        _repositoryMock
            .Setup(r => r.AddLogAsync(It.IsAny<ConsultationLog>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.Ask(docId, new AskDocumentRequest("Question?"));

        // Assert
        _repositoryMock.Verify(r => r.AddLogAsync(It.Is<ConsultationLog>(l =>
            l.DocumentId == docId &&
            l.Question == "Question?" &&
            l.Answer == "Some answer"
        )), Times.Once);
    }

    [Fact]
    public async Task Ask_PassesDocumentContentToAiService()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var content = "Specific document content for AI";

        _repositoryMock
            .Setup(r => r.GetByIdAsync(docId))
            .ReturnsAsync(new Document
            {
                Id = docId,
                Content = content,
                FileName = "file.txt",
                BlobUrl = "https://blob/file.txt",
                UploadedAt = DateTime.UtcNow
            });

        _aiMock
            .Setup(a => a.AskAsync(content, It.IsAny<string>()))
            .ReturnsAsync("answer");

        _repositoryMock
            .Setup(r => r.AddLogAsync(It.IsAny<ConsultationLog>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.Ask(docId, new AskDocumentRequest("Q?"));

        // Assert
        _aiMock.Verify(a => a.AskAsync(content, "Q?"), Times.Once);
    }

    // =====================================================================
    // HISTORY
    // =====================================================================

    [Fact]
    public async Task History_ExistingDocument_ReturnsAllLogs()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var logs = new List<ConsultationLog>
        {
            new() { DocumentId = docId, Question = "Q1", Answer = "A1", AskedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { DocumentId = docId, Question = "Q2", Answer = "A2", AskedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { DocumentId = docId, Question = "Q3", Answer = "A3", AskedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetLogsAsync(docId))
            .ReturnsAsync(logs);

        // Act
        var result = await _controller.History(docId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var responses = Assert.IsAssignableFrom<IEnumerable<AskDocumentResponse>>(ok.Value);
        Assert.Equal(3, responses.Count());
    }

    [Fact]
    public async Task History_NoLogs_ReturnsEmptyList()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetLogsAsync(docId))
            .ReturnsAsync(new List<ConsultationLog>());

        // Act
        var result = await _controller.History(docId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var responses = Assert.IsAssignableFrom<IEnumerable<AskDocumentResponse>>(ok.Value);
        Assert.Empty(responses);
    }

    [Fact]
    public async Task History_MapsLogsToAskDocumentResponse()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var askedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        _repositoryMock
            .Setup(r => r.GetLogsAsync(docId))
            .ReturnsAsync(new List<ConsultationLog>
            {
                new() { DocumentId = docId, Question = "What is AI?", Answer = "AI is...", AskedAt = askedAt }
            });

        // Act
        var result = await _controller.History(docId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var responses = Assert.IsAssignableFrom<IEnumerable<AskDocumentResponse>>(ok.Value).ToList();
        Assert.Single(responses);
        Assert.Equal("What is AI?", responses[0].Question);
        Assert.Equal("AI is...", responses[0].Answer);
        Assert.Equal(askedAt, responses[0].AskedAt);
    }
}
