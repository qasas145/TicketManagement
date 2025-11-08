namespace TicketManagement.Services.Booking.Clients;

public interface IPaymentServiceClient
{
    Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request);
    Task<bool> RefundPaymentAsync(string paymentId);
}

public class PaymentRequestDto
{
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PaymentResponseDto
{
    public bool Success { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

