// Leaflet map for the Weather page. Leaflet itself is loaded globally (window.L) from App.razor.
let map;
let stationLayer;
let stationMarkers = [];
let showNames = false;

export function init(element) {
    map = L.map(element, {
        minZoom: 6,
        maxZoom: 13,
        maxBounds: [[53.5, 6.5], [58.5, 16.5]],
    }).fitBounds([[54.5, 8], [57.8, 15.3]]);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors | Temperatures: <a href="https://opendatadocs.dmi.govcloud.dk/">DMI</a>',
    }).addTo(map);

    stationLayer = L.layerGroup().addTo(map);
}

export function setStations(stations) {
    stationLayer.clearLayers();
    stationMarkers = stations.map(station => {
        const marker = L.marker([station.latitude, station.longitude], {
            icon: L.divIcon({
                className: 'temp-marker',
                html: `<span style="background-color: ${station.color}">${escapeHtml(station.label)}</span>`,
                iconSize: [26, 26],
            }),
            riseOnHover: true,
        });
        marker.station = station;
        bindTooltip(marker);
        return marker.addTo(stationLayer);
    });
}

export function setShowNames(show) {
    showNames = show;
    stationMarkers.forEach(bindTooltip);
}

export function dispose() {
    map?.remove();
    map = undefined;
    stationMarkers = [];
}

// Hover shows name + details; "show names" pins just the name below every marker.
function bindTooltip(marker) {
    const station = marker.station;
    marker.unbindTooltip();
    if (showNames) {
        marker.bindTooltip(escapeHtml(station.name), {
            permanent: true,
            direction: 'bottom',
            offset: [0, 10],
            className: 'station-name',
        });
    } else {
        marker.bindTooltip(`<strong>${escapeHtml(station.name)}</strong><br>${escapeHtml(station.details)}`, {
            direction: 'top',
            offset: [0, -12],
        });
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
