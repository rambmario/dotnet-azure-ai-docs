namespace AzureAiDocs.Application.Interfaces;

public interface IStorageService
{
    Task<(string blobUrl, string content)> UploadAsync(Stream fileStream, string fileName);
}