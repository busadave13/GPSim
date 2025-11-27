# GPSim Technical Context

## Technology Stack

### Core Technologies
| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 9.0 |
| Frontend | Blazor WebAssembly | 9.0 |
| Backend | ASP.NET Core | 9.0 |
| Mapping | Mapbox GL JS | 3.x |
| Geospatial | Turf.js | 7.x |

### Project Structure
```
GPSim/
├── src/
│   ├── GPSim.Client/          # Blazor WASM
│   │   ├── Pages/
│   │   │   └── Simulator.razor
│   │   ├── Services/
│   │   │   └── MapboxInteropService.cs
│   │   └── wwwroot/
│   │       ├── js/mapbox-interop.js
│   │       └── css/app.css
│   │
│   ├── GPSim.Server/          # ASP.NET Core
│   │   ├── Controllers/
│   │   │   ├── ConfigurationController.cs
│   │   │   ├── WebhookController.cs
│   │   │   └── RoutesController.cs
│   │   ├── Services/
│   │   │   ├── IWebhookForwarderService.cs
│   │   │   ├── WebhookForwarderService.cs
│   │   │   ├── IRoutePersistenceService.cs
│   │   │   └── RoutePersistenceService.cs
│   │   └── Configuration/
│   │       ├── MapboxSettings.cs
│   │       ├── WebhookSettings.cs
│   │       └── StorageSettings.cs
│   │
│   └── GPSim.Shared/          # Shared Models
│       └── Models/
│           ├── Coordinate.cs
│           ├── GpsPayload.cs
│           ├── RouteGeometry.cs
│           ├── SimulationRoute.cs
│           ├── SimulationSettings.cs
│           └── SimulationState.cs
└── memory-bank/               # Project Documentation
```

## Key Dependencies

### Client-Side (GPSim.Client)
- `Microsoft.AspNetCore.Components.WebAssembly` - Blazor WASM runtime
- Mapbox GL JS (CDN) - Interactive map rendering
- Turf.js (CDN) - Geospatial calculations

### Server-Side (GPSim.Server)
- `Microsoft.AspNetCore.Components.WebAssembly.Server` - Serves Blazor app
- Built-in `HttpClient` for webhook forwarding

## Configuration

### appsettings.json Structure
```json
{
  "Mapbox": {
    "AccessToken": "pk.xxx..."
  },
  "Webhook": {
    "DefaultUrl": "https://...",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  },
  "Storage": {
    "RoutesPath": "data/routes"
  }
}
```

### Environment Variables
- `Mapbox__AccessToken` - Override Mapbox token
- `Webhook__DefaultUrl` - Override default webhook URL

## API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/configuration/mapbox` | GET | Returns Mapbox access token |
| `/api/webhook/broadcast` | POST | Forwards GPS payload to webhook |
| `/api/routes` | GET | List saved routes |
| `/api/routes/{id}` | GET | Get specific route |
| `/api/routes` | POST | Save a new route |

### Webhook Broadcast Parameters
```
POST /api/webhook/broadcast?webhookUrl={url}&webhookHeaders={headers}
Content-Type: application/json

{
  "deviceId": "sim-xxx",
  "latitude": 37.7749,
  "longitude": -122.4194,
  ...
}
```

## Key Models

### GpsPayload
```csharp
public class GpsPayload
{
    public string DeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Speed { get; set; }
    public double Bearing { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
    public int SequenceNumber { get; set; }
}
```

### SimulationSettings
```csharp
public class SimulationSettings
{
    public int IntervalMs { get; set; } = 1000;
    public double SpeedMph { get; set; } = 30.0;
    public string DeviceId { get; set; } = "simulator";
    public string? WebhookUrl { get; set; }
    public string? WebhookHeaders { get; set; }
}
```

### RouteGeometry
```csharp
public class RouteGeometry
{
    public List<double[]> Coordinates { get; set; }
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
}
```

## Development Commands

```bash
# Restore and build
dotnet restore
dotnet build

# Run the application
cd src/GPSim.Server
dotnet run

# Run with watch mode
dotnet watch run

# Build for production
dotnet publish -c Release
```

## Ports
- Development: `https://localhost:5001` / `http://localhost:5000`
