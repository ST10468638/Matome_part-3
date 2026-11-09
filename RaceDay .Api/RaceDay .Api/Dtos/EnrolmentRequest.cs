using System.ComponentModel.DataAnnotations;

namespace RaceDay_.Api.Dtos;

public class EnrolmentRequest
{
    [Required]
    public int CategoryId { get; set; }
}