using Accounts.CreateAccount;
using Clients.Contracts.Events;
using DotNet.Testcontainers.Builders;
using Infrastructure.RabbitMQ;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testing.Utilities.Extensions;

namespace Accounts.Tests;

public class AccountsFactory : WebApplicationFactory<IAccountsAssemblyMarker>,
    IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
        .Build();

    public string? RabbitMqConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        RabbitMqConnectionString = _rabbitMqContainer.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(
            services =>
            {
                services.RemoveDbContext<AccountsDbContext>();
                services.AddDbContext<AccountsDbContext>(options =>
                    options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));
                services.EnsureDbCreated<AccountsDbContext>();

                services.Remove<RabbitMqPublisher>();
                services.AddSingleton(new RabbitMqPublisher(
                    _rabbitMqContainer.GetConnectionString()
                ));

                services.Remove<RabbitMqConsumer<CreateClientRequested>>();
                services.AddScoped<RabbitMqConsumer<CreateClientRequested>>(
                    sp => new RabbitMqConsumer<CreateClientRequested>
                    (_rabbitMqContainer.GetConnectionString(),
                        sp.GetRequiredService<CreateClientRequestedHandler>()));
            });
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}