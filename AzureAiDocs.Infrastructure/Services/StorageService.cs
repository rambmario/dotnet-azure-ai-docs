using Azure.Storage.Blobs;
using AzureAiDocs.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AzureAiDocs.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public StorageService(IConfiguration config)
    {
        _blobServiceClient = new BlobServiceClient(
            config["AzureStorage:ConnectionString"]);
        _containerName = config["AzureStorage:ContainerName"] ?? "documents";
    }

    public async Task<(string blobUrl, string content)> UploadAsync(
        Stream fileStream, string fileName)
    {
        var containerClient = _blobServiceClient
            .GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(fileStream, overwrite: true);

        // Read content as text (supports .txt files)
        fileStream.Position = 0;
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();

        return (blobClient.Uri.ToString(), content);
    }
}