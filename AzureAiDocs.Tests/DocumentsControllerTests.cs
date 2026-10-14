using AzureAiDocs.Api.Controllers;
using AzureAiDocs.Application.DTOs;
using AzureAiDocs.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AzureAiDocs.Tests.Controllers;

public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _documentsMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _documentsMock = new Mock<IDocumentService>();
        _controller = new DocumentsController(_documentsMock.Object);
    }

    [Fact]
    public async Task Upload_ValidFile_ReturnsOkWithResponse()
    {
        var fileContent = "Hello, this is a document."u8.ToArray();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        var expected = new UploadDocumentResponse(Guid.NewGuid(), "test.txt", DateTime.UtcNow);
        _documentsMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "test.txt", fileContent.Length, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Upload(fileMock.Object, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task Upload_NullFile_ReturnsBadRequest()
    {
        var result = await _controller.Upload(null!, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No file provided.", bad.Value);
    }

    [Fact]
    public async Task Ask_ReturnsOkWithAnswer()
    {
        var docId = Guid.NewGuid();
        var request = new AskDocumentRequest("What color is the sky?");
        var expected = new AskDocumentResponse(request.Question, "The sky is blue.", DateTime.UtcNow);

        _documentsMock
            .Setup(s => s.AskAsync(docId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Ask(docId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task History_ReturnsOkWithLogs()
    {
        var docId = Guid.NewGuid();
        IReadOnlyList<AskDocumentResponse> expected =
        [
            new("Q1", "A1", DateTime.UtcNow)
        ];

        _documentsMock
            .Setup(s => s.GetHistoryAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.History(docId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }
}
