import OlMap from "ol/Map.js";
import View from "ol/View.js";
import { defaults as defaultControls } from "ol/control/defaults.js";
import VectorLayer from "ol/layer/Vector.js";
import TileLayer from "ol/layer/Tile.js";
import { fromLonLat, transformExtent } from "ol/proj.js";
import OSM from "ol/source/OSM.js";
import VectorSource from "ol/source/Vector.js";
import type {
    DotNetViewport,
    MarkerConfig,
    Wgs84Bounds,
    Wgs84Coordinate,
} from "./mapContracts.js";
import { configureMarkerStyles, createFeatureStyle } from "./mapStyles.js";

export interface MapHandle {
    map: OlMap;
    resizeObserver: ResizeObserver;
    geometrySource: VectorSource;
    geometryLayer: VectorLayer<VectorSource>;
    dotNetViewport: DotNetViewport;
    hoveredWaypointKey: string | null;
}

const maps = new Map<string, MapHandle>();
const tileUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
const attribution =
    '© <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap contributors</a>';

export function initializeMap(
    elementId: string,
    markerConfig: MarkerConfig,
    dotNetViewport: DotNetViewport,
): void {
    disposeMap(elementId);
    configureMarkerStyles(markerConfig);

    const target = document.getElementById(elementId);
    if (!target) {
        throw new Error(`Map target '${elementId}' was not found.`);
    }

    const geometrySource = new VectorSource();
    const geometryLayer = new VectorLayer({
        source: geometrySource,
        style: createFeatureStyle(null, null),
    });
    const map = new OlMap({
        target,
        layers: [createBaseLayer(), geometryLayer],
        controls: defaultControls({ attributionOptions: { collapsible: false } }),
        view: new View({
            center: fromLonLat([10, 56]),
            zoom: 5,
        }),
    });

    const resizeObserver = new ResizeObserver(() => map.updateSize());
    resizeObserver.observe(target);
    const handle = {
        map,
        resizeObserver,
        geometrySource,
        geometryLayer,
        dotNetViewport,
        hoveredWaypointKey: null,
    };
    map.on("pointermove", event => notifyWaypointHover(handle, event.pixel));
    maps.set(elementId, handle);
    fitMapBounds(elementId, [7.5, 54.5, 15.5, 57.8], 32);
}

export function fitMapBounds(
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

export function disposeMap(elementId: string): void {
    const handle = maps.get(elementId);
    if (!handle) return;

    handle.resizeObserver.disconnect();
    handle.map.setTarget(undefined);
    maps.delete(elementId);
}

export function getMap(elementId: string): MapHandle {
    const handle = maps.get(elementId);
    if (!handle) {
        throw new Error(`Map '${elementId}' has not been initialized.`);
    }

    return handle;
}

function createBaseLayer(): TileLayer<OSM> {
    return new TileLayer({
        source: new OSM({
            attributions: attribution,
            maxZoom: 19,
            url: tileUrl,
        }),
    });
}

function notifyWaypointHover(handle: MapHandle, pixel: number[]): void {
    const feature = handle.map.forEachFeatureAtPixel(
        pixel,
        candidate => candidate.get("kind") === "waypoint" ? candidate : undefined,
        { hitTolerance: 6 },
    );
    const target = handle.map.getTargetElement();
    target.style.cursor = feature ? "pointer" : "";
    if (!feature) {
        if (handle.hoveredWaypointKey !== null) {
            handle.hoveredWaypointKey = null;
            void handle.dotNetViewport.invokeMethodAsync("HideWaypointTooltip");
        }
        return;
    }

    const documentId = feature.get("documentId") as string;
    const waypointIndex = feature.get("waypointIndex") as number;
    const key = `${documentId}|${waypointIndex}`;
    if (handle.hoveredWaypointKey === key) return;

    handle.hoveredWaypointKey = key;
    const coordinate = feature.get("waypointCoordinate") as Wgs84Coordinate;
    const anchorPixel = handle.map.getPixelFromCoordinate(fromLonLat(coordinate));
    void handle.dotNetViewport.invokeMethodAsync(
        "ShowWaypointTooltip",
        documentId,
        waypointIndex,
        anchorPixel[0],
        anchorPixel[1],
    );
}
