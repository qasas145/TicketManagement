using TicketManagement.Services.Inventory.Entities;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.Repositories;

public interface ISeatRepository
{
    Task<Seat?> GetByEventIdAndSeatNumberAsync(long eventId, string seatNumber);
    Task<Seat?> GetByIdAsync(long seatId);
    Task<List<Seat>> GetByEventIdAsync(long eventId);
    Task<Seat> AddAsync(Seat seat);
    Task<Seat> UpdateAsync(Seat seat);
    Task<bool> UpdateSeatStatusWithVersionAsync(long seatId, SeatStatus newStatus, long expectedVersion, string? reservedBy, DateTime? reservedUntil);
    Task<List<Seat>> GetAvailableSeatsByEventIdAsync(long eventId);
}

