namespace AzureAiDocs.Domain.Entities;

public class ConsultationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;
    public Document? Document { get; set; }
}