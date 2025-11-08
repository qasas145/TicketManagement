using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Services.Inventory.DTOs;
using TicketManagement.Services.Inventory.Repositories;
using TicketManagement.Services.Inventory.Services;

namespace TicketManagement.Services.Inventory.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IReservationRepository _reservationRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        IReservationRepository reservationRepository,
        ISeatRepository seatRepository,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _reservationRepository = reservationRepository;
        _seatRepository = seatRepository;
        _logger = logger;
    }

    [HttpPost("reservations")]
    [Authorize]
    public async Task<ActionResult<ReservationResponse>> ReserveSeats([FromBody] ReserveSeatsRequest request)
    {
        var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
        var response = await _inventoryService.ReserveSeatsAsync(request, userId);
        return Ok(response);
    }

    [HttpGet("reservations/{reservationId}")]
    public async Task<ActionResult<ReservationDto>> GetReservation(long reservationId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            return NotFound();
        }

        var seat = await _seatRepository.GetByIdAsync(reservation.SeatId);
        return Ok(new ReservationDto
        {
            ReservationId = reservation.ReservationId,
            SeatId = reservation.SeatId,
            SeatNumber = seat?.SeatNumber ?? "Unknown",
            EventId = reservation.EventId,
            ExpiresAt = reservation.ExpiresAt
        });
    }

    [HttpPost("reservations/{reservationId}/confirm")]
    [Authorize]
    public async Task<ActionResult> ConfirmReservation(long reservationId, [FromBody] ConfirmReservationRequest request)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            return NotFound();
        }

        var seat = await _seatRepository.GetByIdAsync(reservation.SeatId);
        if (seat == null)
        {
            return NotFound("Seat not found");
        }

        seat.Status = Shared.Models.SeatStatus.Booked;
        seat.BookingId = request.BookingId;
        seat.ReservedBy = null;
        seat.ReservedUntil = null;
        await _seatRepository.UpdateAsync(seat);

        reservation.Status = Shared.Models.ReservationStatus.Confirmed;
        await _reservationRepository.UpdateAsync(reservation);

        return Ok(new { message = "Reservation confirmed" });
    }

    [HttpGet("events/{eventId}/seats")]
    public async Task<ActionResult<List<SeatDto>>> GetAvailableSeats(long eventId)
    {
        var seats = await _inventoryService.GetAvailableSeatsAsync(eventId);
        return Ok(seats);
    }

    [HttpGet("events/{eventId}/seats/{seatNumber}")]
    public async Task<ActionResult<SeatDto>> GetSeat(long eventId, string seatNumber)
    {
        var seat = await _inventoryService.GetSeatAsync(eventId, seatNumber);
        if (seat == null)
        {
            return NotFound();
        }
        return Ok(seat);
    }

    [HttpGet("seats/{seatId}")]
    public async Task<ActionResult<SeatDto>> GetSeatById(long seatId)
    {
        var seat = await _seatRepository.GetByIdAsync(seatId);
        if (seat == null)
        {
            return NotFound();
        }

        return Ok(new SeatDto
        {
            SeatId = seat.SeatId,
            EventId = seat.EventId,
            SeatNumber = seat.SeatNumber,
            Section = seat.Section,
            RowNumber = seat.RowNumber,
            SeatType = seat.SeatType,
            Price = seat.Price,
            Status = seat.Status
        });
    }
}

public class ConfirmReservationRequest
{
    public long BookingId { get; set; }
}
