using Clients.Contracts.Events;

namespace Clients.Api.Clients;

public record ClientListItem(Guid Id, string Name);

public static class ClientMappingExtensions
{
    public static ClientListItem AsClientListItem(this Client client)
    {
        return new( client.Id, client.Name);
    }
}