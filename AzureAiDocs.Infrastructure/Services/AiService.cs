using Azure;
using Azure.AI.OpenAI;
using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AzureAiDocs.Infrastructure.Services;

public class AiService : IAiService
{
    private const int ChunkSize = 2_000;
    private const int ChunkOverlap = 200;

    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AiService> _logger;

    public AiService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AiService> logger)
    {
        _options = options.Value;
        _client = new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));
        _logger = logger;
    }

    public async Task<string> AskAsync(
        string documentContent,
        string question,
        CancellationToken cancellationToken = default)
    {
        var groundedContent = SelectRelevantContent(documentContent, question, _options.MaxPromptCharacters);

        _logger.LogDebug(
            "Sending prompt with {CharCount} characters of grounded content",
            groundedContent.Length);

        var chatClient = _client.GetChatClient(_options.DeploymentName);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a helpful assistant. Answer questions based only " +
                "on the document content provided. If the answer is not in " +
                "the document, say so clearly."),
            new UserChatMessage(
                $"Document content:\n\n{groundedContent}\n\nQuestion: {question}")
        };

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }

    /// <summary>
    /// For short documents, returns the full text. For longer ones, splits into
    /// overlapping chunks and keeps the highest-scoring chunks by keyword overlap
    /// with the question (lightweight retrieval before calling the model).
    /// </summary>
    public static string SelectRelevantContent(string documentContent, string question, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(documentContent))
            return string.Empty;

        if (documentContent.Length <= maxCharacters)
            return documentContent;

        var chunks = ChunkText(documentContent, ChunkSize, ChunkOverlap);
        var terms = Tokenize(question);

        var ranked = chunks
            .Select((chunk, index) => new
            {
                Index = index,
                Chunk = chunk,
                Score = ScoreChunk(chunk, terms)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Index)
            .ToList();

        var selected = new List<(int Index, string Chunk)>();
        var total = 0;

        foreach (var item in ranked)
        {
            if (total + item.Chunk.Length > maxCharacters && selected.Count > 0)
                break;

            selected.Add((item.Index, item.Chunk));
            total += item.Chunk.Length;
        }

        return string.Join("\n\n---\n\n", selected.OrderBy(x => x.Index).Select(x => x.Chunk));
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var step = Math.Max(1, chunkSize - overlap);

        for (var i = 0; i < text.Length; i += step)
        {
            var length = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, length));
            if (i + length >= text.Length)
                break;
        }

        return chunks;
    }

    private static HashSet<string> Tokenize(string text) =>
        text.Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '?', '!', '(', ')', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.Ordinal);

    private static int ScoreChunk(string chunk, HashSet<string> terms)
    {
        if (terms.Count == 0)
            return 0;

        var chunkTerms = Tokenize(chunk);
        return terms.Count(t => chunkTerms.Contains(t));
    }
}
