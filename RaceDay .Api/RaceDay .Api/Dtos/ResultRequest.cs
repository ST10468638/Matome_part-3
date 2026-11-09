using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Dtos;

public class ResultRequest
{
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? OverallPosition { get; set; }

    [Range(1, 1000000)]
    public int? CategoryPosition { get; set; }

    [Required]
    [RegularExpression(
        "^(Finished|DNF|DNS|Disqualified)$",
        ErrorMessage = "ResultStatus must be Finished, DNF, DNS, or Disqualified.")]
    public string ResultStatus { get; set; } = "Finished";
}