using System.Text.Json;
using Clients.Api.Clients.Risk;
using Clients.Contracts.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Clients.Api.Clients;

internal static class ClientsApi
{
    public static RouteGroupBuilder MapClients(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/clients");

        group.WithTags("Clients");

        group.MapGet("/", async (ClientsDbContext db) => await db.Clients
            .Select(c => c.AsClientListItem()).AsNoTracking().ToListAsync());

        group.MapGet("/{id:guid}", async (ClientsDbContext db, IDistributedCache cache, Guid id) =>
        {
            var cacheKey = $"client-{id}";
            var clientJson = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(clientJson))
            {
                return Results.Ok(JsonSerializer.Deserialize<Client>(clientJson));
            }

            var client = await db.Clients.FindAsync(id);
            if (client == null)
            {
                return Results.NotFound();
            }

            clientJson = JsonSerializer.Serialize(client);
            await cache.SetStringAsync(cacheKey, clientJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Results.Ok(client);
        });

        group.MapPost("/",
            async Task<Results<Created<Client>, BadRequest<string>, Conflict<string>>> (
                IRiskValidator riskValidator,
                ClientsDbContext db,
                EventsPublisher eventsPublisher,
                Client newClient) =>
            {
                if (string.IsNullOrWhiteSpace(newClient.Name))
                    return TypedResults.BadRequest("Name is required.");

                if (string.IsNullOrWhiteSpace(newClient.Email))
                    return TypedResults.BadRequest("Email is required.");

                if (await IsDuplicatedEmailAsync(db, newClient))
                    return TypedResults.Conflict("A client with the same email already exists.");

                if (!await riskValidator.HasAcceptableRiskLevelAsync(newClient))
                    return TypedResults.BadRequest("The request cannot be processed. Please contact support.");

                var client = new Client
                {
                    Name = newClient.Name,
                    Email = newClient.Email,
                    Membership = newClient.Membership,
                    Addresses = newClient.Addresses
                };

                db.Clients.Add(client);
                await db.SaveChangesAsync();

                eventsPublisher.Publish(client);
                
                return TypedResults.Created($"/clients/{client.Id}", client);
            });

        return group;
    }


    private static async Task<bool> IsDuplicatedEmailAsync(ClientsDbContext db, Client newClient)
    {
        return await db.Clients.AnyAsync(c => c.Email == newClient.Email);
    }
}