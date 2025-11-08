namespace TicketManagement.Services.Booking.DTOs;

public class ConfirmBookingRequest
{
    public long ReservationId { get; set; }
    public PaymentRequest Payment { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}

public class PaymentRequest
{
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

