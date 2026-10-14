using System.Text;
using Azure.Storage.Blobs;
using AzureAiDocs.Application.Interfaces;
using AzureAiDocs.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace AzureAiDocs.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<StorageService> _logger;

    public StorageService(
        IOptions<AzureStorageOptions> options,
        ILogger<StorageService> logger)
    {
        var value = options.Value;
        _blobServiceClient = new BlobServiceClient(value.ConnectionString);
        _containerName = value.ContainerName;
        _logger = logger;
    }

    public async Task<(string blobUrl, string content)> UploadAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(buffer, overwrite: true, cancellationToken);

        buffer.Position = 0;
        var content = ExtractText(buffer, fileName);

        _logger.LogInformation(
            "Uploaded blob {BlobName} and extracted {CharCount} characters",
            blobName,
            content.Length);

        return (blobClient.Uri.ToString(), content);
    }

    private static string ExtractText(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".txt" => ExtractPlainText(stream),
            ".pdf" => ExtractPdfText(stream),
            _ => throw new InvalidOperationException($"Unsupported file extension '{extension}'.")
        };
    }

    private static string ExtractPlainText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return reader.ReadToEnd().Trim();
    }

    private static string ExtractPdfText(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            var pageText = page.Text;
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                builder.AppendLine(pageText);
                builder.AppendLine();
            }
        }

        return builder.ToString().Trim();
    }
}
