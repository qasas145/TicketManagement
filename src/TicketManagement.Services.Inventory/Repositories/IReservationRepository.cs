using TicketManagement.Services.Inventory.Entities;

namespace TicketManagement.Services.Inventory.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(long reservationId);
    Task<List<Reservation>> GetExpiredReservationsAsync(DateTime now);
    Task<Reservation> AddAsync(Reservation reservation);
    Task<Reservation> UpdateAsync(Reservation reservation);
    Task<List<Reservation>> GetByUserIdAsync(string userId);
}

