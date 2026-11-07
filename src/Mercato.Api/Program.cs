using Mercato.Infrastructure;
using Mercato.Infrastructure.Persistence;
using Mercato.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

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

// Development only (TR-42): apply pending EF migrations, then insert demo data.
// In production, migrations are applied by the deployment pipeline, never by the app itself.
if (app.Environment.IsDevelopment())
{
    // Startup code runs outside any request, so there is no scope yet; create one to resolve the scoped DbContext.
    using IServiceScope scope = app.Services.CreateScope();
    MercatoDbContext db = scope.ServiceProvider.GetRequiredService<MercatoDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false); // Creates the database if missing, then runs any migrations not yet applied.
    await DevelopmentDataSeeder.SeedAsync(db).ConfigureAwait(false); // Inserts demo vendors, categories, products and stock (skipped if already seeded).
}

app.Run();
