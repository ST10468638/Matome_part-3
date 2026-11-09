using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Models;

public class Result
{
    [Key]
    public int ResultId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }

    public TimeSpan? FinishTime { get; set; }

    public int? OverallPosition { get; set; }

    public int? CategoryPosition { get; set; }

    [Required]
    [StringLength(20)]
    public string ResultStatus { get; set; } = "Finished";

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Enrolment Enrolment { get; set; } = null!;
}