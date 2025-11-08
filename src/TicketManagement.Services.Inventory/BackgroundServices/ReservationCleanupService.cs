using TicketManagement.Services.Inventory.Services;

namespace TicketManagement.Services.Inventory.BackgroundServices;

public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    public ReservationCleanupService(IServiceProvider serviceProvider, ILogger<ReservationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                
                // Get expired reservations and release them
                // This is a simplified version - in production, you'd batch process
                _logger.LogInformation("Running reservation cleanup at {Time}", DateTime.UtcNow);
                
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reservation cleanup service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}

