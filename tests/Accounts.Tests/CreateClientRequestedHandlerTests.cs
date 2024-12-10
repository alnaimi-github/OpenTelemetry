using Accounts.CreateAccount;
using Clients.Contracts.Events;
using FluentAssertions;
using Infrastructure.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Tests;

public class CreateClientRequestedHandlerTests : IClassFixture<AccountsFactory>
{
    private readonly AccountsFactory _factory;
    private readonly AccountsDbContext _db;
    private const string AccountsExchange = "accounts.events";
    private const string ClientsExchange = "clients.events";

    public CreateClientRequestedHandlerTests(AccountsFactory factory)
    {
        _factory = factory;
        var scopeFactory = _factory.Services.GetService<IServiceScopeFactory>();
        ArgumentNullException.ThrowIfNull(scopeFactory);
        var scope = scopeFactory.CreateScope();
        _db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
    }

    [Fact]
    public async Task Should_create_an_account_when_create_client_requested_event_is_handled()
    {
        var client = new Client()
        {
            Id = Guid.NewGuid(),
            Email = "joe@duneltd.com",
            Name = "Joe Doe",
            Birthdate = new DateOnly(1980, 1, 1),
            Membership = MembershipLevel.Premium,
            Addresses = [new Address(Guid.NewGuid(), "1234 Elm St", "Springfield", "IL", "USA", "62701")]
        };
        var @event = new CreateClientRequested(client, DateTimeOffset.Now);
        var publisher = new RabbitMqPublisher(_factory.RabbitMqConnectionString!);

        publisher.Publish(@event, ClientsExchange);

        var hasBeenCreated = await HasAccountBeenCreated(client);
        hasBeenCreated.Should().BeTrue();
    }

    [Fact]
    public async Task Should_publish_an_account_created_even_when_account_created()
    {
        var client = new Client()
        {
            Id = Guid.NewGuid(),
            Email = "joe@duneltd.com",
            Name = "Joe Doe",
            Birthdate = new DateOnly(1980, 1, 1),
            Membership = MembershipLevel.Premium,
            Addresses = [new Address(Guid.NewGuid(), "1234 Elm St", "Springfield", "IL", "USA", "62701")]
        };
        var @event = new CreateClientRequested(client, DateTimeOffset.Now);
        var publisher = new RabbitMqPublisher(_factory.RabbitMqConnectionString!);

        publisher.Publish(@event, ClientsExchange);

        var consumer = new Testing.Utilities.RabbitMq.RabbitMqConsumer(_factory.RabbitMqConnectionString!);

        var messageConsumed = await consumer.TryToConsumeAsync(AccountsExchange, 
            "account_created_testing", TimeSpan.FromSeconds(5));
        Assert.True(messageConsumed, "Message was not received within the timeout period.");
    }

    [Fact]
    public async Task
        When_account_already_exist_for_client_id_then_should_not_create_another_account_and_publish_the_account_created_event()
    {
        var client = new Client()
        {
            Id = Guid.NewGuid(),
            Email = "joe@duneltd.com",
            Name = "Joe Doe",
            Birthdate = new DateOnly(1980, 1, 1),
            Membership = MembershipLevel.Premium,
            Addresses = [new Address(Guid.NewGuid(), "1234 Elm St", "Springfield", "IL", "USA", "62701")]
        };
        var @event = new CreateClientRequested(client, DateTimeOffset.Now);
        var publisher = new RabbitMqPublisher(_factory.RabbitMqConnectionString!);
        var createdAccount = await CreateAccount(client);

        publisher.Publish(@event, ClientsExchange);

        var consumer = new Testing.Utilities.RabbitMq.RabbitMqConsumer(_factory.RabbitMqConnectionString!);
        var messageConsumed = await consumer.TryToConsumeAsync(AccountsExchange, 
            "account_created_testing", TimeSpan.FromSeconds(5));
        Assert.True(messageConsumed, "Message was not received within the timeout period.");
        _db.Accounts.Count(account => account.ClientId == client.Id || account.Id == createdAccount.Id).Should().Be(1);
    }

    private async Task<Account> CreateAccount(Client client)
    {
        var account = new Account()
        {
            ClientId = client.Id,
            ClientName = client.Name,
            ClientEmail = client.Email,
            Id = Guid.NewGuid()
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    private async Task<bool> HasAccountBeenCreated(Client client)
    {
        bool hasBeenAdded;
        do
        {
            hasBeenAdded = _db.Accounts.Any(account => client.Id == account.ClientId);
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        } while (!hasBeenAdded);

        return hasBeenAdded;
    }
}