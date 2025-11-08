using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics;
using TicketManagement.Infrastructure.Observability;

namespace TicketManagement.Infrastructure.DistributedLock;

public class RedisDistributedLock : IDistributedLock
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLock> _logger;
    private readonly IDatabase _database;
    private readonly LockContentionMonitor? _contentionMonitor;
    private const string ReleaseLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    public RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger, LockContentionMonitor? contentionMonitor = null)
    {
        _redis = redis;
        _logger = logger;
        _database = redis.GetDatabase();
        _contentionMonitor = contentionMonitor;
    }

    public async Task<bool> TryAcquireLockAsync(string resourceKey, string lockValue, TimeSpan expiry)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var key = $"lock:{resourceKey}";
            var acquired = await _database.StringSetAsync(
                key,
                lockValue,
                expiry,
                When.NotExists,
                CommandFlags.None);

            if (acquired)
            {
                _logger.LogDebug("Lock acquired: {ResourceKey} in {ElapsedMs}ms", resourceKey, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug("Lock acquisition failed: {ResourceKey} (contention)", resourceKey);
            }

            _contentionMonitor?.RecordLockAttempt(resourceKey, acquired, stopwatch.ElapsedMilliseconds);

            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock: {ResourceKey}", resourceKey);
            return false;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task ReleaseLockAsync(string resourceKey, string lockValue)
    {
        try
        {
            var key = $"lock:{resourceKey}";
            var script = LuaScript.Prepare(ReleaseLockScript);
            await _database.ScriptEvaluateAsync(script, new { keys = new RedisKey[] { key }, values = new RedisValue[] { lockValue } });
            _logger.LogDebug("Lock released: {ResourceKey}", resourceKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock: {ResourceKey}", resourceKey);
        }
    }

    public async Task<T?> ExecuteWithLockAsync<T>(string resourceKey, TimeSpan lockExpiry, Func<Task<T>> action)
    {
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await TryAcquireLockAsync(resourceKey, lockValue, lockExpiry);

        if (!acquired)
        {
            return default;
        }

        try
        {
            return await action();
        }
        finally
        {
            await ReleaseLockAsync(resourceKey, lockValue);
        }
    }
}

