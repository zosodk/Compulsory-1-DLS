
using Polly;
using Polly.CircuitBreaker;
using Prometheus;

namespace IndexerService.Services
{
    public class WorkerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<WorkerService> _logger;
        private readonly IAsyncPolicy _rabbitMqResiliencePolicy;

        private static readonly Counter WorkerRestarts = Metrics.CreateCounter(
            "worker_service_restarts_total", "Total times WorkerService restarted");

        private static readonly Gauge WorkerUptime = Metrics.CreateGauge(
            "worker_service_uptime_seconds", "Time the WorkerService has been running");

        public WorkerService(IServiceScopeFactory serviceScopeFactory, ILogger<WorkerService> logger, IAsyncPolicy rabbitMqResiliencePolicy)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _rabbitMqResiliencePolicy = rabbitMqResiliencePolicy;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerService started, listening for RabbitMQ messages...");
            WorkerRestarts.Inc();

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var mailIndexer = scope.ServiceProvider.GetRequiredService<MailIndexer>();

                    try
                    {
                       
                        await _rabbitMqResiliencePolicy.ExecuteAsync(async () =>
                        {
                            mailIndexer.StartListening();
                            _logger.LogInformation("MailIndexer is now actively listening for messages...");
                        });
                    }
                    catch (BrokenCircuitException)
                    {
                        _logger.LogWarning("Circuit breaker is OPEN! Skipping restart for now...");
                        await Task.Delay(60000, stoppingToken); 
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in WorkerService. Restarting in 10s...");
                        await Task.Delay(10000, stoppingToken); 
                    }
                }

                WorkerUptime.Set(WorkerUptime.Value + 5);
                await Task.Delay(5000, stoppingToken);
            }

            _logger.LogInformation("WorkerService stopping...");
        }
    }
}