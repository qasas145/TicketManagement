using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Booking.Data;
using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Repositories;

public class IdempotencyKeyRepository : IIdempotencyKeyRepository
{
    private readonly BookingDbContext _context;

    public IdempotencyKeyRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyKey?> GetByKeyAsync(string key)
    {
        return await _context.IdempotencyKeys
            .FirstOrDefaultAsync(ik => ik.Key == key && ik.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<IdempotencyKey> AddAsync(IdempotencyKey idempotencyKey)
    {
        _context.IdempotencyKeys.Add(idempotencyKey);
        await _context.SaveChangesAsync();
        return idempotencyKey;
    }
}

