using Microsoft.AspNetCore.Http;

namespace RaceDay_.Api.Services;

public static class ImageUploadValidator
{
    public const long MaximumFileSize = 5 * 1024 * 1024;

    public static async Task<bool> IsValidImageAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > MaximumFileSize)
        {
            return false;
        }

        var expectedLength = file.ContentType switch
        {
            "image/jpeg" => 3,
            "image/png" => 8,
            "image/webp" => 12,
            _ => 0
        };

        if (expectedLength == 0)
        {
            return false;
        }

        var header = new byte[expectedLength];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (bytesRead != expectedLength)
        {
            return false;
        }

        return file.ContentType switch
        {
            "image/jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => header[0] == (byte)'R' && header[1] == (byte)'I' &&
                             header[2] == (byte)'F' && header[3] == (byte)'F' &&
                             header[8] == (byte)'W' && header[9] == (byte)'E' &&
                             header[10] == (byte)'B' && header[11] == (byte)'P',
            _ => false
        };
    }
}
