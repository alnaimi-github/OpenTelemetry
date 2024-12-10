namespace Clients.Api.Extensions;

public static class HealthCheckConfigurationExtensions
{
    public static WebApplicationBuilder AddHealthChecksConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("ClientsDb")!)
            .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMq")!)
            .AddRedis(builder.Configuration.GetConnectionString("ClientsCache")!);

        return builder;
    }
}