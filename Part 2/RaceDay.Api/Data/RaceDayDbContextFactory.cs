using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceDay.Api.Data;

public sealed class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RACEDAY_SQLSERVER_CONNECTION")
                        ?? "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer(connection)
            .Options;
        return new RaceDayDbContext(options);
    }
}
