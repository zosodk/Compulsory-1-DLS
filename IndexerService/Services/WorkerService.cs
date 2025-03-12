
namespace IndexerService.Services
{
    public class WorkerService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(IServiceScopeFactory serviceScopeFactory, ILogger<WorkerService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(" WorkerService started, listening for RabbitMQ messages...");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var mailIndexer = scope.ServiceProvider.GetRequiredService<MailIndexer>();

                try
                {
                    mailIndexer.StartListening(); 
                    _logger.LogInformation("MailIndexer is now listening for messages...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(" Error in WorkerService: {Message}", ex.Message);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken); 
            }
        }
    }
}