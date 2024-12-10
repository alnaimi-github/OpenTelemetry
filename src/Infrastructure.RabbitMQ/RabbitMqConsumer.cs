using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.RabbitMQ;

public class RabbitMqConsumer<T>
{
    private readonly IEventHandler<T> _handler;

    private readonly ConnectionFactory _factory;

    public RabbitMqConsumer(string connectionString, IEventHandler<T> handler)
    {
        _handler = handler;
        _factory = new ConnectionFactory()
        {
            Uri = new Uri(connectionString)
        };
    }

    public void StartConsuming(string exchange, string queueName)
    {
        var connection = _factory.CreateConnection();
        var channel = connection.CreateModel();

        var consumer = new EventingBasicConsumer(channel);

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        var queueResult = channel.QueueDeclare(queueName, true, false, false, null);
        channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);

        consumer.Received += async (_, @event) =>
        {
            var body = @event.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var data = JsonSerializer.Deserialize<T>(message);

            await _handler.HandleAsync(data!);
        };

        channel.BasicConsume(queue: queueResult.QueueName,
            autoAck: true,
            consumer: consumer);
    }
}