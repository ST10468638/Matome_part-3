using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Dtos;

public class CategoryRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression(
        "^(Age|Distance)$",
        ErrorMessage = "CategoryType must be Age or Distance.")]
    public string CategoryType { get; set; } = "Age";

    // Required for age-based categories.
    [Range(0, 120)]
    public int? MinimumAge { get; set; }

    [Range(0, 120)]
    public int? MaximumAge { get; set; }

    // Required for distance-based categories, measured in kilometres.
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int Capacity { get; set; }
}