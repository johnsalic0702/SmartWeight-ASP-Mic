var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

static string GetServiceAddress(IConfiguration configuration, string key, string defaultAddress)
{
    var address = configuration[key] ?? defaultAddress;
    return address.EndsWith('/') ? address : $"{address}/";
}

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ReverseProxy:Clusters:auth-cluster:Destinations:auth-destination:Address"] =
        GetServiceAddress(builder.Configuration, "AUTH_SERVICE_URL", "http://localhost:8081"),
    ["ReverseProxy:Clusters:pesaje-cluster:Destinations:pesaje-destination:Address"] =
        GetServiceAddress(builder.Configuration, "PESAJE_SERVICE_URL", "http://localhost:8082"),
    ["ReverseProxy:Clusters:maestros-cluster:Destinations:maestros-destination:Address"] =
        GetServiceAddress(builder.Configuration, "MAESTROS_SERVICE_URL", "http://localhost:8083")
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
