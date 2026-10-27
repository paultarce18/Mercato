using Mercato.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Hosted services (Phase 8 adds the outbox publisher, saga and scheduled jobs).
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
