using DotNetEnv;
using GPSim.Server.Configuration;
using GPSim.Server.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

// Configure OpenTelemetry using standard OTEL environment variables with fallback to appsettings.json
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
    ?? builder.Configuration.GetValue<string>("OpenTelemetry:ServiceName")
    ?? "GPSim.Server";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint");
var otlpProtocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")
    ?? builder.Configuration.GetValue<string>("OpenTelemetry:Protocol")
    ?? "grpc";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(WebhookTelemetry.ActivitySource.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        // Add console exporter for development
        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                // Use gRPC (default) or HTTP/protobuf based on configuration
                // Supports both "http" and standard "http/protobuf" values
                options.Protocol = otlpProtocol.Contains("http", StringComparison.OrdinalIgnoreCase)
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
            });
        }
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
