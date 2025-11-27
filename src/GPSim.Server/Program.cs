using DotNetEnv;
using GPSim.Server.Configuration;
using GPSim.Server.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Server;

// Load environment variables from .env file (if exists)
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<MapboxSettings>(
    builder.Configuration.GetSection(MapboxSettings.SectionName));
builder.Services.Configure<WebhookSettings>(
    builder.Configuration.GetSection(WebhookSettings.SectionName));
builder.Services.Configure<StorageSettings>(
    builder.Configuration.GetSection(StorageSettings.SectionName));

// Add services
builder.Services.AddScoped<IRoutePersistenceService, RoutePersistenceService>();

// Add HttpClient for webhook forwarding
builder.Services.AddHttpClient<IWebhookForwarderService, WebhookForwarderService>((sp, client) =>
{
    var settings = builder.Configuration.GetSection(WebhookSettings.SectionName).Get<WebhookSettings>();
    client.Timeout = TimeSpan.FromSeconds(settings?.TimeoutSeconds ?? 30);
});

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseCors();

// Serve Blazor WebAssembly files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
