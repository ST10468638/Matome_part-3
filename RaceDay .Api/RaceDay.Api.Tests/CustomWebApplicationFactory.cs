using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay_.Api.Data;

namespace RaceDay.Api.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    // Share the database root across test factories.
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();

    // Give each factory its own database to keep tests isolated.
    private readonly string _databaseName =
        $"RaceDayTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Registration:OrganiserInviteCode"] =
                        "TestOrganiserInvite123!"
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(
                    _databaseName,
                    DatabaseRoot));
        });
    }
}