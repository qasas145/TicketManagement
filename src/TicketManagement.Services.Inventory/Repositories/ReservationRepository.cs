using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Inventory.Data;
using TicketManagement.Services.Inventory.Entities;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly InventoryDbContext _context;

    public ReservationRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(long reservationId)
    {
        return await _context.Reservations.FindAsync(reservationId);
    }

    public async Task<List<Reservation>> GetExpiredReservationsAsync(DateTime now)
    {
        return await _context.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt < now)
            .ToListAsync();
    }

    public async Task<Reservation> AddAsync(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task<Reservation> UpdateAsync(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task<List<Reservation>> GetByUserIdAsync(string userId)
    {
        return await _context.Reservations
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }
}

