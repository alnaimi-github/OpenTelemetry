using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Infrastructure.RabbitMQ;

public class RabbitMqPublisher
{
    private readonly ConnectionFactory _factory;

    public RabbitMqPublisher(string connectionString)
    {
        _factory = new ConnectionFactory()
        {
            Uri = new Uri(connectionString)
        };
    }

    public void Publish<T>(T @event, string exchange)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);

        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: exchange, routingKey: string.Empty, basicProperties: null, body: body);
    }
}