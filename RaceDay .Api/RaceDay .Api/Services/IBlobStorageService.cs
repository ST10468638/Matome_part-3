using Microsoft.AspNetCore.Http;

namespace RaceDay_.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, IFormFile file, CancellationToken cancellationToken);
    Task DeleteIfExistsAsync(string containerName, string? blobName, CancellationToken cancellationToken);
    string? GetReadUrl(string containerName, string? blobName);
    bool IsConfigured { get; }
}
