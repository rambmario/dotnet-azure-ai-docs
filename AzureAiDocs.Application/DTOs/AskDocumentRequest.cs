using System.ComponentModel.DataAnnotations;

namespace AzureAiDocs.Application.DTOs;

public record AskDocumentRequest(
    [Required(ErrorMessage = "Question is required.")]
    [MinLength(1, ErrorMessage = "Question cannot be empty.")]
    [MaxLength(2000, ErrorMessage = "Question must be at most 2000 characters.")]
    string Question);
