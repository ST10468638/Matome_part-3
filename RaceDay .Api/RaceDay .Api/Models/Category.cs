using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Determines whether this category is age-based or distance-based.
    [Required]
    [StringLength(20)]
    public string CategoryType { get; set; } = "Age";

    // Used for age-based categories.
    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }

    // Used for distance-based categories, measured in kilometres.
    public decimal? DistanceKm { get; set; }

    public decimal EntryFee { get; set; }

    public int? Capacity { get; set; }

    public Event Event { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}