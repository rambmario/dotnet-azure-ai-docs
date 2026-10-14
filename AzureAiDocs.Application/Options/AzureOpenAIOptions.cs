using System.ComponentModel.DataAnnotations;

namespace AzureAiDocs.Application.Options;

public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string DeploymentName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Maximum characters of document content included in a single prompt.
    /// Longer documents are chunked and ranked by relevance to the question.
    /// </summary>
    public int MaxPromptCharacters { get; set; } = 12_000;
}
