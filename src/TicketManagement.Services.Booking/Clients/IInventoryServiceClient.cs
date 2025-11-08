using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Booking.Clients;

public interface IInventoryServiceClient
{
    Task<ReservationDto?> GetReservationAsync(long reservationId);
    Task<SeatDto?> GetSeatAsync(long eventId, long seatId);
    Task<SeatDto?> GetSeatByIdAsync(long seatId);
    Task ConfirmReservationAsync(long reservationId, long bookingId);
}

public class SeatDto
{
    public long SeatId { get; set; }
    public long EventId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string? RowNumber { get; set; }
    public SeatType SeatType { get; set; }
    public decimal Price { get; set; }
    public SeatStatus Status { get; set; }
    public string? ReservedBy { get; set; }
}

public enum ReservationStatus
{
    Active,
    Confirmed,
    Expired,
    Cancelled
}

public class ReservationDto
{
    public long ReservationId { get; set; }
    public long SeatId { get; set; }
    public long EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public ReservationStatus Status { get; set; }
}

