namespace TicketManagement.Services.Booking.Entities;

public class IdempotencyKey
{
    public long IdempotencyKeyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public long? BookingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

