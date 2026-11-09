using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Dtos;

public class EventRouteRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string RouteName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string StartLocation { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string FinishLocation { get; set; } = string.Empty;

    [Url]
    [StringLength(500)]
    public string? RouteUrl { get; set; }
}