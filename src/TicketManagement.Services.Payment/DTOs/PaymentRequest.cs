namespace TicketManagement.Services.Payment.DTOs;

public class PaymentRequest
{
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class RefundResponse
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
}

