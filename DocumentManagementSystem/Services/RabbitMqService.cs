using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace DocumentManagementSystem.Services;

public class RabbitMqService
{
    private readonly ConnectionFactory _factory;
    private readonly string _queueName = "ocr-queue";

    public RabbitMqService(string hostName = "rabbitmq")
    {
        _factory = new ConnectionFactory() { HostName = hostName };
    }

    public void SendOcrMessage(object message)
    {
        using var connection = _factory.CreateConnectionAsync().GetAwaiter().GetResult();
        using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublishAsync(exchange: "", routingKey: _queueName, body: body);
    }
}