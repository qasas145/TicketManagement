using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure.Observability;

namespace TicketManagement.Services.Inventory.Controllers;

[ApiController]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly LockContentionMonitor _contentionMonitor;
    private readonly MetricsCollector _metricsCollector;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        LockContentionMonitor contentionMonitor,
        MetricsCollector metricsCollector,
        ILogger<MonitoringController> logger)
    {
        _contentionMonitor = contentionMonitor;
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    [HttpGet("lock-contention")]
    public ActionResult<Dictionary<string, LockContentionStats>> GetLockContentionStats()
    {
        var stats = _contentionMonitor.GetAllStats();
        return Ok(stats);
    }

    [HttpGet("lock-contention/{resourceKey}")]
    public ActionResult<LockContentionStats> GetLockContentionStats(string resourceKey)
    {
        var stats = _contentionMonitor.GetStats(resourceKey);
        return Ok(stats);
    }

    [HttpPost("lock-contention/reset")]
    public ActionResult ResetLockContentionStats()
    {
        _contentionMonitor.ResetStats();
        return Ok(new { message = "Lock contention stats reset" });
    }
}

