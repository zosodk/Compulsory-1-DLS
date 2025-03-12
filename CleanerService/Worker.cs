using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTelemetry.Trace;
using System.Text.RegularExpressions;
using SharedLibrary.Settings;

namespace CleanerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMQSettings _rabbitSettings;
        private readonly MaildirSettings _maildirSettings;
        private readonly Tracer _tracer;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMQSettings> rabbitSettings, IOptions<MaildirSettings> maildirSettings, TracerProvider tracerProvider)
        {
            _logger = logger;
            _rabbitSettings = rabbitSettings.Value;
            _maildirSettings = maildirSettings.Value;
            _tracer = tracerProvider.GetTracer("CleanerService");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CleanerService started");

            var watcher = new FileSystemWatcher(_maildirSettings.Path);
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName; // Active Watch on OS for files and directories
            watcher.Created += OnFileSystemEvent; // Use a single event handler
            watcher.EnableRaisingEvents = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("CleanerService stopped");
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            using var span = _tracer.StartActiveSpan("CleanAndPublishEmail");
            try
            {
                if (File.Exists(e.FullPath) && Regex.IsMatch(Path.GetFileName(e.FullPath), @"^\d+_$")) // Check if file and matches pattern from mails 1_ 2_ 3_ etc.
                {
                    _logger.LogInformation($"File created: {e.FullPath}");
                    string cleanedContent = CleanEmail(e.FullPath);
                    PublishToRabbitMQ(cleanedContent);
                    span.SetAttribute("event", "email published");
                    _logger.LogInformation($"Cleaned and published: {e.FullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing {e.FullPath}");
                span.SetAttribute("error", true);
                span.SetAttribute("error.message", ex.Message);
            }
        }

        private string CleanEmail(string filePath)
        {
            // Regex things from Python And Strip HEaders etc.
            _logger.LogInformation($"Cleaning email: {filePath}");
            return File.ReadAllText(filePath); // Placeholder
        }

        private void PublishToRabbitMQ(string message)
        {
            // ... NEed to setup Rabbit somewhere to push tge cleaned message/mail (RabbitMQ publishing logic)
        }
    }

    // ... Might need some settings classes - those should go here when needed
}