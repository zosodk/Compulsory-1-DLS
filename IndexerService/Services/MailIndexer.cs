using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary;
using SharedLibrary.Models;
using System.Text;
using Prometheus;

namespace IndexerService.Services
{
    public class MailIndexer
    {
        private readonly string _rabbitMqHost;
        private readonly DbContextConfig _dbContext;
        private readonly ILogger<MailIndexer> _logger;
        
        private static readonly Counter MessagesReceived = Metrics.CreateCounter(
            "indexer_rabbitmq_messages_received_total", "Total messages received from RabbitMQ");

        private static readonly Counter MessagesFailed = Metrics.CreateCounter(
            "indexer_rabbitmq_messages_failed_total", "Total RabbitMQ messages failed to process");

        private static readonly Counter FilesSavedToDB = Metrics.CreateCounter(
            "indexer_files_saved_total", "Total files successfully saved to PostgreSQL");

        private static readonly Histogram MessageProcessingTime = Metrics.CreateHistogram(
            "indexer_message_processing_seconds", "Histogram of message processing times");


        public MailIndexer(IConfiguration configuration, DbContextConfig dbContext, ILogger<MailIndexer> logger)
        {
            _rabbitMqHost = configuration.GetValue<string>("RABBITMQ_HOST", "rabbitmq"); 
            _dbContext = dbContext;
            _logger = logger;

            _logger.LogInformation(" RabbitMQ host set to {RabbitMqHost}", _rabbitMqHost);
            _logger.LogInformation("MailIndexer initialized with PostgreSQL");
        }

      public void StartListening()
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = _rabbitMqHost };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "cleaned_emails", durable: true, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    using (var timer = MessageProcessingTime.NewTimer()) 
                    {
                        try
                        {
                            MessagesReceived.Inc();
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            _logger.LogInformation("Received message: {Message}", message);

                            var parts = message.Split('|');
                            if (parts.Length == 2)
                            {
                                string fileName = parts[0];
                                string filePath = parts[1];

                                if (!File.Exists(filePath))
                                {
                                    _logger.LogWarning("skipping non-existent file: {FilePath}", filePath);
                                    MessagesFailed.Inc();
                                    return;
                                }

                                string content = File.ReadAllText(filePath);
                                SaveToDatabase(fileName, filePath, content);
                            }
                            else
                            {
                                _logger.LogWarning("Received malformed message: {Message}", message);
                                MessagesFailed.Inc();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing RabbitMQ message");
                            MessagesFailed.Inc();
                        }
                    }
                };

                channel.BasicConsume(queue: "cleaned_emails", autoAck: true, consumer: consumer);
                _logger.LogInformation(" Listening for messages on queue: cleaned_emails");

                while (true) { Thread.Sleep(1000); }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error while connecting to RabbitMQ");
            }
        }


        private void SaveToDatabase(string fileName, string filePath, string content)
        {
            try
            {
                var fileEntity = new FileEntity
                {
                    FileName = fileName,
                    Content = Encoding.UTF8.GetBytes(content)
                };

                _dbContext.Files.Add(fileEntity);
                _dbContext.SaveChanges();
                 FilesSavedToDB.Inc();
                _logger.LogInformation(" Saved file to PostgreSQL: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file {FileName} to database", fileName);
                MessagesFailed.Inc();
            }
        }
    }
}
