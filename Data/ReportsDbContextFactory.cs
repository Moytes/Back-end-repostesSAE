using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Back_end_RepostesSAE.Data;

public sealed class ReportsDbContextFactory : IDesignTimeDbContextFactory<ReportsDbContext>
{
    public ReportsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("ReportsDb")
            ?? configuration["REPORTS_DB_CONNECTION"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure la cadena de conexion con ConnectionStrings__ReportsDb o REPORTS_DB_CONNECTION.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ReportsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ReportsDbContext(optionsBuilder.Options);
    }
}
