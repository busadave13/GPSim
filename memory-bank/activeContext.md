# GPSim Active Context

## Current Session: November 26, 2025

### Recent Changes

#### Webhook Headers Feature (Completed)
Added support for custom HTTP headers to be sent with webhook requests.

**Files Modified:**
1. `src/GPSim.Shared/Models/SimulationSettings.cs`
   - Added `WebhookHeaders` property

2. `src/GPSim.Client/Pages/Simulator.razor`
   - Added "Webhook Headers" textbox in UI
   - Updated `SendGpsDataAsync` to pass headers as query parameter

3. `src/GPSim.Server/Controllers/WebhookController.cs`
   - Added `webhookHeaders` query parameter to broadcast endpoint

4. `src/GPSim.Server/Services/IWebhookForwarderService.cs`
   - Updated interface to accept `webhookHeaders` parameter

5. `src/GPSim.Server/Services/WebhookForwarderService.cs`
   - Added `ParseHeaders` method to parse semicolon-separated headers
   - Modified `ForwardAsync` to add custom headers to HTTP requests

**Usage:**
- Format: `Header1:Value1;Header2:Value2`
- Example: `Authorization:Bearer token123;X-Custom-Header:myvalue`

### Current State
- Application is fully functional
- All webhook header changes are complete
- Memory bank initialized

### Areas of Focus
- Core simulation functionality is stable
- Webhook integration with custom headers working
- Map integration with Mapbox fully operational

### Known Considerations
- Webhook headers are passed as a query parameter (URL-encoded)
- Headers are parsed using semicolon as separator, colon for name/value
- Empty headers field results in no custom headers being added

## Active Development Notes

### Testing the Webhook Headers Feature
1. Start the application: `cd src/GPSim.Server && dotnet run`
2. Navigate to the simulator page
3. Set a route (start + end points, get directions)
4. Configure webhook URL and optional headers
5. Start simulation and observe webhook requests

### Potential Future Enhancements
- [ ] Geolocation API integration for current location
- [ ] Save/load routes functionality
- [ ] Multiple device simulation
- [ ] Route playback speed control
- [ ] Webhook response logging in UI
