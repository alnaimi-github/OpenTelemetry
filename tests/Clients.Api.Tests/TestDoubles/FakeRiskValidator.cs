using Clients.Api.Clients;
using Clients.Api.Clients.Risk;
using Clients.Contracts.Events;

namespace Clients.Api.Tests.TestDoubles;

public class FakeRiskValidator : IRiskValidator
{
    private readonly List<string> _highRiskEmails = [];

    public void FlagAsHighRisk(string email)
    {
        _highRiskEmails.Add(email);
    }

    public Task<bool> HasAcceptableRiskLevelAsync(Client client)
    {
        return Task.FromResult(!_highRiskEmails.Contains(client.Email));
    }
}