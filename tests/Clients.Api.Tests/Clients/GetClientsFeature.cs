using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clients.Api.Clients;
using Clients.Contracts.Events;

namespace Clients.Api.Tests.Clients;

[Collection("API collection")]
public class GetClientsFeature
{
    private readonly HttpClient _apiClient;

    public GetClientsFeature(ClientsApiFactory factory)
    {
        _apiClient = factory.CreateClient();
    }

    [Fact]
    public async Task Should_return_list_of_clients()
    {
        var response = await _apiClient.GetAsync("/clients");

        response.Should().BeSuccessful();
        var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
        clients.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_return_client_when_exists()
    {
        var newClientId = await CreateClient();

        var response = await _apiClient.GetAsync($"/clients/{newClientId}");
        
        response.Should().BeSuccessful();
        var clients = await response.Content.ReadFromJsonAsync<Client>();
        clients.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_return_not_found_when_not_exists()
    {
        const string nonExistingClientId = "270cb1be-6359-4feb-b806-8de60e5b4c63";

        var response = await _apiClient.GetAsync($"/clients/{nonExistingClientId}");

        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateClient()
    {
        var newClientRequest = new
        {
            Name = "John Doe",
            Email = "john@test.com",
            Membership = "Premium",
        };

        var response = await _apiClient.PostAsJsonAsync("/clients",
            newClientRequest);
        var createdClient =
            await response.Content.ReadFromJsonAsync<Client>();
        return createdClient!.Id;
    }
}