using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Models;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(
    RaceDayDbContext context,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers an Organiser or Participant account.
    /// The password is hashed before it is stored.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Organiser accounts require a private invitation code.
        if (request.Role == "Organiser")
        {
            var configuredCode =
                _configuration["Registration:OrganiserInviteCode"];

            if (string.IsNullOrWhiteSpace(configuredCode) ||
                !string.Equals(
                    request.OrganiserInviteCode,
                    configuredCode,
                    StringComparison.Ordinal))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "Organiser registration requires a valid invitation code."
                    });
            }
        }
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "An account with this email address already exists."
            });
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim(),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Also handle a possible duplicate email caused by
            // concurrent registration attempts.
            var duplicateEmail = await _context.Users
                .AnyAsync(u => u.Email == email, cancellationToken);

            if (duplicateEmail)
            {
                return Conflict(new
                {
                    message = "An account with this email address already exists."
                });
            }

            throw;
        }

        var response = ToUserResponse(user);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    /// <summary>
    /// Authenticates a user and stores the user ID and role in the session.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .SingleOrDefaultAsync(
                u => u.Email == email,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email address or password."
            });
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email address or password."
            });
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            await _context.SaveChangesAsync(cancellationToken);
        }

        // Store the authenticated identity and role in the session.
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("Email", user.Email);

        return Ok(new
        {
            message = "Login successful.",
            user = ToUserResponse(user)
        });
    }

    /// <summary>
    /// Clears the authenticated user's session.
    /// </summary>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return NoContent();
    }

    private static UserResponse ToUserResponse(User user)
    {
        return new UserResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}