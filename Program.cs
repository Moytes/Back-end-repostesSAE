using Back_end_RepostesSAE.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<ReportsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ReportsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "reports",
    status = "running"
}));

app.MapGet("/health/db", async (ReportsDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

    return canConnect
        ? Results.Ok(new { database = "reports", status = "connected" })
        : Results.Problem("No se pudo conectar a la base de datos.");
});

app.MapGet("/health/db/migrations", async (ReportsDbContext dbContext, CancellationToken cancellationToken) =>
{
    var applied = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
    var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

    return Results.Ok(new
    {
        applied,
        pending
    });
});

app.Run();
