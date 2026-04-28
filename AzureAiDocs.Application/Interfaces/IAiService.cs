namespace AzureAiDocs.Application.Interfaces;

public interface IAiService
{
    Task<string> AskAsync(string documentContent, string question);
}