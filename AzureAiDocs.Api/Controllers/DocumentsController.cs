using AzureAiDocs.Api.Auth;
using AzureAiDocs.Application.DTOs;
using AzureAiDocs.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureAiDocs.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents)
    {
        _documents = documents;
    }

    /// <summary>Upload a .txt or .pdf document (max size configured in DocumentUpload).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadDocumentResponse>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest("No file provided.");

        await using var stream = file.OpenReadStream();
        var response = await _documents.UploadAsync(
            stream,
            file.FileName,
            file.Length,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Ask a natural-language question grounded in the document content.</summary>
    [HttpPost("{id:guid}/ask")]
    [ProducesResponseType(typeof(AskDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AskDocumentResponse>> Ask(
        Guid id,
        [FromBody] AskDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _documents.AskAsync(id, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Return the Q and A history for a document.</summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<AskDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AskDocumentResponse>>> History(
        Guid id,
        CancellationToken cancellationToken)
    {
        var history = await _documents.GetHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}
