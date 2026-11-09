using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser invitation code"), DataType(DataType.Password)]
    public string? OrganiserInviteCode { get; set; }
}

public sealed class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public sealed class LoginApiResponse
{
    public string Message { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public sealed class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public DateTime EventDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public DateTime RegistrationOpenDate { get; set; }
    public DateTime RegistrationCloseDate { get; set; }
    public int? MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BannerImageUrl { get; set; }
}

public sealed class EventFormViewModel : IValidatableObject
{
    public int EventId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Running|Walking|Cycling)$")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required, Display(Name = "Event date and time")]
    public DateTime EventDate { get; set; } = DateTime.Now.AddDays(30);

    [Required, StringLength(150)]
    public string Venue { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Province { get; set; } = "Gauteng";

    [Required, Display(Name = "Registration opens")]
    public DateTime RegistrationOpenDate { get; set; } = DateTime.Now;

    [Required, Display(Name = "Registration closes")]
    public DateTime RegistrationCloseDate { get; set; } = DateTime.Now.AddDays(20);

    [Range(1, 1000000)]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerImage { get; set; }

    public string? CurrentBannerImageUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RegistrationOpenDate >= RegistrationCloseDate)
        {
            yield return new ValidationResult(
                "Registration must open before it closes.",
                new[] { nameof(RegistrationOpenDate), nameof(RegistrationCloseDate) });
        }

        if (RegistrationCloseDate > EventDate)
        {
            yield return new ValidationResult(
                "Registration must close on or before the event date.",
                new[] { nameof(RegistrationCloseDate), nameof(EventDate) });
        }
    }
}

public sealed class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryType { get; set; } = "Age";
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? Capacity { get; set; }
}

public sealed class CategoryFormViewModel : IValidatableObject
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(Age|Distance)$")]
    [Display(Name = "Category type")]
    public string CategoryType { get; set; } = "Age";

    [Range(0, 120)]
    [Display(Name = "Minimum age")]
    public int? MinimumAge { get; set; }

    [Range(0, 120)]
    [Display(Name = "Maximum age")]
    public int? MaximumAge { get; set; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Entry fee")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int Capacity { get; set; } = 100;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryType == "Age")
        {
            if (!MinimumAge.HasValue || !MaximumAge.HasValue)
            {
                yield return new ValidationResult("Enter both the minimum and maximum ages.",
                    new[] { nameof(MinimumAge), nameof(MaximumAge) });
            }
            else if (MinimumAge > MaximumAge)
            {
                yield return new ValidationResult("Minimum age cannot exceed maximum age.",
                    new[] { nameof(MinimumAge), nameof(MaximumAge) });
            }

            if (DistanceKm.HasValue)
            {
                yield return new ValidationResult("Do not enter a distance for an age category.",
                    new[] { nameof(DistanceKm) });
            }
        }
        else if (CategoryType == "Distance" && !DistanceKm.HasValue)
        {
            yield return new ValidationResult("Enter the category distance.", new[] { nameof(DistanceKm) });
        }
    }
}

public sealed class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public int? ResultId { get; set; }
    public string? ResultStatus { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
}

public sealed class EventSummaryViewModel
{
    public EventDto Event { get; set; } = new();
    public int TotalEnrolments { get; set; }
}

public sealed class ResultFormViewModel : IValidatableObject
{
    [Required]
    public int EnrolmentId { get; set; }

    [Required]
    [RegularExpression("^(Finished|DNF|DNS|Disqualified)$")]
    public string ResultStatus { get; set; } = "Finished";

    [Display(Name = "Finish time")]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 1000000)]
    [Display(Name = "Overall position")]
    public int? OverallPosition { get; set; }

    [Range(1, 1000000)]
    [Display(Name = "Category position")]
    public int? CategoryPosition { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ResultStatus == "Finished" && !FinishTime.HasValue)
        {
            yield return new ValidationResult(
                "A finish time is required for a participant who finished the event.",
                new[] { nameof(FinishTime) });
        }

        if (FinishTime.HasValue && FinishTime.Value < TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "Finish time cannot be negative.",
                new[] { nameof(FinishTime) });
        }
    }
}

public sealed class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int TotalParticipants { get; set; }
    public string PositionDisplay { get; set; } = string.Empty;
}

public sealed class ProfileFormViewModel
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfilePicture { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}
