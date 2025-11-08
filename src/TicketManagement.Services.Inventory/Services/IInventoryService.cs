using TicketManagement.Services.Inventory.DTOs;

namespace TicketManagement.Services.Inventory.Services;

public interface IInventoryService
{
    Task<ReservationResponse> ReserveSeatsAsync(ReserveSeatsRequest request, string userId);
    Task<bool> ReleaseExpiredReservationAsync(long reservationId);
    Task<List<SeatDto>> GetAvailableSeatsAsync(long eventId);
    Task<SeatDto?> GetSeatAsync(long eventId, string seatNumber);
}

