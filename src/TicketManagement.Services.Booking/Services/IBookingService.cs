using TicketManagement.Services.Booking.DTOs;

namespace TicketManagement.Services.Booking.Services;

public interface IBookingService
{
    Task<BookingResponse> ConfirmBookingAsync(ConfirmBookingRequest request, string userId);
    Task<List<BookingResponse>> GetUserBookingsAsync(string userId);
    Task<BookingResponse?> GetBookingByReferenceAsync(string bookingReference);
}

