using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Booking.DTOs;

public class BookingResponse
{
    public long BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public long EventId { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public List<BookingSeatDto> Seats { get; set; } = new();
}

public class BookingSeatDto
{
    public long SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

