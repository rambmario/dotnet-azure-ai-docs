using System.ComponentModel.DataAnnotations;

namespace AzureAiDocs.Application.Options;

public class ApiKeyAuthOptions
{
    public const string SectionName = "ApiKeyAuth";
    public const string HeaderName = "X-Api-Key";

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
