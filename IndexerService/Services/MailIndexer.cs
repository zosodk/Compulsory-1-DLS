using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary;
using SharedLibrary.Models;
using System.Text;

namespace IndexerService.Services
{
    public class MailIndexer
    {
        private readonly string _rabbitMqHost;
        private readonly DbContextConfig _dbContext;
        private readonly ILogger<MailIndexer> _logger;

        public MailIndexer(IConfiguration configuration, DbContextConfig dbContext, ILogger<MailIndexer> logger)
        {
            _rabbitMqHost = configuration.GetValue<string>("RABBITMQ_HOST", "localhost"); 
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

                channel.QueueDeclare(queue: "cleaned_emails", durable: false, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation(" Received message: {Message}", message);

                    var parts = message.Split('|');
                    if (parts.Length == 2)
                    {
                        string fileName = parts[0];
                        string filePath = parts[1];

                        try
                        {
                            string content = File.ReadAllText(filePath);
                            SaveToDatabase(fileName, filePath, content);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, " Failed to read file: {FilePath}", filePath);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(" Received malformed message: {Message}", message);
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
                _logger.LogInformation(" Saved file to PostgreSQL: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file {FileName} to database", fileName);
            }
        }
    }
}
