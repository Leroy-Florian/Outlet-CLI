using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Outlet.Cloud.Infrastructure.DependencyInjection;
using Outlet.Cloud.Web.DependencyInjection;
using Outlet.Cloud.Web.Endpoints;
using Outlet.Identity.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Persistence is PostgreSQL in the host; integration tests run under the "Testing"
// environment and register an in-memory SQLite provider instead (single provider each).
if (!builder.Environment.IsEnvironment("Testing"))
{
    var identityConnection = builder.Configuration.GetConnectionString("Identity")
        ?? "Host=localhost;Database=outlet_identity;Username=outlet;Password=outlet";
    var cloudConnection = builder.Configuration.GetConnectionString("Cloud")
        ?? "Host=localhost;Database=outlet_cloud;Username=outlet;Password=outlet";

    builder.Services.AddOutletIdentityInfrastructure(options => options.UseNpgsql(identityConnection));
    builder.Services.AddOutletCloudInfrastructure(options => options.UseNpgsql(cloudConnection));
}

builder.Services.AddOutletCloudWeb();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapOutletCloud();
app.MapOutletAuth();

app.Run();

/// <summary>Exposed so integration tests can spin the host via WebApplicationFactory.</summary>
public partial class Program;
