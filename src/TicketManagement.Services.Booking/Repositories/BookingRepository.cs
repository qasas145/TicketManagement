using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Booking.Data;
using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Entities.Booking?> GetByIdAsync(long bookingId)
    {
        return await _context.Bookings.FindAsync(bookingId);
    }

    public async Task<Entities.Booking?> GetByReferenceAsync(string bookingReference)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);
    }

    public async Task<List<Entities.Booking>> GetByUserIdAsync(string userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Entities.Booking> AddAsync(Entities.Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<Entities.Booking> UpdateAsync(Entities.Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
        return booking;
    }
}

