using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary;
using SharedLibrary.Models;
using System.Text;
using Polly;
using Prometheus;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

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

                    channel.QueueDeclare(queue: "cleaned_emails", durable: true, exclusive: false, autoDelete: false,
                        arguments: null);
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

                    while (true)
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while connecting to RabbitMQ");
                }
            }).Wait();
        }

        private async Task SaveToDatabase(string fileName, string filePath, string content)
        {
            await _databaseResiliencePolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var fileEntity = new FileEntity
                    {
                        FileName = fileName,
                        Content = Encoding.UTF8.GetBytes(content)
                    };

                    _dbContext.Files.Add(fileEntity);
                    await _dbContext.SaveChangesAsync(); 
                    
                    var wordCounts = CountWords(content);
                    var wordTexts = wordCounts.Keys.ToList();
                    
                    var existingWords = await _dbContext.Words
                        .Where(w => wordTexts.Contains(w.WordText))
                        .ToDictionaryAsync(w => w.WordText, w => w);

                    var newWords = new List<Word>();
                    var occurrences = new List<Occurrence>();

                    foreach (var (wordText, count) in wordCounts)
                    {
                        if (!existingWords.TryGetValue(wordText, out var word))
                        {
                            word = new Word { WordText = wordText };
                            newWords.Add(word);
                            existingWords[wordText] = word;
                        }

                        occurrences.Add(new Occurrence
                        {
                            Word = word,
                            FileId = fileEntity.FileId,
                            Count = count
                        });
                    }
                    
                    if (newWords.Any())
                    {
                        await _dbContext.Words.AddRangeAsync(newWords);
                        await _dbContext.SaveChangesAsync(); 
                    }

                    if (occurrences.Any())
                    {
                        await _dbContext.Occurrences.AddRangeAsync(occurrences);
                        await _dbContext.SaveChangesAsync();
                    }
                    FilesSavedToDB.Inc();
                    _logger.LogInformation("Saved file and word occurrences to PostgreSQL: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save file {FileName} to database", fileName);
                    MessagesFailed.Inc();
                }
            });
        }


        private Dictionary<string, int> CountWords(string content)
        {
            var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); 
            var words = Regex.Matches(content,
                @"\b[a-zA-Z']+\b"); 

            foreach (Match match in words)
            {
                string word = match.Value.ToLower(); 
                wordCounts[word] = wordCounts.TryGetValue(word, out int count) ? count + 1 : 1;
            }

            return wordCounts;
        }
    }
}
