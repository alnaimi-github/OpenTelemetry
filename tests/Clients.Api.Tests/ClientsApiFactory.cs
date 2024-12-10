using Clients.Api.Clients.Risk;
using Clients.Api.Tests.TestDoubles;
using DotNet.Testcontainers.Builders;
using Infrastructure.RabbitMQ;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Testing.Utilities.Extensions;
using Testing.Utilities.RabbitMq;

namespace Clients.Api.Tests;

public class ClientsApiFactory : WebApplicationFactory<IClientsApiAssemblyMarker>,
    IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
        .Build();

    public RabbitMqConsumer RabbitMqConsumer;

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _redisContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        RabbitMqConsumer = new RabbitMqConsumer(_rabbitMqContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__ClientsCache", _redisContainer.GetConnectionString());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(
            services =>
            {
                services.RemoveDbContext<ClientsDbContext>();
                services.AddDbContext<ClientsDbContext>(options =>
                    options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));
                services.EnsureDbCreated<ClientsDbContext>();

                services.Remove<IDistributedCache>();
                services.AddStackExchangeRedisCache(options =>
                    options.Configuration = _redisContainer.GetConnectionString());

                services.Remove<RabbitMqPublisher>();
                services.AddSingleton(new RabbitMqPublisher(
                    _rabbitMqContainer.GetConnectionString()
                ));

                services.Remove<IRiskValidator>();
                services.AddSingleton<IRiskValidator, FakeRiskValidator>();
            });
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}