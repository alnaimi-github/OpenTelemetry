using Clients.Api.Clients;
using Clients.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace Clients.Api;

public class ClientsDbContext : DbContext
{
    public ClientsDbContext(DbContextOptions<ClientsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
}