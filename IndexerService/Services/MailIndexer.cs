using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary;
using SharedLibrary.Models;
using System.Text;
using Polly;
using Prometheus;
using System.Text.RegularExpressions;

namespace IndexerService.Services
{
    public class MailIndexer
    {
        private readonly string _rabbitMqHost;
        private readonly DbContextConfig _dbContext;
        private readonly ILogger<MailIndexer> _logger;
        private readonly IAsyncPolicy _rabbitMqResiliencePolicy;
        private readonly IAsyncPolicy _databaseResiliencePolicy;

        private static readonly Counter MessagesReceived = Metrics.CreateCounter(
            "indexer_rabbitmq_messages_received_total", "Total messages received from RabbitMQ");

        private static readonly Counter MessagesFailed = Metrics.CreateCounter(
            "indexer_rabbitmq_messages_failed_total", "Total RabbitMQ messages failed to process");

        private static readonly Counter FilesSavedToDB = Metrics.CreateCounter(
            "indexer_files_saved_total", "Total files successfully saved to PostgreSQL");

        private static readonly Histogram MessageProcessingTime = Metrics.CreateHistogram(
            "indexer_message_processing_seconds", "Histogram of message processing times");


        public MailIndexer(IConfiguration configuration, DbContextConfig dbContext,
            ILogger<MailIndexer> logger,
            IAsyncPolicy rabbitMqResiliencePolicy,
            IAsyncPolicy databaseResiliencePolicy)
        {
            _rabbitMqHost = configuration.GetValue<string>("RABBITMQ_HOST", "rabbitmq");
            _dbContext = dbContext;
            _logger = logger;
            _rabbitMqResiliencePolicy = rabbitMqResiliencePolicy;
            _databaseResiliencePolicy = databaseResiliencePolicy;

            _logger.LogInformation(" RabbitMQ host set to {RabbitMqHost}", _rabbitMqHost);
            _logger.LogInformation("MailIndexer initialized with PostgreSQL");
        }

        public void StartListening()
        {
            _rabbitMqResiliencePolicy.ExecuteAsync(async () =>
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
                                        _logger.LogWarning("Skipping non-existent file: {FilePath}", filePath);
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
                    _logger.LogInformation("Listening for messages on queue: cleaned_emails");

                    while (true) { await Task.Delay(1000); }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while connecting to RabbitMQ");
                }
            }).Wait();
        }

        private void SaveToDatabase(string fileName, string filePath, string content)
        {
            _databaseResiliencePolicy.ExecuteAsync(async () =>
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

                    var wordCounts = CountWords(content);

                    foreach (var wordCount in wordCounts)
                    {
                        var wordText = wordCount.Key;
                        var count = wordCount.Value;

                        var word = _dbContext.Words.FirstOrDefault(w => w.WordText == wordText);
                        if (word == null)
                        {
                            word = new Word { WordText = wordText };
                            _dbContext.Words.Add(word);
                            _dbContext.SaveChanges();
                        }

                        var occurrence = new Occurrence
                        {
                            WordId = word.WordId,
                            FileId = fileEntity.FileId,
                            Count = count
                        };

                        _dbContext.Occurrences.Add(occurrence);
                    }

                    _dbContext.SaveChanges();
                    FilesSavedToDB.Inc();
                    _logger.LogInformation("Saved file and word occurrences to PostgreSQL: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save file {FileName} to database", fileName);
                    MessagesFailed.Inc();
                }
            }).Wait();
        }

        private Dictionary<string, int> CountWords(string content)
        {
            var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var words = Regex.Matches(content, @"\b\w+\b");

            foreach (Match match in words)
            {
                var word = match.Value;
                if (wordCounts.ContainsKey(word))
                {
                    wordCounts[word]++;
                }
                else
                {
                    wordCounts[word] = 1;
                }
            }

            return wordCounts;
        }
    }
}
