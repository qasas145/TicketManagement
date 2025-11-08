using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.Entities;

public class Seat
{
    public long SeatId { get; set; }
    public long EventId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string? RowNumber { get; set; }
    public SeatType SeatType { get; set; } = SeatType.Regular;
    public decimal Price { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public long Version { get; set; } = 0;
    public string? ReservedBy { get; set; }
    public DateTime? ReservedUntil { get; set; }
    public long? BookingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

