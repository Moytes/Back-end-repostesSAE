using System.Text;
using Back_end_RepostesSAE.Data;
using Back_end_RepostesSAE.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<ReportsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    options.UseNpgsql(connectionString);
});

// Repositorios (Dapper sobre la DB compartida)
builder.Services.AddScoped<IScopeRepository, ScopeRepository>();
builder.Services.AddScoped<ICanalizacionRepository, CanalizacionRepository>();
builder.Services.AddScoped<IClinicalReadRepository, ClinicalReadRepository>();
builder.Services.AddScoped<IEvaluacionRepository, EvaluacionRepository>();
builder.Services.AddScoped<ISesionRepository, SesionRepository>();

// Autenticación JWT (mismo secreto/issuer/audience que Back-end-SAEV3)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };

        // El token puede llegar como Bearer header o como cookie httpOnly "jwt"
        // (reenviada por el Gateway). Mismo patrón que Back-end-SAEV3.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"]
                    .FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(token))
                    token = context.Request.Cookies["jwt"];

                if (!string.IsNullOrEmpty(token))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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
