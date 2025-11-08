namespace TicketManagement.Services.Inventory.DTOs;

public class ReservationResponse
{
    public List<ReservationDto> Reservations { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class ReservationDto
{
    public long ReservationId { get; set; }
    public long SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public long EventId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

