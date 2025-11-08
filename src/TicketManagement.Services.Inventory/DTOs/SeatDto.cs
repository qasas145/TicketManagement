using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.DTOs;

public class SeatDto
{
    public long SeatId { get; set; }
    public long EventId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string? RowNumber { get; set; }
    public SeatType SeatType { get; set; }
    public decimal Price { get; set; }
    public SeatStatus Status { get; set; }
}

