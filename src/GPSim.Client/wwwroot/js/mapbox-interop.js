// Mapbox GL JS interop for Blazor
window.mapboxInterop = {
    map: null,
    marker: null,
    homeMarker: null,
    routeLine: null,
    directionsLayer: null,
    radiusCircle: null,
    dotNetRef: null,

    /**
     * Initialize the Mapbox map
     */
    initialize: function (containerId, accessToken, center, zoom, circleRadiusMiles) {
        console.log('Initializing Mapbox map...', { containerId, center, zoom, circleRadiusMiles });
        
        // Store the circle radius for later use
        this.circleRadiusMiles = circleRadiusMiles || 0.1;
        
        if (!accessToken) {
            console.error('Mapbox access token is required!');
            return Promise.reject('Access token required');
        }
        
        mapboxgl.accessToken = accessToken;

        try {
            this.map = new mapboxgl.Map({
                container: containerId,
                style: 'mapbox://styles/mapbox/streets-v12',
                center: center || [-122.4194, 37.7749], // Default to San Francisco
                zoom: zoom || 12
            });

            // Add navigation controls
            this.map.addControl(new mapboxgl.NavigationControl());

            // Add geolocate control
            const geolocateControl = new mapboxgl.GeolocateControl({
                positionOptions: {
                    enableHighAccuracy: true
                },
                trackUserLocation: false,
                showUserHeading: false
            });
            this.map.addControl(geolocateControl);

            // Store reference for later use
            this.geolocateControl = geolocateControl;

            return new Promise((resolve, reject) => {
                this.map.on('load', () => {
                    console.log('Mapbox map loaded successfully');
                    
                    // Try to get user's location and center the map
                    if (navigator.geolocation) {
                        navigator.geolocation.getCurrentPosition(
                            (position) => {
                                console.log('Got user location:', position.coords.latitude, position.coords.longitude);
                                const lng = position.coords.longitude;
                                const lat = position.coords.latitude;
                                
                                this.map.flyTo({
                                    center: [lng, lat],
                                    zoom: 14,
                                    essential: true
                                });
                                
                                // Place driver marker at user's location and notify Blazor
                                this.setMarker(lng, lat, 0);
                                
                                // Draw radius circle around user using configured radius
                                this.drawRadiusCircle(lng, lat, this.circleRadiusMiles);
                                
                                // Notify Blazor of initial location
                                if (this.dotNetRef) {
                                    this.dotNetRef.invokeMethodAsync('OnInitialLocation', lng, lat);
                                }
                            },
                            (error) => {
                                console.warn('Geolocation error:', error.message);
                                // Keep default location (San Francisco) if geolocation fails
                            },
                            {
                                enableHighAccuracy: true,
                                timeout: 10000,
                                maximumAge: 0
                            }
                        );
                    }
                    
                    resolve(true);
                });
                this.map.on('error', (e) => {
                    console.error('Mapbox error:', e);
                    reject(e);
                });
            });
        } catch (error) {
            console.error('Error creating Mapbox map:', error);
            return Promise.reject(error);
        }
    },

    /**
     * Get directions between two points using Mapbox Directions API
     */
    getDirections: async function (accessToken, origin, destination, profile = 'driving') {
        const url = `https://api.mapbox.com/directions/v5/mapbox/${profile}/${origin[0]},${origin[1]};${destination[0]},${destination[1]}?geometries=geojson&overview=full&access_token=${accessToken}`;

        try {
            const response = await fetch(url);
            const data = await response.json();

            if (data.routes && data.routes.length > 0) {
                const route = data.routes[0];
                return {
                    coordinates: route.geometry.coordinates,
                    distance: route.distance,
                    duration: route.duration,
                    geometry: route.geometry
                };
            }
            return null;
        } catch (error) {
            console.error('Error fetching directions:', error);
            return null;
        }
    },

    /**
     * Draw a route on the map
     */
    drawRoute: function (coordinates) {
        // Remove existing route if any
        if (this.map.getSource('route')) {
            this.map.removeLayer('route');
            this.map.removeSource('route');
        }

        this.map.addSource('route', {
            type: 'geojson',
            data: {
                type: 'Feature',
                properties: {},
                geometry: {
                    type: 'LineString',
                    coordinates: coordinates
                }
            }
        });

        this.map.addLayer({
            id: 'route',
            type: 'line',
            source: 'route',
            layout: {
                'line-join': 'round',
                'line-cap': 'round'
            },
            paint: {
                'line-color': '#3887be',
                'line-width': 5,
                'line-opacity': 0.75
            }
        });

        // Fit map to route bounds
        const bounds = coordinates.reduce((bounds, coord) => {
            return bounds.extend(coord);
        }, new mapboxgl.LngLatBounds(coordinates[0], coordinates[0]));

        this.map.fitBounds(bounds, { padding: 50 });
    },

    /**
     * Clear the route from the map
     */
    clearRoute: function () {
        if (this.map.getSource('route')) {
            this.map.removeLayer('route');
            this.map.removeSource('route');
        }
    },

    /**
     * Create or update the driver marker
     */
    setMarker: function (lng, lat, bearing = 0) {
        if (this.marker) {
            this.marker.setLngLat([lng, lat]);
            // Update rotation using Mapbox's built-in rotation
            this.marker.setRotation(bearing);
        } else {
            // Create custom marker element with arrow pointing UP (North = 0°)
            const el = document.createElement('div');
            el.className = 'driver-marker';
            el.innerHTML = `
                <svg width="40" height="40" viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <circle cx="20" cy="20" r="18" fill="#4285F4" stroke="white" stroke-width="3"/>
                    <polygon points="20,8 28,28 20,24 12,28" fill="white"/>
                </svg>
            `;
            el.style.width = '40px';
            el.style.height = '40px';
            el.style.cursor = 'pointer';

            this.marker = new mapboxgl.Marker({
                element: el,
                anchor: 'center',
                rotationAlignment: 'map'  // Rotate with the map for realistic navigation
            })
                .setLngLat([lng, lat])
                .setRotation(bearing)
                .addTo(this.map);
        }
    },

    /**
     * Animate marker to a new position
     */
    animateMarker: function (lng, lat, bearing, duration = 1000) {
        if (!this.marker) {
            this.setMarker(lng, lat, bearing);
            return;
        }

        const currentPos = this.marker.getLngLat();
        const startLng = currentPos.lng;
        const startLat = currentPos.lat;
        const startTime = performance.now();

        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            // Ease function for smooth animation
            const easeProgress = 1 - Math.pow(1 - progress, 3);

            const newLng = startLng + (lng - startLng) * easeProgress;
            const newLat = startLat + (lat - startLat) * easeProgress;

            this.marker.setLngLat([newLng, newLat]);

            // Update bearing using Mapbox's built-in rotation
            this.marker.setRotation(bearing);

            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };

        requestAnimationFrame(animate);
    },

    /**
     * Remove the marker from the map
     */
    removeMarker: function () {
        if (this.marker) {
            this.marker.remove();
            this.marker = null;
        }
    },

    /**
     * Center the map on a specific location
     */
    flyTo: function (lng, lat, zoom = 14) {
        this.map.flyTo({
            center: [lng, lat],
            zoom: zoom,
            essential: true
        });
    },

    /**
     * Get click coordinates from the map
     */
    enableClickCapture: function (dotNetRef) {
        // Store the reference for callbacks
        this.dotNetRef = dotNetRef;
        
        this.map.on('click', (e) => {
            dotNetRef.invokeMethodAsync('OnMapClick', e.lngLat.lng, e.lngLat.lat);
        });
    },

    /**
     * Disable click capture
     */
    disableClickCapture: function () {
        this.map.off('click');
    },

    /**
     * Add start and end markers for route planning
     */
    setWaypoint: function (lng, lat, type) {
        const markerId = `waypoint-${type}`;

        // Remove existing marker of same type
        if (this[markerId]) {
            this[markerId].remove();
        }

        const color = type === 'start' ? '#22c55e' : '#ef4444';
        const label = type === 'start' ? 'A' : 'B';

        const el = document.createElement('div');
        el.className = 'waypoint-marker';
        el.innerHTML = `
            <div style="
                width: 30px;
                height: 30px;
                border-radius: 50%;
                background-color: ${color};
                border: 3px solid white;
                box-shadow: 0 2px 6px rgba(0,0,0,0.3);
                display: flex;
                align-items: center;
                justify-content: center;
                color: white;
                font-weight: bold;
                font-size: 14px;
            ">${label}</div>
        `;

        this[markerId] = new mapboxgl.Marker({
            element: el,
            anchor: 'center'
        })
            .setLngLat([lng, lat])
            .addTo(this.map);
    },

    /**
     * Clear all waypoint markers
     */
    clearWaypoints: function () {
        if (this['waypoint-start']) {
            this['waypoint-start'].remove();
            this['waypoint-start'] = null;
        }
        if (this['waypoint-end']) {
            this['waypoint-end'].remove();
            this['waypoint-end'] = null;
        }
    },

    /**
     * Set home marker at user's current location
     */
    setHomeMarker: function (lng, lat) {
        // Remove existing home marker if any
        if (this.homeMarker) {
            this.homeMarker.remove();
        }

        const el = document.createElement('div');
        el.className = 'home-marker';
        el.innerHTML = `
            <div style="
                width: 36px;
                height: 36px;
                background-color: #8b5cf6;
                border: 3px solid white;
                border-radius: 50%;
                box-shadow: 0 2px 6px rgba(0,0,0,0.3);
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 18px;
            ">🏠</div>
        `;

        this.homeMarker = new mapboxgl.Marker({
            element: el,
            anchor: 'center'
        })
            .setLngLat([lng, lat])
            .addTo(this.map);
        
        console.log('Home marker placed at:', lat, lng);
    },

    /**
     * Remove home marker from the map
     */
    removeHomeMarker: function () {
        if (this.homeMarker) {
            this.homeMarker.remove();
            this.homeMarker = null;
        }
    },

    /**
     * Calculate bearing between two points
     */
    calculateBearing: function (start, end) {
        const startLat = start[1] * Math.PI / 180;
        const startLng = start[0] * Math.PI / 180;
        const endLat = end[1] * Math.PI / 180;
        const endLng = end[0] * Math.PI / 180;

        const dLng = endLng - startLng;

        const x = Math.sin(dLng) * Math.cos(endLat);
        const y = Math.cos(startLat) * Math.sin(endLat) -
            Math.sin(startLat) * Math.cos(endLat) * Math.cos(dLng);

        const bearing = Math.atan2(x, y) * 180 / Math.PI;
        return (bearing + 360) % 360;
    },

    /**
     * Interpolate a position along the route at a given fraction
     */
    interpolatePosition: function (coordinates, fraction) {
        if (!coordinates || coordinates.length < 2) return null;
        if (fraction <= 0) return { position: coordinates[0], bearing: 0 };
        if (fraction >= 1) return {
            position: coordinates[coordinates.length - 1],
            bearing: this.calculateBearing(
                coordinates[coordinates.length - 2],
                coordinates[coordinates.length - 1]
            )
        };

        // Calculate total distance
        let totalDistance = 0;
        const distances = [];
        for (let i = 1; i < coordinates.length; i++) {
            const d = this.haversineDistance(coordinates[i - 1], coordinates[i]);
            distances.push(d);
            totalDistance += d;
        }

        // Find the segment and position within it
        const targetDistance = totalDistance * fraction;
        let accumulatedDistance = 0;

        for (let i = 0; i < distances.length; i++) {
            if (accumulatedDistance + distances[i] >= targetDistance) {
                // Found the segment
                const segmentFraction = (targetDistance - accumulatedDistance) / distances[i];
                const start = coordinates[i];
                const end = coordinates[i + 1];

                const lng = start[0] + (end[0] - start[0]) * segmentFraction;
                const lat = start[1] + (end[1] - start[1]) * segmentFraction;
                const bearing = this.calculateBearing(start, end);

                return { position: [lng, lat], bearing: bearing };
            }
            accumulatedDistance += distances[i];
        }

        return {
            position: coordinates[coordinates.length - 1],
            bearing: 0
        };
    },

    /**
     * Calculate haversine distance between two points (in meters)
     */
    haversineDistance: function (coord1, coord2) {
        const R = 6371000; // Earth's radius in meters
        const lat1 = coord1[1] * Math.PI / 180;
        const lat2 = coord2[1] * Math.PI / 180;
        const dLat = (coord2[1] - coord1[1]) * Math.PI / 180;
        const dLng = (coord2[0] - coord1[0]) * Math.PI / 180;

        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1) * Math.cos(lat2) *
            Math.sin(dLng / 2) * Math.sin(dLng / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return R * c;
    },

    /**
     * Calculate total route distance in meters
     */
    calculateRouteDistance: function (coordinates) {
        if (!coordinates || coordinates.length < 2) return 0;

        let total = 0;
        for (let i = 1; i < coordinates.length; i++) {
            total += this.haversineDistance(coordinates[i - 1], coordinates[i]);
        }
        return total;
    },

    /**
     * Draw a radius circle around a point
     * @param {number} lng - Longitude of center
     * @param {number} lat - Latitude of center
     * @param {number} radiusMiles - Radius in miles
     */
    drawRadiusCircle: function (lng, lat, radiusMiles) {
        console.log('drawRadiusCircle called:', { lng, lat, radiusMiles });
        
        try {
            // Remove existing circle if any
            this.removeRadiusCircle();
            
            // Convert miles to meters (1 mile = 1609.344 meters)
            const radiusMeters = radiusMiles * 1609.344;
            
            // Create a circle using turf.js style calculation
            const points = 64;
            const coordinates = [];
            
            for (let i = 0; i <= points; i++) {
                const angle = (i / points) * 360;
                const point = this.destinationPoint(lng, lat, radiusMeters, angle);
                coordinates.push(point);
            }
            
            // Check if source already exists (shouldn't happen after removeRadiusCircle, but just in case)
            if (this.map.getSource('radius-circle')) {
                console.warn('Source already exists, updating data');
                this.map.getSource('radius-circle').setData({
                    type: 'Feature',
                    properties: {},
                    geometry: {
                        type: 'Polygon',
                        coordinates: [coordinates]
                    }
                });
                return;
            }
            
            // Add the circle source and layer
            this.map.addSource('radius-circle', {
                type: 'geojson',
                data: {
                    type: 'Feature',
                    properties: {},
                    geometry: {
                        type: 'Polygon',
                        coordinates: [coordinates]
                    }
                }
            });
            
            // Add fill layer
            this.map.addLayer({
                id: 'radius-circle-fill',
                type: 'fill',
                source: 'radius-circle',
                paint: {
                    'fill-color': '#4285F4',
                    'fill-opacity': 0.1
                }
            });
            
            // Add outline layer
            this.map.addLayer({
                id: 'radius-circle-outline',
                type: 'line',
                source: 'radius-circle',
                paint: {
                    'line-color': '#4285F4',
                    'line-width': 2,
                    'line-opacity': 0.6
                }
            });
            
            console.log('Radius circle drawn successfully:', radiusMiles, 'miles at', lng, lat);
        } catch (error) {
            console.error('Error drawing radius circle:', error);
        }
    },
    
    /**
     * Remove the radius circle from the map
     */
    removeRadiusCircle: function () {
        if (this.map.getLayer('radius-circle-fill')) {
            this.map.removeLayer('radius-circle-fill');
        }
        if (this.map.getLayer('radius-circle-outline')) {
            this.map.removeLayer('radius-circle-outline');
        }
        if (this.map.getSource('radius-circle')) {
            this.map.removeSource('radius-circle');
        }
    },
    
    /**
     * Calculate destination point given start, distance and bearing
     * @param {number} lng - Start longitude
     * @param {number} lat - Start latitude
     * @param {number} distanceMeters - Distance in meters
     * @param {number} bearingDegrees - Bearing in degrees
     * @returns {Array} [lng, lat] of destination point
     */
    destinationPoint: function (lng, lat, distanceMeters, bearingDegrees) {
        const R = 6371000; // Earth's radius in meters
        const d = distanceMeters / R; // Angular distance
        const brng = bearingDegrees * Math.PI / 180; // Bearing in radians
        
        const lat1 = lat * Math.PI / 180;
        const lng1 = lng * Math.PI / 180;
        
        const lat2 = Math.asin(
            Math.sin(lat1) * Math.cos(d) +
            Math.cos(lat1) * Math.sin(d) * Math.cos(brng)
        );
        
        const lng2 = lng1 + Math.atan2(
            Math.sin(brng) * Math.sin(d) * Math.cos(lat1),
            Math.cos(d) - Math.sin(lat1) * Math.sin(lat2)
        );
        
        return [lng2 * 180 / Math.PI, lat2 * 180 / Math.PI];
    },

    /**
     * Get current marker position
     * @returns {Object|null} {lng, lat} or null if no marker
     */
    getMarkerPosition: function () {
        if (this.marker) {
            const pos = this.marker.getLngLat();
            return { lng: pos.lng, lat: pos.lat };
        }
        return null;
    },

    /**
     * Cleanup and destroy the map
     */
    destroy: function () {
        this.removeRadiusCircle();
        if (this.marker) {
            this.marker.remove();
            this.marker = null;
        }
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
    }
};
