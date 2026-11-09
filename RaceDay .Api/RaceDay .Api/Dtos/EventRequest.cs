
using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Dtos;

public class EventRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(Running|Walking|Cycling)$",
        ErrorMessage = "EventType must be Running, Walking, or Cycling.")]
    public string EventType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99",
        ErrorMessage = "DistanceKm must be between 0.01 and 9999.99 km.")]
    public decimal DistanceKm { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(150)]
    public string Venue { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required]
    public DateTime RegistrationOpenDate { get; set; }

    [Required]
    public DateTime RegistrationCloseDate { get; set; }

    [Range(1, 1000000)]
    public int? MaxParticipants { get; set; }
}
