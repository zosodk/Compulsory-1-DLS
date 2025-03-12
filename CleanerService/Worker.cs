using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WorkerShares.Settings;
using WorkerShares.Interfaces;
using RabbitMQ.Client;

namespace CleanerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly MaildirSettings _maildirSettings;
    private readonly Tracer _tracer;
    private readonly IConnectionWrapper _connectionWrapper;
    private readonly IModelWrapper _modelWrapper;

    public Worker(ILogger<Worker> logger, IOptions<RabbitMQSettings> rabbitSettings, IOptions<MaildirSettings> maildirSettings, TracerProvider tracerProvider, IConnectionWrapper connectionWrapper, IModelWrapper modelWrapper)
    {
        _logger = logger;
        _rabbitSettings = rabbitSettings.Value;
        _maildirSettings = maildirSettings.Value;
        _tracer = tracerProvider.GetTracer("CleanerService");
        _connectionWrapper = connectionWrapper;
        _modelWrapper = modelWrapper;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            _modelWrapper.QueueDeclare(queue: _rabbitSettings.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing RabbitMQ");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanerService started");

        var watcher = new FileSystemWatcher(_maildirSettings.Path);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.Created += OnFileSystemEvent;
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
            if (File.Exists(e.FullPath) && Regex.IsMatch(Path.GetFileName(e.FullPath), @"^\d+_$"))
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
        _logger.LogInformation($"Cleaning email: {filePath}");
        try
        {
            string emailContent = File.ReadAllText(filePath);
            emailContent = Regex.Replace(emailContent, @"^[\w\-]+:.*?\r?\n", "", RegexOptions.Multiline);
            return emailContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cleaning email: {filePath}");
            return string.Empty;
        }
    }

    private void PublishToRabbitMQ(string message)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(message);
            _modelWrapper.BasicPublish(exchange: "", routingKey: _rabbitSettings.QueueName, basicProperties: null, body: body);
            _logger.LogInformation("Message published to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to RabbitMQ");
        }
    }

    public override void Dispose()
    {
        _modelWrapper?.Dispose();
        _connectionWrapper?.Dispose();
        base.Dispose();
    }
}