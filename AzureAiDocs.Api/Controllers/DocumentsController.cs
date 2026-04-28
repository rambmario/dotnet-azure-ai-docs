using AzureAiDocs.Application.DTOs;
using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiDocs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storage;
    private readonly IAiService _ai;

    public DocumentsController(
        IDocumentRepository repository,
        IStorageService storage,
        IAiService ai)
    {
        _repository = repository;
        _storage = storage;
        _ai = ai;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        using var stream = file.OpenReadStream();
        var (blobUrl, content) = await _storage.UploadAsync(stream, file.FileName);

        var document = new Document
        {
            FileName = file.FileName,
            BlobUrl = blobUrl,
            Content = content
        };

        var saved = await _repository.AddAsync(document);

        return Ok(new UploadDocumentResponse(
            saved.Id, saved.FileName, saved.UploadedAt));
    }

    [HttpPost("{id}/ask")]
    public async Task<ActionResult<AskDocumentResponse>> Ask(
        Guid id, [FromBody] AskDocumentRequest request)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document == null) return NotFound("Document not found.");

        var answer = await _ai.AskAsync(document.Content, request.Question);

        var log = new ConsultationLog
        {
            DocumentId = id,
            Question = request.Question,
            Answer = answer
        };

        await _repository.AddLogAsync(log);

        return Ok(new AskDocumentResponse(request.Question, answer, log.AskedAt));
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> History(Guid id)
    {
        var logs = await _repository.GetLogsAsync(id);
        return Ok(logs.Select(l => new AskDocumentResponse(
            l.Question, l.Answer, l.AskedAt)));
    }
}