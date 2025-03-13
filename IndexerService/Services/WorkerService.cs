
using Prometheus;

namespace IndexerService.Services
{
    public class WorkerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<WorkerService> _logger;

        private static readonly Counter WorkerRestarts = Metrics.CreateCounter(
            "worker_service_restarts_total", "Total times WorkerService restarted");

        private static readonly Gauge WorkerUptime = Metrics.CreateGauge(
            "worker_service_uptime_seconds", "Time the WorkerService has been running");

        public WorkerService(IServiceScopeFactory serviceScopeFactory, ILogger<WorkerService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(" WorkerService started, listening for RabbitMQ messages...");
            WorkerRestarts.Inc();

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var mailIndexer = scope.ServiceProvider.GetRequiredService<MailIndexer>();

                    try
                    {
                        mailIndexer.StartListening();
                        _logger.LogInformation("MailIndexer is now actively listening for messages...");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in WorkerService. Restarting in 5s...");
                    }
                }

                WorkerUptime.Set(WorkerUptime.Value + 5);
                await Task.Delay(5000, stoppingToken);
            }

            _logger.LogInformation("WorkerService stopping...");
        }
    }
}