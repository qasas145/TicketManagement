using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Services.Booking.DTOs;
using TicketManagement.Services.Booking.Services;

namespace TicketManagement.Services.Booking.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpPost("confirm")]
    [Authorize]
    public async Task<ActionResult<BookingResponse>> ConfirmBooking([FromBody] ConfirmBookingRequest request)
    {
        var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
        var response = await _bookingService.ConfirmBookingAsync(request, userId);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<List<BookingResponse>>> GetMyBookings()
    {
        var userId = User.Identity?.Name ?? throw new UnauthorizedAccessException();
        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(bookings);
    }

    [HttpGet("reference/{bookingReference}")]
    public async Task<ActionResult<BookingResponse>> GetBookingByReference(string bookingReference)
    {
        var booking = await _bookingService.GetBookingByReferenceAsync(bookingReference);
        if (booking == null)
        {
            return NotFound();
        }
        return Ok(booking);
    }
}

