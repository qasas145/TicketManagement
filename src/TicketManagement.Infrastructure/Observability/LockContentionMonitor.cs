using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TicketManagement.Infrastructure.Observability;

public class LockContentionMonitor
{
    private readonly ConcurrentDictionary<string, LockContentionStats> _stats = new();
    private readonly ILogger<LockContentionMonitor> _logger;
    private readonly MetricsCollector _metricsCollector;

    public LockContentionMonitor(ILogger<LockContentionMonitor> logger, MetricsCollector metricsCollector)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public void RecordLockAttempt(string resourceKey, bool acquired, double durationMs)
    {
        var stats = _stats.GetOrAdd(resourceKey, _ => new LockContentionStats());
        
        lock (stats)
        {
            stats.TotalAttempts++;
            stats.TotalDurationMs += durationMs;
            
            if (acquired)
            {
                stats.SuccessfulAttempts++;
            }
            else
            {
                stats.FailedAttempts++;
                if (stats.FailedAttempts % 10 == 0)
                {
                    _logger.LogWarning(
                        "High lock contention detected for {ResourceKey}: {FailedAttempts}/{TotalAttempts} failures",
                        resourceKey, stats.FailedAttempts, stats.TotalAttempts);
                }
            }
            
            stats.AverageDurationMs = stats.TotalDurationMs / stats.TotalAttempts;
        }
    }

    public LockContentionStats GetStats(string resourceKey)
    {
        return _stats.TryGetValue(resourceKey, out var stats) ? stats : new LockContentionStats();
    }

    public Dictionary<string, LockContentionStats> GetAllStats()
    {
        return _stats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void ResetStats()
    {
        _stats.Clear();
    }
}

public class LockContentionStats
{
    public long TotalAttempts { get; set; }
    public long SuccessfulAttempts { get; set; }
    public long FailedAttempts { get; set; }
    public double TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }
    public double ContentionRate => TotalAttempts > 0 ? (double)FailedAttempts / TotalAttempts : 0;
}

