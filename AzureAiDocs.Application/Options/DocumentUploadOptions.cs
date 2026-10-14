namespace AzureAiDocs.Application.Options;

public class DocumentUploadOptions
{
    public const string SectionName = "DocumentUpload";

    /// <summary>Maximum upload size in bytes (default 5 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Allowed file extensions (including the leading dot).</summary>
    public string[] AllowedExtensions { get; set; } = [".txt", ".pdf"];
}
