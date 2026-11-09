namespace RaceDay_.Api.Dtos;

public sealed class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public DateTime EventDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public DateTime RegistrationOpenDate { get; set; }
    public DateTime RegistrationCloseDate { get; set; }
    public int? MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BannerImageUrl { get; set; }
}
