using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Models;

public class Event
{
    [Key]
    public int EventId { get; set; }

    [Required]
    public int OrganiserId { get; set; }

    [Required]
    [StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string EventType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; set; }

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

    public DateTime RegistrationOpenDate { get; set; }

    public DateTime RegistrationCloseDate { get; set; }

    public int? MaxParticipants { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(512)]
    public string? BannerBlobName { get; set; }

    public User Organiser { get; set; } = null!;

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public EventRoute? EventRoute { get; set; }
}