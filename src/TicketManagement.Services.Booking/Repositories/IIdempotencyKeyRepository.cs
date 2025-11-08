using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Repositories;

public interface IIdempotencyKeyRepository
{
    Task<IdempotencyKey?> GetByKeyAsync(string key);
    Task<IdempotencyKey> AddAsync(IdempotencyKey idempotencyKey);
}

