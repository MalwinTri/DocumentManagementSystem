using System.Text;
using System.Text.Json;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.OCR_Worker.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.OCR_Worker.Worker;

public class RabbitConsumerService : BackgroundService
{
    private readonly ILogger<RabbitConsumerService> _log;
    private readonly OcrJobHandler _handler;
    private readonly RabbitOptions _opts;
    private IConnection? _conn;
    private IModel? _ch;

    public RabbitConsumerService(ILogger<RabbitConsumerService> log, OcrJobHandler handler, IOptions<RabbitOptions> opts)
        => (_log, _handler, _opts) = (log, handler, opts.Value);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opts.Host,
            UserName = _opts.User,
            Password = _opts.Pass,
            DispatchConsumersAsync = true
        };
        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();

        _ch.QueueDeclare(_opts.Queue, durable: true, exclusive: false, autoDelete: false);
        _ch.BasicQos(0, _opts.Prefetch, global: false);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_ch!);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
                var job = JsonSerializer.Deserialize<OcrJob>(msg, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                          ?? throw new InvalidOperationException("Invalid OCR job payload");

                await _handler.HandleAsync(job, stoppingToken);
                _ch!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OCR processing failed");
                _ch!.BasicNack(ea.DeliveryTag, multiple: false, requeue: _opts.RequeueOnError);
            }
        };

        _ch!.BasicConsume(queue: _opts.Queue, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _ch?.Close();
        _conn?.Close();
        base.Dispose();
    }
}
