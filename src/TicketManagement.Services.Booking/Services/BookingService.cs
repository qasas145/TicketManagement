using System.Diagnostics;
using System.Text;
using TicketManagement.Infrastructure.DistributedLock;
using TicketManagement.Infrastructure.Observability;
using TicketManagement.Services.Booking.Clients;
using TicketManagement.Services.Booking.Data;
using TicketManagement.Services.Booking.DTOs;
using TicketManagement.Services.Booking.Entities;
using TicketManagement.Services.Booking.Repositories;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Booking.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingSeatRepository _bookingSeatRepository;
    private readonly IIdempotencyKeyRepository _idempotencyKeyRepository;
    private readonly IInventoryServiceClient _inventoryServiceClient;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IDistributedLock _distributedLock;
    private readonly MetricsCollector _metricsCollector;
    private readonly ILogger<BookingService> _logger;
    private const int LockTimeoutSeconds = 30;

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingSeatRepository bookingSeatRepository,
        IIdempotencyKeyRepository idempotencyKeyRepository,
        IInventoryServiceClient inventoryServiceClient,
        IPaymentServiceClient paymentServiceClient,
        IDistributedLock distributedLock,
        MetricsCollector metricsCollector,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _bookingSeatRepository = bookingSeatRepository;
        _idempotencyKeyRepository = idempotencyKeyRepository;
        _inventoryServiceClient = inventoryServiceClient;
        _paymentServiceClient = paymentServiceClient;
        _distributedLock = distributedLock;
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<BookingResponse> ConfirmBookingAsync(ConfirmBookingRequest request, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivityExtensions.StartBookingActivity("ConfirmBooking");
        activity?.SetTag("user_id", userId);
        activity?.SetTag("reservation_id", request.ReservationId);

        _metricsCollector.RecordBookingAttempt(userId);

        try
        {
            // Check idempotency
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existingKey = await _idempotencyKeyRepository.GetByKeyAsync(request.IdempotencyKey);
                if (existingKey != null && existingKey.BookingId.HasValue)
                {
                    var existingBooking = await _bookingRepository.GetByIdAsync(existingKey.BookingId.Value);
                    if (existingBooking != null)
                    {
                        stopwatch.Stop();
                        _metricsCollector.RecordBookingSuccess(userId, stopwatch.ElapsedMilliseconds);
                        return await MapToBookingResponseAsync(existingBooking);
                    }
                }
            }

            // Get reservation from inventory service
            var reservation = await _inventoryServiceClient.GetReservationAsync(request.ReservationId);
            if (reservation == null)
            {
                throw new InvalidOperationException("Reservation not found");
            }

            if (reservation.Status != Clients.ReservationStatus.Active)
            {
                throw new InvalidOperationException("Reservation is not active");
            }

            if (DateTime.UtcNow > reservation.ExpiresAt)
            {
                throw new InvalidOperationException("Reservation expired. Please select seats again.");
            }

            if (reservation.UserId != userId)
            {
                throw new UnauthorizedAccessException("Reservation belongs to another user");
            }

            // Acquire lock for the seat
            var lockKey = $"seat:{reservation.EventId}:{reservation.SeatId}";
            var lockValue = Guid.NewGuid().ToString();
            var acquired = await _distributedLock.TryAcquireLockAsync(
                lockKey,
                lockValue,
                TimeSpan.FromSeconds(LockTimeoutSeconds));

            if (!acquired)
            {
                throw new InvalidOperationException("Seat is being processed");
            }

            try
            {
                // Process payment
                var paymentResponse = await _paymentServiceClient.ProcessPaymentAsync(new PaymentRequestDto
                {
                    CardNumber = request.Payment.CardNumber,
                    CardHolderName = request.Payment.CardHolderName,
                    ExpiryDate = request.Payment.ExpiryDate,
                    Cvv = request.Payment.Cvv,
                    Amount = request.Payment.Amount
                });

                if (!paymentResponse.Success)
                {
                    throw new InvalidOperationException($"Payment failed: {paymentResponse.ErrorMessage}");
                }

                // Get seat details
                var seat = await _inventoryServiceClient.GetSeatAsync(reservation.EventId, reservation.SeatId);
                if (seat == null)
                {
                    throw new InvalidOperationException("Seat not found");
                }

                if (seat.Status != SeatStatus.Reserved || seat.ReservedBy != userId)
                {
                    // Compensating transaction: refund payment
                    await _paymentServiceClient.RefundPaymentAsync(paymentResponse.PaymentId);
                    throw new InvalidOperationException("Seat state changed");
                }

                // Create booking
                var booking = new Entities.Booking
                {
                    EventId = reservation.EventId,
                    UserId = userId,
                    TotalAmount = seat.Price,
                    Status = BookingStatus.Confirmed,
                    PaymentId = paymentResponse.PaymentId,
                    PaymentStatus = PaymentStatus.Success,
                    BookingReference = GenerateBookingReference(),
                    ConfirmedAt = DateTime.UtcNow
                };

                booking = await _bookingRepository.AddAsync(booking);

                // Link seat to booking
                var bookingSeat = new BookingSeat
                {
                    BookingId = booking.BookingId,
                    SeatId = seat.SeatId,
                    Price = seat.Price
                };
                await _bookingSeatRepository.AddAsync(bookingSeat);

                // Update seat status in inventory service
                await _inventoryServiceClient.ConfirmReservationAsync(reservation.ReservationId, booking.BookingId);

                // Store idempotency key if provided
                if (!string.IsNullOrEmpty(request.IdempotencyKey))
                {
                    var idempotencyKey = new IdempotencyKey
                    {
                        Key = request.IdempotencyKey,
                        BookingId = booking.BookingId,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    };
                    await _idempotencyKeyRepository.AddAsync(idempotencyKey);
                }

                stopwatch.Stop();
                _metricsCollector.RecordBookingSuccess(userId, stopwatch.ElapsedMilliseconds);
                activity?.SetTag("booking_id", booking.BookingId);

                return await MapToBookingResponseAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking for reservation {ReservationId}", request.ReservationId);
                stopwatch.Stop();
                _metricsCollector.RecordBookingFailure(userId, stopwatch.ElapsedMilliseconds);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                await _distributedLock.ReleaseLockAsync(lockKey, lockValue);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsCollector.RecordBookingFailure(userId, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Error in booking confirmation");
            throw;
        }
    }

    public async Task<List<BookingResponse>> GetUserBookingsAsync(string userId)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        var result = new List<BookingResponse>();

        foreach (var booking in bookings)
        {
            result.Add(await MapToBookingResponseAsync(booking));
        }

        return result;
    }

    public async Task<BookingResponse?> GetBookingByReferenceAsync(string bookingReference)
    {
        var booking = await _bookingRepository.GetByReferenceAsync(bookingReference);
        if (booking == null)
        {
            return null;
        }

        return await MapToBookingResponseAsync(booking);
    }

    private async Task<BookingResponse> MapToBookingResponseAsync(Entities.Booking booking)
    {
        var bookingSeats = await _bookingSeatRepository.GetByBookingIdAsync(booking.BookingId);
        var seatDtos = new List<BookingSeatDto>();

        foreach (var bookingSeat in bookingSeats)
        {
            // Get seat details from inventory service
            var seat = await _inventoryServiceClient.GetSeatByIdAsync(bookingSeat.SeatId);
            seatDtos.Add(new BookingSeatDto
            {
                SeatId = bookingSeat.SeatId,
                SeatNumber = seat?.SeatNumber ?? "Unknown",
                Price = bookingSeat.Price
            });
        }

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            BookingReference = booking.BookingReference,
            EventId = booking.EventId,
            TotalAmount = booking.TotalAmount,
            Status = booking.Status,
            PaymentStatus = booking.PaymentStatus,
            CreatedAt = booking.CreatedAt,
            ConfirmedAt = booking.ConfirmedAt,
            Seats = seatDtos
        };
    }

    private string GenerateBookingReference()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"BK-{timestamp}-{random}";
    }
}

