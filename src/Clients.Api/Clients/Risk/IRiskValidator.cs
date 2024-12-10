using Clients.Contracts.Events;

namespace Clients.Api.Clients.Risk;

public interface IRiskValidator
{
    Task<bool> HasAcceptableRiskLevelAsync(Client client);
}