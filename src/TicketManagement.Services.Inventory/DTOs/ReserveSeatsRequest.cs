namespace TicketManagement.Services.Inventory.DTOs;

public class ReserveSeatsRequest
{
    public long EventId { get; set; }
    public List<string> SeatNumbers { get; set; } = new();
}

