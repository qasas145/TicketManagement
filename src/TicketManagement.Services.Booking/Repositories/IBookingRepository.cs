using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Repositories;

public interface IBookingRepository
{
    Task<Entities.Booking?> GetByIdAsync(long bookingId);
    Task<Entities.Booking?> GetByReferenceAsync(string bookingReference);
    Task<List<Entities.Booking>> GetByUserIdAsync(string userId);
    Task<Entities.Booking> AddAsync(Entities.Booking booking);
    Task<Entities.Booking> UpdateAsync(Entities.Booking booking);
}

