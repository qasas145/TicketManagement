using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Events.Entities;

public class Event
{
    public long EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? VenueName { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Upcoming;
    public DateTime? SaleStartTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

