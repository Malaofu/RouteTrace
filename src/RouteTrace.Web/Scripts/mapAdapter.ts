import OlMap from "ol/Map.js";
import View from "ol/View.js";
import TileLayer from "ol/layer/Tile.js";
import OSM from "ol/source/OSM.js";
import { defaults as defaultControls } from "ol/control/defaults.js";
import { fromLonLat, transformExtent } from "ol/proj.js";

type Wgs84Bounds = [west: number, south: number, east: number, north: number];

interface MapHandle {
    map: OlMap;
    resizeObserver: ResizeObserver;
}

const maps = new Map<string, MapHandle>();
const tileUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
const attribution =
    '© <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap contributors</a>';

export function initialize(elementId: string): void {
    dispose(elementId);

    const target = document.getElementById(elementId);
    if (!target) {
        throw new Error(`Map target '${elementId}' was not found.`);
    }

    const map = new OlMap({
        target,
        layers: [
            new TileLayer({
                source: new OSM({
                    attributions: attribution,
                    maxZoom: 19,
                    url: tileUrl,
                }),
            }),
        ],
        controls: defaultControls({
            attributionOptions: {
                collapsible: false,
            },
        }),
        view: new View({
            center: fromLonLat([10, 56]),
            zoom: 5,
        }),
    });

    const resizeObserver = new ResizeObserver(() => map.updateSize());
    resizeObserver.observe(target);
    maps.set(elementId, { map, resizeObserver });
    fitBounds(elementId, [7.5, 54.5, 15.5, 57.8], 32);
}

export function fitBounds(
    elementId: string,
    bounds: Wgs84Bounds,
    padding = 48,
): void {
    const handle = getMap(elementId);
    const projectedBounds = transformExtent(bounds, "EPSG:4326", "EPSG:3857");

    handle.map.getView().fit(projectedBounds, {
        duration: 250,
        maxZoom: 18,
        padding: [padding, padding, padding, padding],
    });
}

export function dispose(elementId: string): void {
    const handle = maps.get(elementId);
    if (!handle) {
        return;
    }

    handle.resizeObserver.disconnect();
    handle.map.setTarget(undefined);
    maps.delete(elementId);
}

function getMap(elementId: string): MapHandle {
    const handle = maps.get(elementId);
    if (!handle) {
        throw new Error(`Map '${elementId}' has not been initialized.`);
    }

    return handle;
}
