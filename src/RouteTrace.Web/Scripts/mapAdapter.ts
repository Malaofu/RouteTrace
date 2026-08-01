import OlMap from "ol/Map.js";
import View from "ol/View.js";
import TileLayer from "ol/layer/Tile.js";
import OSM from "ol/source/OSM.js";
import { defaults as defaultControls } from "ol/control/defaults.js";
import { fromLonLat, transformExtent } from "ol/proj.js";
import Feature from "ol/Feature.js";
import LineString from "ol/geom/LineString.js";
import Point from "ol/geom/Point.js";
import VectorLayer from "ol/layer/Vector.js";
import VectorSource from "ol/source/Vector.js";
import { Circle as CircleStyle, Fill, Stroke, Style } from "ol/style.js";
import type { StyleFunction } from "ol/style/Style.js";

type Wgs84Bounds = [west: number, south: number, east: number, north: number];

interface MapHandle {
    map: OlMap;
    resizeObserver: ResizeObserver;
    geometrySource: VectorSource;
    geometryLayer: VectorLayer<VectorSource>;
}

type Wgs84Coordinate = [longitude: number, latitude: number];

interface ImportedGeometry {
    tracks: Array<{ segments: Wgs84Coordinate[][] }>;
    routes: Wgs84Coordinate[][];
    waypoints: Wgs84Coordinate[];
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

    const geometrySource = new VectorSource();
    const geometryLayer = new VectorLayer({ source: geometrySource });
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
            geometryLayer,
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
    maps.set(elementId, { map, resizeObserver, geometrySource, geometryLayer });
    fitBounds(elementId, [7.5, 54.5, 15.5, 57.8], 32);
}

export function renderGeometry(
    elementId: string,
    geometry: ImportedGeometry | null,
    selectedTrack: number | null,
    selectedSegment: number | null,
): void {
    const handle = getMap(elementId);
    handle.geometrySource.clear();
    handle.geometryLayer.setStyle(featureStyle(selectedTrack, selectedSegment));

    if (!geometry) {
        return;
    }

    geometry.tracks.forEach((track, trackIndex) => {
        track.segments.forEach((segment, segmentIndex) => {
            if (segment.length === 0) return;
            handle.geometrySource.addFeature(new Feature({
                geometry: segment.length === 1
                    ? new Point(fromLonLat(segment[0]))
                    : new LineString(segment.map(coordinate => fromLonLat(coordinate))),
                kind: "track",
                trackIndex,
                segmentIndex,
            }));
        });
    });
    geometry.routes.forEach((route) => {
        if (route.length === 0) return;
        handle.geometrySource.addFeature(new Feature({
            geometry: route.length === 1
                ? new Point(fromLonLat(route[0]))
                : new LineString(route.map(coordinate => fromLonLat(coordinate))),
            kind: "route",
        }));
    });
    geometry.waypoints.forEach((waypoint) => {
        handle.geometrySource.addFeature(new Feature({
            geometry: new Point(fromLonLat(waypoint)),
            kind: "waypoint",
        }));
    });

    if (!handle.geometrySource.isEmpty()) {
        const extent = handle.geometrySource.getExtent();
        if (!extent) return;
        handle.map.getView().fit(extent, {
            duration: 250,
            maxZoom: 18,
            padding: [64, 64, 64, 64],
        });
    }
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

function featureStyle(selectedTrack: number | null, selectedSegment: number | null): StyleFunction {
    return (feature): Style => {
        const kind = feature.get("kind") as string;
        const trackIndex = feature.get("trackIndex") as number | undefined;
        const segmentIndex = feature.get("segmentIndex") as number | undefined;
        const selected = kind === "track" && trackIndex === selectedTrack &&
            (selectedSegment === null || segmentIndex === selectedSegment);

        if (kind === "waypoint") {
            return new Style({
                image: new CircleStyle({
                    radius: 7,
                    fill: new Fill({ color: "#f59e0b" }),
                    stroke: new Stroke({ color: "#ffffff", width: 2 }),
                }),
            });
        }

        if (kind === "route") {
            return new Style({ stroke: new Stroke({ color: "#7c3aed", width: 4, lineDash: [8, 6] }) });
        }

        return new Style({
            image: new CircleStyle({
                radius: selected ? 7 : 5,
                fill: new Fill({ color: selected ? "#ef4444" : "#2563eb" }),
                stroke: new Stroke({ color: "#ffffff", width: 2 }),
            }),
            stroke: new Stroke({ color: selected ? "#ef4444" : "#2563eb", width: selected ? 7 : 4 }),
            zIndex: selected ? 10 : 1,
        });
    };
}
