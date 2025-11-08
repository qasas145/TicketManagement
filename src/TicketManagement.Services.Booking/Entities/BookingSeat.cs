namespace TicketManagement.Services.Booking.Entities;

public class BookingSeat
{
    public long BookingSeatId { get; set; }
    public long BookingId { get; set; }
    public long SeatId { get; set; }
    public decimal Price { get; set; }
}

