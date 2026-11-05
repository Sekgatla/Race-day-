using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RaceDay.Api.Tests;

public sealed class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests_{Guid.NewGuid():N}";
    private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"RaceDayMediaTests_{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RaceDay:DatabaseProvider"] = "InMemory",
                ["RaceDay:InMemoryDatabaseName"] = _databaseName,
                ["RACEDAY_MEDIA_PROVIDER"] = "Local",
                ["RaceDay:Media:LocalPath"] = _mediaPath
            });
        });
    }
}
