using Microsoft.AspNetCore.Mvc;
using TicketManagement.Services.Payment.DTOs;

namespace TicketManagement.Services.Payment.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(ILogger<PaymentController> logger)
    {
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment([FromBody] PaymentRequest request)
    {
        try
        {
            // Validate payment request
            if (string.IsNullOrWhiteSpace(request.CardNumber) ||
                string.IsNullOrWhiteSpace(request.CardHolderName) ||
                string.IsNullOrWhiteSpace(request.ExpiryDate) ||
                string.IsNullOrWhiteSpace(request.Cvv) ||
                request.Amount <= 0)
            {
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid payment request"
                });
            }

            // Validate card number format (basic validation)
            var cardNumber = request.CardNumber.Replace(" ", "").Replace("-", "");
            if (cardNumber.Length < 13 || cardNumber.Length > 19 || !cardNumber.All(char.IsDigit))
            {
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid card number format"
                });
            }

            // Validate CVV
            if (request.Cvv.Length < 3 || request.Cvv.Length > 4 || !request.Cvv.All(char.IsDigit))
            {
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid CVV"
                });
            }

            // Payment gateway processing
            // In production, integrate with actual payment gateway (Stripe, PayPal, etc.)
            await Task.Delay(150); // Network latency to payment gateway

            // Payment processing logic
            // Business rule: reject cards ending with 0 or 9 for testing purposes
            var lastDigit = cardNumber.Last();
            var isSuccess = lastDigit != '0' && lastDigit != '9';

            if (isSuccess)
            {
                var paymentId = Guid.NewGuid().ToString();
                var transactionId = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";

                _logger.LogInformation("Payment processed successfully: {PaymentId}, Amount: {Amount}", paymentId, request.Amount);

                return Ok(new PaymentResponse
                {
                    Success = true,
                    PaymentId = paymentId,
                    TransactionId = transactionId
                });
            }
            else
            {
                _logger.LogWarning("Payment declined: Card ending with {LastDigit}", lastDigit);
                return BadRequest(new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Payment declined by payment gateway"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return StatusCode(500, new PaymentResponse
            {
                Success = false,
                ErrorMessage = "Internal server error during payment processing"
            });
        }
    }

    [HttpPost("refund/{paymentId}")]
    public async Task<ActionResult<RefundResponse>> RefundPayment(string paymentId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return BadRequest(new RefundResponse
                {
                    Success = false
                });
            }

            // Refund processing
            // In production, integrate with actual payment gateway to process refund
            await Task.Delay(150); // Network latency to payment gateway

            // Refund processing logic
            var refundId = $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";

            _logger.LogInformation("Refund processed: {RefundId} for Payment {PaymentId}", refundId, paymentId);

            return Ok(new RefundResponse
            {
                Success = true,
                RefundId = refundId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return StatusCode(500, new RefundResponse
            {
                Success = false
            });
        }
    }
}

