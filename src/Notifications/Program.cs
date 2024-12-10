using Accounts.Contracts.Events;
using Infrastructure.RabbitMQ;
using Notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddScoped<AccountCreatedHandler>();
builder.Services.AddScoped<RabbitMqConsumer<AccountCreated>>(
    sp => new RabbitMqConsumer<AccountCreated>
    (builder.Configuration.GetConnectionString("RabbitMq")!,
        sp.GetRequiredService<AccountCreatedHandler>()));

builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMq")!);

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "Notifications");

using var scope = app.Services.CreateScope();
var rabbitMqConsumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer<AccountCreated>>();
rabbitMqConsumer.StartConsuming("accounts.events", "notifications.email_sender");

app.Run();