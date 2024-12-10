using Clients.Api.Clients;
using Infrastructure.RabbitMQ;

namespace Clients.Api.Extensions;

public static class RabbitMqPublisherConfigurationExtensions
{
    public static WebApplicationBuilder AddRabbitMq(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(new RabbitMqPublisher(builder.Configuration.GetConnectionString("RabbitMq")!));
        builder.Services.AddSingleton<EventsPublisher>(sp =>
            new EventsPublisher(sp.GetRequiredService<RabbitMqPublisher>(),
                builder.Configuration.GetValue<bool>("Feature:PublishEventFailure")));

        return builder;
    }
}