using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TicketManagement.Infrastructure.DistributedLock;
using TicketManagement.Infrastructure.Observability;
using TicketManagement.Services.Inventory.DTOs;
using TicketManagement.Services.Inventory.Entities;
using TicketManagement.Services.Inventory.Repositories;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly ISeatRepository _seatRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly MetricsCollector _metricsCollector;
    private readonly ILogger<InventoryService> _logger;
    private const int ReservationTimeoutMinutes = 10;
    private const int LockTimeoutSeconds = 30;

    public InventoryService(
        ISeatRepository seatRepository,
        IReservationRepository reservationRepository,
        IDistributedLock distributedLock,
        MetricsCollector metricsCollector,
        ILogger<InventoryService> logger)
    {
        _seatRepository = seatRepository;
        _reservationRepository = reservationRepository;
        _distributedLock = distributedLock;
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<ReservationResponse> ReserveSeatsAsync(ReserveSeatsRequest request, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = ActivityExtensions.StartReservationActivity("ReserveSeats");
        activity?.SetTag("event_id", request.EventId);
        activity?.SetTag("seat_count", request.SeatNumbers.Count);
        activity?.SetTag("user_id", userId);

        _metricsCollector.RecordReservationAttempt(request.EventId);

        try
        {
            // Sort seat numbers to prevent deadlock (always lock in same order)
            var sortedSeats = request.SeatNumbers.OrderBy(s => s).ToList();
            var lockKeys = sortedSeats.Select(seat => $"seat:{request.EventId}:{seat}").ToList();
            var lockValue = Guid.NewGuid().ToString();
            var acquiredLocks = new List<string>();

            try
            {
                // Acquire all locks (for multiple seats)
                foreach (var lockKey in lockKeys)
                {
                    _metricsCollector.RecordLockAcquisitionAttempt(lockKey);
                    var lockStart = Stopwatch.StartNew();
                    var acquired = await _distributedLock.TryAcquireLockAsync(
                        lockKey,
                        lockValue,
                        TimeSpan.FromSeconds(LockTimeoutSeconds));

                    lockStart.Stop();

                    if (!acquired)
                    {
                        _metricsCollector.RecordLockAcquisitionFailure(lockKey, lockStart.ElapsedMilliseconds);
                        // Failed to acquire all locks, release what we have
                        await ReleaseLocksAsync(acquiredLocks, lockValue);
                        _metricsCollector.RecordReservationFailure(request.EventId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException("One or more seats are being processed. Please try again.");
                    }

                    _metricsCollector.RecordLockAcquisitionSuccess(lockKey, lockStart.ElapsedMilliseconds);
                    acquiredLocks.Add(lockKey);
                }

                // All locks acquired! Proceed with reservation
                var reservedUntil = DateTime.UtcNow.AddMinutes(ReservationTimeoutMinutes);
                var reservedSeats = new List<Seat>();
                var reservations = new List<Reservation>();

                foreach (var seatNumber in sortedSeats)
                {
                    var seat = await _seatRepository.GetByEventIdAndSeatNumberAsync(request.EventId, seatNumber);

                    if (seat == null)
                    {
                        await ReleaseLocksAsync(acquiredLocks, lockValue);
                        _metricsCollector.RecordReservationFailure(request.EventId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException($"Seat not found: {seatNumber}");
                    }

                    if (seat.Status != SeatStatus.Available)
                    {
                        await ReleaseLocksAsync(acquiredLocks, lockValue);
                        _metricsCollector.RecordReservationFailure(request.EventId, stopwatch.ElapsedMilliseconds);
                        throw new InvalidOperationException($"Seat {seatNumber} is no longer available");
                    }

                    // Reserve the seat
                    seat.Status = SeatStatus.Reserved;
                    seat.ReservedBy = userId;
                    seat.ReservedUntil = reservedUntil;
                    seat = await _seatRepository.UpdateAsync(seat);
                    reservedSeats.Add(seat);

                    // Create reservation record
                    var reservation = new Reservation
                    {
                        SeatId = seat.SeatId,
                        EventId = request.EventId,
                        UserId = userId,
                        ExpiresAt = reservedUntil,
                        Status = ReservationStatus.Active
                    };
                    reservation = await _reservationRepository.AddAsync(reservation);
                    reservations.Add(reservation);
                }

                stopwatch.Stop();
                _metricsCollector.RecordReservationSuccess(request.EventId, stopwatch.ElapsedMilliseconds);
                activity?.SetTag("reservation_count", reservations.Count);

                return new ReservationResponse
                {
                    Reservations = reservations.Select(r => new ReservationDto
                    {
                        ReservationId = r.ReservationId,
                        SeatId = r.SeatId,
                        SeatNumber = reservedSeats.First(s => s.SeatId == r.SeatId).SeatNumber,
                        EventId = r.EventId,
                        ExpiresAt = r.ExpiresAt
                    }).ToList(),
                    ExpiresAt = reservedUntil
                };
            }
            finally
            {
                // Always release locks
                await ReleaseLocksAsync(acquiredLocks, lockValue);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsCollector.RecordReservationFailure(request.EventId, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Error reserving seats for event {EventId}", request.EventId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> ReleaseExpiredReservationAsync(long reservationId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null || reservation.Status != ReservationStatus.Active)
        {
            return false;
        }

        if (DateTime.UtcNow <= reservation.ExpiresAt)
        {
            return false;
        }

        var lockKey = $"seat:{reservation.EventId}:{reservation.SeatId}";
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await _distributedLock.TryAcquireLockAsync(
            lockKey,
            lockValue,
            TimeSpan.FromSeconds(5));

        if (!acquired)
        {
            return false;
        }

        try
        {
            var seat = await _seatRepository.GetByIdAsync(reservation.SeatId);
            if (seat != null && seat.Status == SeatStatus.Reserved &&
                DateTime.UtcNow > seat.ReservedUntil)
            {
                seat.Status = SeatStatus.Available;
                seat.ReservedBy = null;
                seat.ReservedUntil = null;
                await _seatRepository.UpdateAsync(seat);

                reservation.Status = ReservationStatus.Expired;
                await _reservationRepository.UpdateAsync(reservation);
                return true;
            }
        }
        finally
        {
            await _distributedLock.ReleaseLockAsync(lockKey, lockValue);
        }

        return false;
    }

    public async Task<List<SeatDto>> GetAvailableSeatsAsync(long eventId)
    {
        var seats = await _seatRepository.GetAvailableSeatsByEventIdAsync(eventId);
        return seats.Select(s => new SeatDto
        {
            SeatId = s.SeatId,
            EventId = s.EventId,
            SeatNumber = s.SeatNumber,
            Section = s.Section,
            RowNumber = s.RowNumber,
            SeatType = s.SeatType,
            Price = s.Price,
            Status = s.Status
        }).ToList();
    }

    public async Task<SeatDto?> GetSeatAsync(long eventId, string seatNumber)
    {
        var seat = await _seatRepository.GetByEventIdAndSeatNumberAsync(eventId, seatNumber);
        if (seat == null)
        {
            return null;
        }

        return new SeatDto
        {
            SeatId = seat.SeatId,
            EventId = seat.EventId,
            SeatNumber = seat.SeatNumber,
            Section = seat.Section,
            RowNumber = seat.RowNumber,
            SeatType = seat.SeatType,
            Price = seat.Price,
            Status = seat.Status
        };
    }

    private async Task ReleaseLocksAsync(List<string> lockKeys, string lockValue)
    {
        foreach (var lockKey in lockKeys)
        {
            await _distributedLock.ReleaseLockAsync(lockKey, lockValue);
        }
    }
}

