using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Models;

public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Confirmed";

    [StringLength(20)]
    public string? BibNumber { get; set; }

    public User Participant { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}