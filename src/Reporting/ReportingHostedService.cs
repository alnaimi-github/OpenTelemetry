using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Reporting;

public class ReportingHostedService : BackgroundService
{
    private readonly ILogger<ReportingHostedService> _logger;
    private readonly int _batchSize;
    private readonly string _queueName;
    private readonly ConcurrentBag<string> _messageBatch = new();
    private readonly object _batchLock = new();
    private readonly IModel _channel;
    private readonly IConnection _connection;

    public ReportingHostedService(ILogger<ReportingHostedService> logger, string connectionString,
        string exchange, string queueName, int batchSize)
    {
        _logger = logger;
        _queueName = queueName;
        _batchSize = batchSize;

        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        var queueResult = _channel.QueueDeclare(_queueName, true, false, false, null);
        _channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hosted Service running.");

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("New message...");

            lock (_batchLock)
            {
                _messageBatch.Add(message);

                if (_messageBatch.Count < _batchSize) return;

                ProcessBatch(_messageBatch);
                _messageBatch.Clear();
            }
        };

        _channel.BasicConsume(queue: _queueName,
            autoAck: true,
            consumer: consumer);

        await BackgroundProcessing(stoppingToken);
    }


    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Hosted Service is working...");

            lock (_batchLock)
            {
                if (_messageBatch.Count <= 0) return;

                ProcessBatch(_messageBatch);

                _messageBatch.Clear();
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
        base.Dispose();
    }

    private static void ProcessBatch(ConcurrentBag<string> messageBatch)
    {
        Console.WriteLine($"Processing batch of {messageBatch.Count} messages.");
        foreach (var message in messageBatch)
        {
            Console.WriteLine(message);
        }
    }
}