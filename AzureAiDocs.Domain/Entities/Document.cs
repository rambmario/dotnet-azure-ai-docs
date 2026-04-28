namespace AzureAiDocs.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ConsultationLog> Logs { get; set; } = new List<ConsultationLog>();
}