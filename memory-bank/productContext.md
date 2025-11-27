# GPSim Product Context

## Problem Statement
Developers building GPS-dependent applications need a way to test their systems without requiring physical GPS devices or actual movement. Manual testing with real devices is time-consuming, expensive, and impractical for development workflows.

## Solution
GPSim provides a web-based interface to:
1. Plan routes visually on an interactive map
2. Simulate GPS device movement along those routes
3. Send GPS data payloads to external webhooks
4. Control simulation timing and speed

## User Experience

### Route Planning Flow
1. User clicks "Set Start" button, then clicks map to place start marker
2. User clicks "Set End" button, then clicks map to place end marker
3. User clicks "Get Directions" to fetch route from Mapbox
4. Route is displayed as a blue line on the map

### Simulation Flow
1. User configures settings (interval, speed, device ID, webhook)
2. User clicks "Start" to begin simulation
3. Marker moves smoothly along the route
4. GPS payloads are sent to configured webhook at specified intervals
5. User can pause/resume/stop the simulation at any time

### Control Panel
- Located on the left side of the screen
- Contains all controls for route planning and simulation
- Status panel shows current position, progress, and sequence number

## GPS Payload Format
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

## Configuration Options
| Setting | Description | Range |
|---------|-------------|-------|
| Update Interval | Time between GPS updates | 100ms - 10000ms |
| Speed | Simulated vehicle speed | 1 - 200 MPH |
| Device ID | Identifier sent in payloads | Any string |
| Webhook URL | Target endpoint for GPS data | Valid URL |
| Webhook Headers | Custom HTTP headers | Header:Value;... |
