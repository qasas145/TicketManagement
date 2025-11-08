using System.Diagnostics;

namespace TicketManagement.Infrastructure.Observability;

public static class ActivityExtensions
{
    private static readonly ActivitySource ActivitySource = new("TicketManagement");

    public static Activity? StartReservationActivity(string operationName)
    {
        return ActivitySource.StartActivity(operationName);
    }

    public static Activity? StartBookingActivity(string operationName)
    {
        return ActivitySource.StartActivity(operationName);
    }

    public static Activity? StartLockActivity(string operationName)
    {
        return ActivitySource.StartActivity(operationName);
    }
}

