namespace Accounts.Contracts.Events;

public record AccountCreated(Guid AccountId, Guid ClientId, string ClientName, string ClientEmail);