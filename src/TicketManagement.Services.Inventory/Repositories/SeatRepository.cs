using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Inventory.Data;
using TicketManagement.Services.Inventory.Entities;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly InventoryDbContext _context;

    public SeatRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Seat?> GetByEventIdAndSeatNumberAsync(long eventId, string seatNumber)
    {
        return await _context.Seats
            .FirstOrDefaultAsync(s => s.EventId == eventId && s.SeatNumber == seatNumber);
    }

    public async Task<Seat?> GetByIdAsync(long seatId)
    {
        return await _context.Seats.FindAsync(seatId);
    }

    public async Task<List<Seat>> GetByEventIdAsync(long eventId)
    {
        return await _context.Seats
            .Where(s => s.EventId == eventId)
            .ToListAsync();
    }

    public async Task<Seat> AddAsync(Seat seat)
    {
        _context.Seats.Add(seat);
        await _context.SaveChangesAsync();
        return seat;
    }

    public async Task<Seat> UpdateAsync(Seat seat)
    {
        seat.Version++;
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync();
        return seat;
    }

    public async Task<bool> UpdateSeatStatusWithVersionAsync(long seatId, SeatStatus newStatus, long expectedVersion, string? reservedBy, DateTime? reservedUntil)
    {
        var seat = await _context.Seats.FindAsync(seatId);
        if (seat == null || seat.Version != expectedVersion)
        {
            return false;
        }

        seat.Status = newStatus;
        seat.ReservedBy = reservedBy;
        seat.ReservedUntil = reservedUntil;
        seat.Version++;
        
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<List<Seat>> GetAvailableSeatsByEventIdAsync(long eventId)
    {
        return await _context.Seats
            .Where(s => s.EventId == eventId && s.Status == SeatStatus.Available)
            .ToListAsync();
    }
}

