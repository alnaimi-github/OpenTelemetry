using Reporting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMq")!);

builder.Services.AddHostedService<ReportingHostedService>(
    sp => new ReportingHostedService(
        sp.GetRequiredService<ILogger<ReportingHostedService>>(),
        builder.Configuration.GetConnectionString("RabbitMq")!,
        "accounts.events",
        "reporting.export_accounts",
        2
    )
);


var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "Reporting");


app.Run();