using System.Net;
using System.Net.Http.Json;
using Clients.Api.Clients;
using Clients.Api.Clients.Risk;
using Clients.Api.Tests.TestDoubles;
using Clients.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Testing.Utilities.RabbitMq;

namespace Clients.Api.Tests.Clients;

[Collection("API collection")]
public class CreateClientsFeature(ClientsApiFactory factory)
{
    private readonly HttpClient _apiClient = factory.CreateClient();
    private readonly RabbitMqConsumer _rabbitMqConsumer = factory.RabbitMqConsumer;

    [Fact]
    public async Task Should_create_and_return_new_id()
    {
        var newClientRequest = new
        {
            Name = "John Doe",
            Email = "john@doe-corporate.com",
            Membership = "Premium",
        };

        var response = await _apiClient.PostAsJsonAsync("/clients",
            newClientRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createClientData = await response.Content.ReadFromJsonAsync<Client>();
        createClientData!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_fail_with_conflict_when_email_already_exists()
    {
        var newClientRequest = new
        {
            Name = "Jane Doe",
            Email = "jane@doe-corporate.com",
            Membership = "Regular",
        };
        await _apiClient.PostAsJsonAsync("/clients", newClientRequest);

        var response = await _apiClient.PostAsJsonAsync("/clients",
            newClientRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_fail_with_problem_when_client_has_high_risk()
    {
        var riskValidator = (FakeRiskValidator)factory.Services.GetService<IRiskValidator>()!;
        riskValidator.FlagAsHighRisk("high-risk@doe-corporate.com");
        var newClientRequest = new
        {
            Name = "Jane Doe",
            Email = "high-risk@doe-corporate.com",
            Membership = "Regular",
        };

        var response = await _apiClient.PostAsJsonAsync("/clients",
            newClientRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_publish_event_when_client_created()
    {
        const string exchange = "clients.events";
        const string queueName = "create_client_testing";
        _rabbitMqConsumer.BindQueue(exchange, queueName);
        var newClientRequest = new
        {
            Name = "John Doe",
            Email = "John@doe-corporate.com",
            Membership = "Premium",
        };

        var response = await _apiClient.PostAsJsonAsync("/clients",
            newClientRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var messageConsumed = await _rabbitMqConsumer.TryToConsumeAsync(exchange, queueName, TimeSpan.FromSeconds(5));
        Assert.True(messageConsumed, "Message was not received within the timeout period.");
    }
}