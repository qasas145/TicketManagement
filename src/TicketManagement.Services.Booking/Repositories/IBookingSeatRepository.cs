using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Repositories;

public interface IBookingSeatRepository
{
    Task<BookingSeat> AddAsync(BookingSeat bookingSeat);
    Task<List<BookingSeat>> GetByBookingIdAsync(long bookingId);
}

