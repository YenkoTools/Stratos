using Server;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Build YARP routes and clusters from appsettings ---
// Clusters are keyed by environment name under Apim:Clusters
var apimClusters = config.GetSection("Apim:Clusters")
    .GetChildren()
    .ToDictionary(x => x.Key, x => x.Value ?? string.Empty);

var initialRoutes = new List<RouteConfig>
{
    new RouteConfig
    {
        RouteId = "apim-route",
        ClusterId = "apim-dev",
        Match = new RouteMatch { Path = "/api/{**catch-all}" },
    }
};

var initialClusters = apimClusters
    .Select(kvp => new ClusterConfig
    {
        ClusterId = $"apim-{kvp.Key}",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new DestinationConfig { Address = kvp.Value }
        }
    })
    .ToList();

// --- Services ---
builder.Services.AddSingleton<EnvironmentState>();
builder.Services.AddSingleton<SecretStore>();
builder.Services.AddHttpClient();

// LoadFromMemory registers InMemoryConfigProvider, enabling runtime cluster switching.
builder.Services
    .AddReverseProxy()
    .LoadFromMemory(initialRoutes, initialClusters);

var app = builder.Build();

// --- Secret bootstrap ---
// DPAPI (ProtectedData) is Windows-only. On Linux in development, keys are read
// directly from IConfiguration (appsettings.json). In production on Windows, keys
// are encrypted to secrets.dat on first run and loaded from there on subsequent runs.
var secretStore = app.Services.GetRequiredService<SecretStore>();

var apimKeys = new Dictionary<string, string>
{
    ["dev"]   = config["Apim:Keys:dev"]   ?? string.Empty,
    ["qa"]    = config["Apim:Keys:qa"]    ?? string.Empty,
    ["stage"] = config["Apim:Keys:stage"] ?? string.Empty,
    ["prod"]  = config["Apim:Keys:prod"]  ?? string.Empty,
};

if (OperatingSystem.IsWindows())
{
    if (!secretStore.IsInitialised)
        secretStore.ProtectAndSave(apimKeys);
    else
        secretStore.LoadAndUnprotect();
}

// --- Static files from wwwroot (Astro build output) ---
app.UseDefaultFiles();
app.UseStaticFiles();

// --- APIM subscription key injection ---
// Runs before YARP so the forwarded request carries the correct key.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        var envState = ctx.RequestServices.GetRequiredService<EnvironmentState>();
        var store    = ctx.RequestServices.GetRequiredService<SecretStore>();
        var env      = envState.Current;

        var key = OperatingSystem.IsWindows()
            ? store.GetKey(env)
            : config[$"Apim:Keys:{env}"] ?? string.Empty;

        ctx.Request.Headers["Ocp-Apim-Subscription-Key"] = key;
    }
    await next();
});

// --- Environment switch endpoint ---
// Called by EnvironmentSwitcher.tsx when the user picks a new environment.
app.MapPost("/internal/environment/{env}", (
    string env,
    EnvironmentState envState,
    InMemoryConfigProvider yarpConfig) =>
{
    if (!envState.TrySet(env))
        return Results.BadRequest(new { error = $"Unknown environment: {env}" });

    // Update the YARP route to point at the new cluster; leave all clusters intact.
    var current = yarpConfig.GetConfig();
    var updatedRoute = current.Routes.First() with
    {
        ClusterId = $"apim-{envState.Current}"
    };

    yarpConfig.Update([updatedRoute], current.Clusters.ToList());

    return Results.Ok(new { active = envState.Current });
});

// --- Active environment query ---
app.MapGet("/internal/environment", (EnvironmentState envState) =>
    Results.Ok(new { active = envState.Current }));

// --- YARP reverse proxy ---
app.MapReverseProxy();

// --- SPA fallback — serve index.html for any unmatched route ---
app.MapFallbackToFile("index.html");

app.Run();
