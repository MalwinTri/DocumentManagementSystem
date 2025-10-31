using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Services;

public class RabbitMqService
{
    private readonly ConnectionFactory _factory;
    private readonly string _queueName = "ocr-queue";
    private readonly ILogger<RabbitMqService> _logger;

    public RabbitMqService(ILogger<RabbitMqService> logger, string hostName = "rabbitmq")
    {
        _factory = new ConnectionFactory { HostName = hostName };
        _logger = logger;
        _logger.LogInformation("RabbitMqService initialized with host {Host}", hostName);
    }

    public void SendOcrMessage(object message)
    {
        try
        {
            _logger.LogDebug("Preparing to send OCR message to queue {Queue}. Payload preview: {PayloadPreview}",
                _queueName, SafePreview(message));

            using var connection = _factory.CreateConnectionAsync().GetAwaiter().GetResult();
            using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

            channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            channel.BasicPublishAsync(exchange: "", routingKey: _queueName, body: body);

            _logger.LogInformation("OCR message published to queue {Queue}", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OCR message to queue {Queue}", _queueName);
        }
    }

    private static string SafePreview(object message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            return json.Length <= 200 ? json : json[..200] + "...";
        }
        catch
        {
            return message?.ToString() ?? "<null>";
        }
    }
}
