using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;

namespace CleanerService.Services
{
    public class MailCleaner
    {
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly string _rabbitMqHost;
        private readonly ILogger<MailCleaner> _logger;

        public MailCleaner(string inputFolder, string outputFolder, string rabbitMqHost, ILogger<MailCleaner> logger)
        {
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _rabbitMqHost = rabbitMqHost;
            _logger = logger;
        }

        public void ProcessFiles()
        {
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
            
            string[] files = Directory.GetFiles(_inputFolder, "*", SearchOption.AllDirectories);
            _logger.LogInformation(" Found {FileCount} files in {InputFolder}", files.Length, _inputFolder);

            foreach (var filePath in files)
            {
                if (Directory.Exists(filePath))
                {
                    _logger.LogWarning(" Skipping directory: {FilePath}", filePath);
                    continue;
                }

                if (new FileInfo(filePath).Length == 0)
                {
                    _logger.LogWarning(" Skipping empty file: {FilePath}", filePath);
                    continue;
                }

                _logger.LogInformation(" Processing file: {FilePath}", filePath);
                try
                {
                    string cleanedContent = CleanMail(File.ReadAllText(filePath));

                    if (!string.IsNullOrEmpty(cleanedContent))
                    {
                        string relativePath = Path.GetRelativePath(_inputFolder, filePath);
                        string outputFile = Path.Combine(_outputFolder, relativePath + ".txt");
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                        File.WriteAllText(outputFile, cleanedContent);
                        _logger.LogInformation("Cleaned file saved: {OutputFile}", outputFile);
                        
                        PublishToQueue(Path.GetFileName(outputFile), outputFile);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {FilePath}", filePath);
                }
            }
        }

        private string CleanMail(string content)
        {
            string headerRegex = @"^(Message-ID:|Mime-Version:|Content-Type:|Content-Transfer-Encoding:|X-.*?:|From:|To:|Cc:|Bcc:|Subject:|Date:|Received:|Forwarded by|[-]+ Forwarded by).*?\n";
            string cleanedContent = Regex.Replace(content, headerRegex, "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            cleanedContent = Regex.Replace(cleanedContent, @"^\s*\n", "", RegexOptions.Multiline);
            return cleanedContent;
        }

        private void PublishToQueue(string fileName, string filePath)
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = _rabbitMqHost };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "cleaned_emails", durable: false, exclusive: false, autoDelete: false, arguments: null);

                string message = $"{fileName}|{filePath}";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "", routingKey: "cleaned_emails", basicProperties: null, body: body);
                _logger.LogInformation(" Sent to RabbitMQ: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to RabbitMQ");
            }
        }
    }
}
