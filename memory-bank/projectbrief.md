# GPSim - GPS Simulator Project Brief

## Overview
GPSim is a web-based GPS simulation application that allows users to simulate a driver moving along a route with configurable intervals, sending GPS coordinates to external webhooks.

## Core Purpose
- Simulate GPS device movement along a mapped route
- Send real-time GPS coordinate updates to external services
- Provide visual representation of simulated movement on an interactive map

## Target Use Cases
1. **Testing GPS-dependent applications** - Simulate device movement without physical hardware
2. **Development/debugging** - Test webhook integrations and GPS data processing
3. **Demo/presentation** - Show GPS tracking capabilities visually

## Key Features
- Interactive Mapbox-based route planning
- Click-to-set start and end waypoints
- Automatic routing via Mapbox Directions API
- Real-time marker animation along routes
- Configurable simulation parameters:
  - Update interval (100ms - 10000ms)
  - Speed in MPH
  - Custom device ID
  - Custom webhook URL and headers
- Start/pause/resume/stop simulation controls
- Webhook broadcasting of GPS payloads

## Success Criteria
- Smooth visual simulation of GPS movement
- Reliable webhook delivery with retry logic
- Intuitive user interface for route planning
- Accurate position interpolation along routes
