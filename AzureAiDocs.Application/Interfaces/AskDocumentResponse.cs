namespace AzureAiDocs.Application.DTOs;

public record AskDocumentResponse(string Question, string Answer, DateTime AskedAt);