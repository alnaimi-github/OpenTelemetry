using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Testing.Utilities.RabbitMq;

public class RabbitMqConsumer
{
    private readonly ConnectionFactory _factory;

    public RabbitMqConsumer(string connectionString)
    {
        _factory = new ConnectionFactory()
        {
            Uri = new Uri(connectionString)
        };
    }

    public void BindQueue(string exchange, string queueName)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);
        var queueResult = channel.QueueDeclare(queueName, true, false, false, null);
        channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);
    }

    public async Task<bool> TryToConsumeAsync(string exchange, string queueName, TimeSpan timeout)
    {
        var messageReceived = new TaskCompletionSource<bool>();
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true);
        var queueResult = channel.QueueDeclare(queueName, true, false, false, null);
        channel.QueueBind(queue: queueResult.QueueName, exchange: exchange, routingKey: string.Empty);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, _) => { messageReceived.SetResult(true); };

        channel.BasicConsume(queue: queueResult.QueueName,
            autoAck: true,
            consumer: consumer);

        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(messageReceived.Task, timeoutTask);

        return completedTask == messageReceived.Task;
    }
}