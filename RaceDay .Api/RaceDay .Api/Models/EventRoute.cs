using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Models;

public class EventRoute
{
    [Key]
    public int EventRouteId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(150)]
    public string RouteName { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    [Required]
    [StringLength(200)]
    public string StartLocation { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FinishLocation { get; set; } = string.Empty;

    [StringLength(500)]
    public string? RouteUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
}