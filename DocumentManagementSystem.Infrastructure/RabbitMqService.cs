using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Services;

public class RabbitMqService
{
    private readonly ILogger<RabbitMqService> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _queueName;

    // Defaults passen zu deiner docker-compose (guest/guest, queue "ocr-queue")
    public RabbitMqService(
        ILogger<RabbitMqService> logger,
        string hostName = "rabbitmq",
        string userName = "guest",
        string password = "guest",
        string queueName = "ocr-queue")
    {
        _logger = logger;
        _queueName = queueName;
        _factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };
        _logger.LogInformation("RabbitMqService initialized with host {Host}", hostName);
    }

    public void SendOcrMessage(object message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            _logger.LogDebug("Publishing to {Queue}: {Preview}",
                _queueName, SafePreview(json));

            // 6.6.0: synchrone API -> kein 'await' / keine Tasks, zuverlässig
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;                 // Message persistent
            props.ContentType = "application/json";   // nice-to-have

            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(
                exchange: "",
                routingKey: _queueName,
                basicProperties: props,
                body: body);

            _logger.LogInformation("OCR message published to queue {Queue}", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OCR message to queue {Queue}", _queueName);
        }
    }

    private static string SafePreview(string json)
        => json.Length <= 200 ? json : json[..200] + "...";
}
