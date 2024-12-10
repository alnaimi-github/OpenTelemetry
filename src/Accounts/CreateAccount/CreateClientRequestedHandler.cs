using Accounts.Contracts.Events;
using Clients.Contracts.Events;
using Infrastructure.RabbitMQ;
using Microsoft.EntityFrameworkCore;

namespace Accounts.CreateAccount;

public class CreateClientRequestedHandler : IEventHandler<CreateClientRequested>
{
    private readonly AccountsDbContext _context;
    private readonly RabbitMqPublisher _publisher;

    public CreateClientRequestedHandler(AccountsDbContext context, RabbitMqPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task HandleAsync(CreateClientRequested @event)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.ClientId == @event.Client.Id);

        if (account is null)
            account = await CreateAccount(@event);

        var accountCreatedEvent = new AccountCreated(account.Id, account.ClientId,
            account.ClientName, account.ClientEmail);

        _publisher.Publish(accountCreatedEvent, "accounts.events");
    }

    private async Task<Account> CreateAccount(CreateClientRequested @event)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            ClientId = @event.Client.Id,
            ClientName = @event.Client.Name,
            ClientEmail = @event.Client.Email
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }
}