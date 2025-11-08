using System.Net.Http.Json;
using TicketManagement.Services.Booking.Clients;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Booking.Clients;

public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ReservationDto?> GetReservationAsync(long reservationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/inventory/reservations/{reservationId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                response.EnsureSuccessStatusCode();
            }

            var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
            return reservation;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting reservation {ReservationId}", reservationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation {ReservationId}", reservationId);
            return null;
        }
    }

    public async Task<SeatDto?> GetSeatAsync(long eventId, long seatId)
    {
        try
        {
            // First get seat by ID to get seat number, then get full details
            var seatById = await GetSeatByIdAsync(seatId);
            if (seatById == null)
            {
                return null;
            }

            // Get full seat details using eventId and seatNumber
            var response = await _httpClient.GetAsync($"/api/inventory/events/{eventId}/seats/{seatById.SeatNumber}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                response.EnsureSuccessStatusCode();
            }

            var seat = await response.Content.ReadFromJsonAsync<SeatDto>();
            return seat;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting seat {SeatId} for event {EventId}", seatId, eventId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat {SeatId} for event {EventId}", seatId, eventId);
            return null;
        }
    }

    public async Task<SeatDto?> GetSeatByIdAsync(long seatId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/inventory/seats/{seatId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                response.EnsureSuccessStatusCode();
            }

            var seat = await response.Content.ReadFromJsonAsync<SeatDto>();
            return seat;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting seat {SeatId}", seatId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat {SeatId}", seatId);
            return null;
        }
    }

    public async Task ConfirmReservationAsync(long reservationId, long bookingId)
    {
        try
        {
            var requestBody = new { ReservationId = reservationId, BookingId = bookingId };
            var response = await _httpClient.PostAsJsonAsync($"/api/inventory/reservations/{reservationId}/confirm", requestBody);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error confirming reservation {ReservationId}", reservationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming reservation {ReservationId}", reservationId);
            throw;
        }
    }
}

