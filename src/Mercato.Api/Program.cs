using Mercato.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Services (Phase 3 adds Application/Infrastructure registrations, versioning, middleware).
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

// HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
