using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.Entities;

public class Reservation
{
    public long ReservationId { get; set; }
    public long SeatId { get; set; }
    public long EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

