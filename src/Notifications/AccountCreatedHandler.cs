using Accounts.Contracts.Events;
using Infrastructure.RabbitMQ;

namespace Notifications;

public class AccountCreatedHandler : IEventHandler<AccountCreated>
{
    private ILogger<AccountCreated> _logger;

    public AccountCreatedHandler(ILogger<AccountCreated> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AccountCreated @event)
    {
        _logger.LogInformation("Sending notification to {Email}. New Account with ID {AccountId}", @event!.ClientEmail,
            @event!.AccountId);
        return Task.CompletedTask;
    }
}