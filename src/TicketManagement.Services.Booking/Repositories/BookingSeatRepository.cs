using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Booking.Data;
using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Repositories;

public class BookingSeatRepository : IBookingSeatRepository
{
    private readonly BookingDbContext _context;

    public BookingSeatRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<BookingSeat> AddAsync(BookingSeat bookingSeat)
    {
        _context.BookingSeats.Add(bookingSeat);
        await _context.SaveChangesAsync();
        return bookingSeat;
    }

    public async Task<List<BookingSeat>> GetByBookingIdAsync(long bookingId)
    {
        return await _context.BookingSeats
            .Where(bs => bs.BookingId == bookingId)
            .ToListAsync();
    }
}

