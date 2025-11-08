using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace TicketManagement.Infrastructure.Observability;

public class MetricsCollector
{
    private readonly Meter _meter;
    private readonly Counter<long> _lockAcquisitionAttempts;
    private readonly Counter<long> _lockAcquisitionFailures;
    private readonly Histogram<double> _lockAcquisitionDuration;
    private readonly Counter<long> _reservationAttempts;
    private readonly Counter<long> _reservationSuccesses;
    private readonly Counter<long> _reservationFailures;
    private readonly Histogram<double> _reservationDuration;
    private readonly Counter<long> _bookingAttempts;
    private readonly Counter<long> _bookingSuccesses;
    private readonly Counter<long> _bookingFailures;
    private readonly Histogram<double> _bookingDuration;
    private readonly ILogger<MetricsCollector> _logger;

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger;
        _meter = new Meter("TicketManagement", "1.0.0");
        
        _lockAcquisitionAttempts = _meter.CreateCounter<long>("lock.acquisition.attempts", "count", "Number of lock acquisition attempts");
        _lockAcquisitionFailures = _meter.CreateCounter<long>("lock.acquisition.failures", "count", "Number of failed lock acquisitions");
        _lockAcquisitionDuration = _meter.CreateHistogram<double>("lock.acquisition.duration", "ms", "Lock acquisition duration in milliseconds");
        
        _reservationAttempts = _meter.CreateCounter<long>("reservation.attempts", "count", "Number of reservation attempts");
        _reservationSuccesses = _meter.CreateCounter<long>("reservation.successes", "count", "Number of successful reservations");
        _reservationFailures = _meter.CreateCounter<long>("reservation.failures", "count", "Number of failed reservations");
        _reservationDuration = _meter.CreateHistogram<double>("reservation.duration", "ms", "Reservation duration in milliseconds");
        
        _bookingAttempts = _meter.CreateCounter<long>("booking.attempts", "count", "Number of booking attempts");
        _bookingSuccesses = _meter.CreateCounter<long>("booking.successes", "count", "Number of successful bookings");
        _bookingFailures = _meter.CreateCounter<long>("booking.failures", "count", "Number of failed bookings");
        _bookingDuration = _meter.CreateHistogram<double>("booking.duration", "ms", "Booking duration in milliseconds");
    }

    public void RecordLockAcquisitionAttempt(string resourceKey)
    {
        _lockAcquisitionAttempts.Add(1, new KeyValuePair<string, object?>("resource", resourceKey));
    }

    public void RecordLockAcquisitionFailure(string resourceKey, double durationMs)
    {
        _lockAcquisitionFailures.Add(1, new KeyValuePair<string, object?>("resource", resourceKey));
        _lockAcquisitionDuration.Record(durationMs, new KeyValuePair<string, object?>("resource", resourceKey));
    }

    public void RecordLockAcquisitionSuccess(string resourceKey, double durationMs)
    {
        _lockAcquisitionDuration.Record(durationMs, new KeyValuePair<string, object?>("resource", resourceKey));
    }

    public void RecordReservationAttempt(long eventId)
    {
        _reservationAttempts.Add(1, new KeyValuePair<string, object?>("event_id", eventId));
    }

    public void RecordReservationSuccess(long eventId, double durationMs)
    {
        _reservationSuccesses.Add(1, new KeyValuePair<string, object?>("event_id", eventId));
        _reservationDuration.Record(durationMs, new KeyValuePair<string, object?>("event_id", eventId));
    }

    public void RecordReservationFailure(long eventId, double durationMs)
    {
        _reservationFailures.Add(1, new KeyValuePair<string, object?>("event_id", eventId));
        _reservationDuration.Record(durationMs, new KeyValuePair<string, object?>("event_id", eventId));
    }

    public void RecordBookingAttempt(string userId)
    {
        _bookingAttempts.Add(1, new KeyValuePair<string, object?>("user_id", userId));
    }

    public void RecordBookingSuccess(string userId, double durationMs)
    {
        _bookingSuccesses.Add(1, new KeyValuePair<string, object?>("user_id", userId));
        _bookingDuration.Record(durationMs, new KeyValuePair<string, object?>("user_id", userId));
    }

    public void RecordBookingFailure(string userId, double durationMs)
    {
        _bookingFailures.Add(1, new KeyValuePair<string, object?>("user_id", userId));
        _bookingDuration.Record(durationMs, new KeyValuePair<string, object?>("user_id", userId));
    }
}

