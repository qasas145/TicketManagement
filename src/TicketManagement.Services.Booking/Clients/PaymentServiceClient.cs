using TicketManagement.Services.Booking.Clients;

namespace TicketManagement.Services.Booking.Clients;

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request)
    {
        try
        {
            var paymentRequest = new
            {
                CardNumber = request.CardNumber,
                CardHolderName = request.CardHolderName,
                ExpiryDate = request.ExpiryDate,
                Cvv = request.Cvv,
                Amount = request.Amount
            };

            var response = await _httpClient.PostAsJsonAsync("/api/payment/process", paymentRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Payment processing failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return new PaymentResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Payment failed: {response.StatusCode}"
                };
            }

            var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponseDto>();
            return paymentResponse ?? new PaymentResponseDto
            {
                Success = false,
                ErrorMessage = "Invalid response from payment service"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error processing payment");
            return new PaymentResponseDto
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return new PaymentResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> RefundPaymentAsync(string paymentId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/payment/refund/{paymentId}", null);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Payment refund failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return false;
            }

            var refundResponse = await response.Content.ReadFromJsonAsync<RefundResponseDto>();
            return refundResponse?.Success ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error refunding payment {PaymentId}", paymentId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
            return false;
        }
    }
}

public class RefundResponseDto
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
}

