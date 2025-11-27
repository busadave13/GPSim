# GPS Simulator (GPSim)

A web-based GPS simulation application using Mapbox for mapping and route visualization. This tool allows you to simulate a driver moving along a route with configurable intervals and send GPS coordinates to an external webhook.

## Features

- **Interactive Map**: Mapbox GL JS integration for route planning
- **Route Planning**: Click to set start/end points, get directions from Mapbox Directions API
- **Simulation Controls**: Start, pause, resume, and stop simulations
- **Real-time Updates**: Smooth marker animation along the route
- **Webhook Integration**: Send GPS coordinates to external services at configurable intervals
- **Configurable Settings**:
  - Update interval (100ms - 10000ms)
  - Speed multiplier (0.1x - 10x)
  - Custom device ID
  - Optional webhook URL override

## Prerequisites

- .NET 9.0 SDK (for local development)
- Docker (for containerized deployment)
- A Mapbox access token (get one at https://account.mapbox.com/)

## Quick Start with Docker

The easiest way to run GPSim is with Docker:

```bash
# 1. Clone the repository
git clone https://github.com/busadave13/GPSim.git
cd GPSim

# 2. Copy environment template and add your Mapbox token
cp .env.example .env
# Edit .env and set MAPBOX_ACCESS_TOKEN=your_token_here

# 3. Build and run
docker compose up --build

# 4. Open browser
# Navigate to http://localhost:4000
```

### Docker Commands

```bash
# Run in background (detached mode)
docker compose up -d

# View logs
docker compose logs -f

# Stop the container
docker compose down

# Rebuild after code changes
docker compose up --build
```

## Configuration

Update `src/GPSim.Server/appsettings.json` with your settings:

```json
{
  "Mapbox": {
    "AccessToken": "YOUR_MAPBOX_ACCESS_TOKEN"
  },
  "Webhook": {
    "DefaultUrl": "https://your-webhook-endpoint.com/gps",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  }
}
```

## Running the Application

1. **Restore packages and build:**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Run the server:**
   ```bash
   cd src/GPSim.Server
   dotnet run
   ```

3. **Open browser:** Navigate to `https://localhost:5001` or `http://localhost:5000`

## Usage

1. **Set Route**:
   - Click "Set Start" and click on the map to place the starting point
   - Click "Set End" and click on the map to place the destination
   - Click "Get Directions" to fetch the route from Mapbox

2. **Configure Simulation**:
   - Adjust the update interval (how often GPS data is sent)
   - Set the speed multiplier (1x = real-time, 10x = 10 times faster)
   - Customize the device ID if needed
   - Optionally set a custom webhook URL

3. **Run Simulation**:
   - Click "Start" to begin the simulation
   - Watch the marker move along the route
   - Use "Pause" to temporarily stop, "Resume" to continue
   - Click "Stop" to end the simulation

## GPS Payload Format

The webhook receives JSON payloads in the following format:

```json
{
  "deviceId": "sim-abc12345",
  "latitude": 37.7749,
  "longitude": -122.4194,
  "altitude": 0,
  "speed": 12.5,
  "bearing": 180.0,
  "accuracy": 5.0,
  "timestamp": "2025-11-26T20:00:00Z",
  "sequenceNumber": 42
}
```

## Project Structure

```
GPSim/
├── src/
│   ├── GPSim.Shared/        # Shared models
│   ├── GPSim.Client/        # Blazor WASM client
│   │   ├── Pages/           # Razor pages
│   │   ├── Services/        # Client services
│   │   └── wwwroot/         # Static assets & JS interop
│   └── GPSim.Server/        # ASP.NET Core server
│       ├── Controllers/     # API endpoints
│       ├── Services/        # Business logic
│       └── Configuration/   # Settings classes
├── .docs/
│   └── design.md           # Design document
└── GPSim.sln               # Solution file
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/configuration/mapbox` | GET | Get Mapbox access token |
| `/api/webhook/broadcast` | POST | Forward GPS payload to webhook |
| `/api/routes` | GET/POST | Save/load simulation routes |

## License

MIT
