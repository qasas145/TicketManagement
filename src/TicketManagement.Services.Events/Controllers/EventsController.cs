using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Events.Data;
using TicketManagement.Services.Events.DTOs;

namespace TicketManagement.Services.Events.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventsDbContext _context;
    private readonly ILogger<EventsController> _logger;

    public EventsController(EventsDbContext context, ILogger<EventsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetEvents()
    {
        var events = await _context.Events
            .Where(e => e.Status == Shared.Models.EventStatus.OnSale)
            .ToListAsync();

        return Ok(events.Select(e => new EventDto
        {
            EventId = e.EventId,
            EventName = e.EventName,
            EventDate = e.EventDate,
            VenueName = e.VenueName,
            AvailableSeats = e.AvailableSeats,
            Status = e.Status.ToString()
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(long id)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity == null)
        {
            return NotFound();
        }

        return Ok(new EventDto
        {
            EventId = eventEntity.EventId,
            EventName = eventEntity.EventName,
            EventDate = eventEntity.EventDate,
            VenueName = eventEntity.VenueName,
            AvailableSeats = eventEntity.AvailableSeats,
            Status = eventEntity.Status.ToString()
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        var eventEntity = new Entities.Event
        {
            EventName = request.EventName,
            EventDate = request.EventDate,
            VenueName = request.VenueName,
            TotalSeats = request.TotalSeats,
            AvailableSeats = request.TotalSeats,
            Status = Shared.Models.EventStatus.Upcoming,
            SaleStartTime = request.SaleStartTime
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        return Ok(new EventDto
        {
            EventId = eventEntity.EventId,
            EventName = eventEntity.EventName,
            EventDate = eventEntity.EventDate,
            VenueName = eventEntity.VenueName,
            AvailableSeats = eventEntity.AvailableSeats,
            Status = eventEntity.Status.ToString()
        });
    }
}

