using Accounts;
using Accounts.CreateAccount;
using Clients.Contracts.Events;
using Infrastructure.RabbitMQ;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountsDb")));
builder.Services.AddSingleton(new RabbitMqPublisher(builder.Configuration.GetConnectionString("RabbitMq")!));
builder.Services.AddScoped<CreateClientRequestedHandler>();

builder.Services.AddScoped<RabbitMqConsumer<CreateClientRequested>>(
    sp => new RabbitMqConsumer<CreateClientRequested>
    (builder.Configuration.GetConnectionString("RabbitMq")!,
        sp.GetRequiredService<CreateClientRequestedHandler>()));


builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("AccountsDb")!)
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMq")!);

var app = builder.Build();

// For demo purposes, ensure the database is created
// On a production application, this should not be a responsibility of the application
EnsureDbCreated(app);

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "Accounts");

using var scope = app.Services.CreateScope();
var rabbitMqConsumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer<CreateClientRequested>>();
rabbitMqConsumer.StartConsuming("clients.events", "accounts.create_account");

app.Run();

static void EnsureDbCreated(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var scopedServices = scope.ServiceProvider;
    var context = scopedServices.GetRequiredService<AccountsDbContext>();
    context.Database.EnsureCreated();
}