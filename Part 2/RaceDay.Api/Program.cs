using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Environment.IsEnvironment("Testing")
    ? "InMemory"
    : builder.Configuration["RACEDAY_DATABASE_PROVIDER"]
      ?? builder.Configuration["RaceDay:DatabaseProvider"]
      ?? "SqlServer";
var sqlServerConnection = builder.Configuration["RACEDAY_SQLSERVER_CONNECTION"]
                          ?? builder.Configuration.GetConnectionString("RaceDay");
if (!databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) &&
    string.IsNullOrWhiteSpace(sqlServerConnection))
{
    throw new InvalidOperationException(
        "Set RACEDAY_SQLSERVER_CONNECTION or ConnectionStrings:RaceDay to a SQL Server connection string.");
}

builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        var databaseName = builder.Configuration["RaceDay:InMemoryDatabaseName"] ?? "RaceDayPreview";
        options.UseInMemoryDatabase(databaseName);
    }
    else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerConnection);
    }
    else
    {
        throw new InvalidOperationException($"Unsupported RaceDay database provider '{databaseProvider}'.");
    }
});

builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, SessionBearerHandler>("Bearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay REST API",
        Version = "v1",
        Description = "Part 2 API for South African running, walking, and cycling events."
    });
    options.EnableAnnotations();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RaceDay.Api.xml"), includeControllerXmlComments: true);
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque session token",
        In = ParameterLocation.Header,
        Description = "Sign in at POST /api/auth/login and paste the returned token here."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger(options => options.RouteTemplate = "api/swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "api/swagger";
        options.SwaggerEndpoint("./v1/swagger.json", "RaceDay REST API v1");
        options.DocumentTitle = "RaceDay API";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/healthz", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
    await DemoSeed.SeedAsync(db);
}

await app.RunAsync();

public partial class Program { }

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.GetCustomAttributes(true).OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any()
                           || context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                               .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any() == true;
        var allowsAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any();
        if (!hasAuthorize || allowsAnonymous)
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            }] = Array.Empty<string>()
        });
    }
}
