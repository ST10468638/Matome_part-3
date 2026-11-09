
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Services;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly IConfiguration _configuration;

    public ProfileController(
        RaceDayDbContext context,
        IBlobStorageService blobStorage,
        IConfiguration configuration)
    {
        _context = context;
        _blobStorage = blobStorage;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId is null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.UserId == userId.Value,
                cancellationToken);

        if (user is null)
        {
            HttpContext.Session.Clear();

            return Unauthorized(new
            {
                message = "The account associated with this session no longer exists."
            });
        }

        return Ok(ToProfileResponse(user));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId is null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.UserId == userId.Value,
            cancellationToken);

        if (user is null)
        {
            HttpContext.Session.Clear();

            return Unauthorized(new
            {
                message = "The account associated with this session no longer exists."
            });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailInUse = await _context.Users.AnyAsync(
            u => u.UserId != userId.Value &&
                 u.Email == normalizedEmail,
            cancellationToken);

        if (emailInUse)
        {
            return Conflict(new
            {
                message = "That email address is already in use."
            });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = request.PhoneNumber?.Trim();

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The profile could not be updated. Check whether the email address is already in use."
            });
        }

        HttpContext.Session.SetString("Email", user.Email);

        return Ok(new
        {
            message = "Profile updated successfully.",
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.CreatedAt,
            ProfilePictureUrl = GetProfilePictureUrl(
                user.ProfilePictureBlobName)
        });
    }

    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadValidator.MaximumFileSize + 64 * 1024)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId is null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        if (!await ImageUploadValidator.IsValidImageAsync(
                file,
                cancellationToken))
        {
            return BadRequest(new
            {
                message = "Choose a valid JPEG, PNG or WebP image up to 5 MB."
            });
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.UserId == userId.Value,
            cancellationToken);

        if (user is null)
        {
            HttpContext.Session.Clear();

            return Unauthorized(new
            {
                message = "The account associated with this session no longer exists."
            });
        }

        var containerName =
            _configuration["AzureBlobStorage:ProfilePictureContainer"]
            ?? "profile-pictures";

        var oldBlobName = user.ProfilePictureBlobName;

        string newBlobName;

        try
        {
            newBlobName = await _blobStorage.UploadAsync(
                containerName,
                file,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Azure Blob Storage is not configured. Set the API storage connection string before uploading a profile picture."
                });
        }
        catch (RequestFailedException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Azure Blob Storage could not store the profile picture. Check the storage account configuration."
                });
        }

        user.ProfilePictureBlobName = newBlobName;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _blobStorage.DeleteIfExistsAsync(
                containerName,
                newBlobName,
                cancellationToken);

            throw;
        }

        await _blobStorage.DeleteIfExistsAsync(
            containerName,
            oldBlobName,
            cancellationToken);

        return Ok(new
        {
            profilePictureUrl = GetProfilePictureUrl(newBlobName)
        });
    }

    private object ToProfileResponse(Models.User user)
    {
        return new
        {
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.CreatedAt,
            ProfilePictureUrl = GetProfilePictureUrl(
                user.ProfilePictureBlobName)
        };
    }

    private string? GetProfilePictureUrl(string? blobName)
    {
        var containerName =
            _configuration["AzureBlobStorage:ProfilePictureContainer"]
            ?? "profile-pictures";

        return _blobStorage.GetReadUrl(containerName, blobName);
    }
}
