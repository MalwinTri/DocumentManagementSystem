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

    // Defaults passend zur docker-compose (guest/guest, queue "ocr-queue")
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
            _logger.LogDebug("Publishing to {Queue}: {Preview}", _queueName, SafePreview(json));

            // RabbitMQ.Client 6.6.0: synchrone API
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            // WICHTIG: Keine (Neu-)Deklaration!
            // Nur passiv prüfen – wenn noch nicht vorhanden, loggen wir und publishen trotzdem.
            try
            {
                channel.QueueDeclarePassive(_queueName);
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
            {
                _logger.LogWarning("Queue {Queue} not found yet (likely before worker init). Publishing anyway.", _queueName);
            }

            // optional: Unroutable-Logging (falls Queue wirklich nicht existiert)
            channel.BasicReturn += (_, args) =>
            {
                _logger.LogError(
                    "Message to queue {Queue} was returned: replyCode={Code}, replyText={Text}",
                    _queueName, args.ReplyCode, args.ReplyText);
            };

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";

            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(
                exchange: "",
                routingKey: _queueName,
                mandatory: true,                 // -> BasicReturn wenn nicht zustellbar
                basicProperties: props,
                body: body);

            _logger.LogInformation("OCR message published to queue {Queue}", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OCR message to queue {Queue}", _queueName);
            throw; // weiterwerfen, damit dein Controller sauber reagieren kann
        }
    }

    private static string SafePreview(string json)
        => json.Length <= 200 ? json : json[..200] + "...";
}
