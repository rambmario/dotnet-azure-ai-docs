using System.ComponentModel.DataAnnotations;

namespace AzureAiDocs.Application.Options;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = "documents";
}
