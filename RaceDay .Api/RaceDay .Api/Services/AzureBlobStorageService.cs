using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;

namespace RaceDay_.Api.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient? _blobServiceClient;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
    }

    public bool IsConfigured => _blobServiceClient is not null;

    public async Task<string> UploadAsync(
        string containerName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Only JPEG, PNG and WebP images are supported.")
        };

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var container = client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(
            publicAccessType: PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            },
            cancellationToken);

        return blobName;
    }

    public async Task DeleteIfExistsAsync(
        string containerName,
        string? blobName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName) || _blobServiceClient is null)
        {
            return;
        }

        var blob = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public string? GetReadUrl(string containerName, string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName) || _blobServiceClient is null)
        {
            return null;
        }

        var blob = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (!blob.CanGenerateSasUri)
        {
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        return blob.GenerateSasUri(BlobSasPermissions.Read, expiresAt).ToString();
    }

    private BlobServiceClient GetClient()
    {
        return _blobServiceClient ?? throw new InvalidOperationException(
            "Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString before uploading images.");
    }
}
