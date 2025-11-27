# GPSim System Patterns

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                          │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                 Blazor WebAssembly App                       ││
│  │  ┌─────────────┐  ┌──────────────────┐  ┌────────────────┐  ││
│  │  │ Simulator   │  │ MapboxInterop    │  │ HttpClient     │  ││
│  │  │ .razor      │◄─┤ Service.cs       │  │ (to Server)    │  ││
│  │  └─────────────┘  └────────┬─────────┘  └───────┬────────┘  ││
│  │                            │                     │           ││
│  │                            ▼                     │           ││
│  │                   ┌────────────────┐             │           ││
│  │                   │ mapbox-interop │             │           ││
│  │                   │ .js            │             │           ││
│  └───────────────────┴────────────────┴─────────────┴───────────┘│
└──────────────────────────────────┬──────────────────────────────┘
                                   │ HTTP API Calls
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Server                         │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                      Controllers                            │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │ │
│  │  │Configuration │  │ Webhook      │  │ Routes           │  │ │
│  │  │Controller    │  │ Controller   │  │ Controller       │  │ │
│  │  └──────────────┘  └──────┬───────┘  └──────────────────┘  │ │
│  │                           │                                 │ │
│  │                           ▼                                 │ │
│  │  ┌────────────────────────────────────────────────────────┐│ │
│  │  │                     Services                            ││ │
│  │  │  ┌────────────────────┐  ┌────────────────────────┐    ││ │
│  │  │  │WebhookForwarder    │  │RoutePersistence        │    ││ │
│  │  │  │Service             │  │Service                 │    ││ │
│  │  │  └─────────┬──────────┘  └────────────────────────┘    ││ │
│  │  └────────────┼───────────────────────────────────────────┘│ │
│  └───────────────┼────────────────────────────────────────────┘ │
└──────────────────┼──────────────────────────────────────────────┘
                   │ HTTP POST
                   ▼
           ┌──────────────┐
           │   External   │
           │   Webhook    │
           └──────────────┘
```

## Key Design Patterns

### 1. Client-Server Separation
- **Client (GPSim.Client)**: Blazor WebAssembly for UI and user interaction
- **Server (GPSim.Server)**: ASP.NET Core for API endpoints and webhook forwarding
- **Shared (GPSim.Shared)**: Common models used by both client and server

### 2. JavaScript Interop Pattern
The `MapboxInteropService` wraps JavaScript calls to Mapbox GL JS:
- C# calls are marshalled to JavaScript via `IJSRuntime`
- Map events (clicks) are captured in JS and passed back to C# via callbacks
- Complex operations (routing, interpolation) handled in JavaScript for performance

### 3. Service Abstraction
- `IWebhookForwarderService` - Interface for webhook forwarding
- `IRoutePersistenceService` - Interface for route storage
- Allows for easy testing and implementation swapping

### 4. Options Pattern for Configuration
```csharp
services.Configure<MapboxSettings>(config.GetSection("Mapbox"));
services.Configure<WebhookSettings>(config.GetSection("Webhook"));
```

### 5. Timer-Based Simulation Loop
```csharp
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Settings.IntervalMs));
while (await timer.WaitForNextTickAsync(cancellationToken))
{
    await SimulationTickAsync();
}
```

### 6. Fire-and-Forget with Error Handling
GPS data is sent asynchronously without blocking the simulation:
```csharp
_ = SendGpsDataAsync(lat, lng, bearing);
```

## Data Flow

### Route Planning
1. User clicks map → JS captures coordinates
2. JS invokes .NET callback → `OnMapClickedAsync`
3. Coordinates stored in component state
4. "Get Directions" → JS calls Mapbox Directions API
5. Route geometry returned and drawn on map

### Simulation Execution
1. Start simulation → Create `PeriodicTimer`
2. Each tick:
   - Calculate elapsed time and distance
   - Compute progress along route (0.0 to 1.0)
   - Interpolate position using Turf.js
   - Animate marker to new position
   - Send GPS payload to server
3. Server forwards payload to configured webhook

### Webhook Forwarding
1. Client POSTs to `/api/webhook/broadcast`
2. Controller receives payload with optional URL/headers overrides
3. `WebhookForwarderService` builds HTTP request
4. Adds custom headers if provided
5. Sends request with exponential backoff retry
