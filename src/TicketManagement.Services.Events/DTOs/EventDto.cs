namespace TicketManagement.Services.Events.DTOs;

public class EventDto
{
    public long EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? VenueName { get; set; }
    public int AvailableSeats { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? VenueName { get; set; }
    public int TotalSeats { get; set; }
    public DateTime? SaleStartTime { get; set; }
}

