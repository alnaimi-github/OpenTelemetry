namespace Accounts.CreateAccount;

public class Account
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; }
    public string ClientEmail { get; init; }
}