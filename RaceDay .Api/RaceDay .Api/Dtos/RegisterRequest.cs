using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Dtos;

public class RegisterRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    [RegularExpression(
        "^(Organiser|Participant)$",
        ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;

    // Required by the API logic only when registering an Organiser.
    public string? OrganiserInviteCode { get; set; }
}