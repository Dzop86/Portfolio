using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Rogue.Api;
using Rogue.Api.Data;
using Rogue.Api.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// A run of 100,000 actions is about 100 kB: nothing legitimate is larger than 256 kB.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 256 * 1024);

string connection = builder.Configuration.GetConnectionString("Scores")
    ?? throw new InvalidOperationException("Set ConnectionStrings__Scores (PostgreSQL connection string).");
builder.Services.AddDbContext<ScoresDb>(options => options.UseNpgsql(connection));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Tokens>();
builder.Services.AddSingleton<IPasswordHasher<Player>, PasswordHasher<Player>>();
builder.Services.AddProblemDetails();
// A body that is not valid JSON is the client's fault (400), in development too, where it is thrown.
builder.Services.Configure<ExceptionHandlerOptions>(handler =>
    handler.StatusCodeSelector = e => e is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError);
builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Tokens>((options, tokens) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokens.ValidationParameters();
    });
builder.Services.AddAuthorization();

// Sign-up and sign-in are limited per client address, against password guessing.
int perMinute = builder.Configuration.GetValue("RateLimit:AccountsPerMinute", 10);
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddPolicy(AccountEndpoints.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddOpenApi(openApi =>
{
    openApi.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Rogue scores API";
        document.Info.Description = "Accounts, ranked runs and leaderboard of the roguelike. The server draws each ranked run's seed and computes the score by replaying the run.";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "The token returned by POST /api/tokens.",
        };
        return Task.CompletedTask;
    });
    openApi.AddOperationTransformer((operation, context, _) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] });
        }
        return Task.CompletedTask;
    });
});

WebApplication app = builder.Build();

if (app.Configuration.GetValue("Database:Migrate", true))
{
    using IServiceScope scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ScoresDb>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.UseSwaggerUI(ui =>
{
    ui.SwaggerEndpoint("/openapi/v1.json", "Rogue scores API");
    ui.RoutePrefix = "swagger";
});
app.MapGet("/health", async (ScoresDb db, CancellationToken cancel) =>
        await db.Database.CanConnectAsync(cancel) ? Results.Ok(new { status = "ok" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .WithTags("Health")
    .ExcludeFromDescription();
app.MapAccountEndpoints();
app.MapRunEndpoints();
app.MapScoreEndpoints();

app.Run();

/// <summary>Visible to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
