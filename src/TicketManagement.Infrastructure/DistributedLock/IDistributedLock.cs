namespace TicketManagement.Infrastructure.DistributedLock;

public interface IDistributedLock
{
    Task<bool> TryAcquireLockAsync(string resourceKey, string lockValue, TimeSpan expiry);
    Task ReleaseLockAsync(string resourceKey, string lockValue);
    Task<T?> ExecuteWithLockAsync<T>(string resourceKey, TimeSpan lockExpiry, Func<Task<T>> action);
}

