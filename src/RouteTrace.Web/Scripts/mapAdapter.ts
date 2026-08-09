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

interface WorkspaceGeometry {
    id: string;
    geometry: ImportedGeometry;
    colour: string;
    isActive: boolean;
    isSelected: boolean;
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
    const geometryLayer = new VectorLayer({ source: geometrySource, style: featureStyle(null, null) });
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

export function beginDocumentUpdate(elementId: string): void {
    getMap(elementId);
    performance.mark("routeTrace.map.render.start");
}

export function removeDocument(elementId: string, documentId: string): void {
    const source = getMap(elementId).geometrySource;
    source.getFeatures()
        .filter(feature => feature.get("documentId") === documentId)
        .forEach(feature => source.removeFeature(feature));
}

export function upsertDocument(
    elementId: string,
    documentId: string,
    geometry: ImportedGeometry,
    colour: string,
): void {
    removeDocument(elementId, documentId);
    const source = getMap(elementId).geometrySource;
    const properties = { documentId, documentColour: colour };
    geometry.tracks.forEach((track, trackIndex) => track.segments.forEach((segment, segmentIndex) => {
        if (segment.length === 0) return;
        source.addFeature(new Feature({
            ...properties,
            geometry: segment.length === 1
                ? new Point(fromLonLat(segment[0]))
                : new LineString(segment.map(coordinate => fromLonLat(coordinate))),
            kind: "track",
            trackIndex,
            segmentIndex,
        }));
    }));
    geometry.routes.forEach(route => {
        if (route.length === 0) return;
        source.addFeature(new Feature({
            ...properties,
            geometry: route.length === 1
                ? new Point(fromLonLat(route[0]))
                : new LineString(route.map(coordinate => fromLonLat(coordinate))),
            kind: "route",
        }));
    });
    geometry.waypoints.forEach(waypoint => source.addFeature(new Feature({
        ...properties,
        geometry: new Point(fromLonLat(waypoint)),
        kind: "waypoint",
    })));
}

export function setDocumentPresentation(
    elementId: string,
    documentId: string,
    colour: string,
    active: boolean,
    selected: boolean,
    selectedTrack: number | null,
    selectedSegment: number | null,
): void {
    const handle = getMap(elementId);
    handle.geometrySource.getFeatures()
        .filter(feature => feature.get("documentId") === documentId)
        .forEach(feature => feature.setProperties({
            documentColour: colour,
            activeDocument: active,
            selectedDocument: selected,
            selectedTrack,
            selectedSegment,
        }, true));
    handle.geometryLayer.changed();
}

export function endDocumentUpdate(elementId: string, fitGeometry: boolean): void {
    const handle = getMap(elementId);
    if (fitGeometry && !handle.geometrySource.isEmpty()) {
        const extent = handle.geometrySource.getExtent();
        if (extent === null) {
            performance.mark("routeTrace.map.render.end");
            return;
        }
        handle.map.getView().fit(extent, {
            duration: 250,
            maxZoom: 18,
            padding: [64, 64, 64, 64],
        });
    }
    performance.mark("routeTrace.map.render.end");
}

export function renderDocuments(
    elementId: string,
    documents: WorkspaceGeometry[],
    selectedTrack: number | null,
    selectedSegment: number | null,
): void {
    performance.mark("routeTrace.map.render.start");
    const handle = getMap(elementId);
    handle.geometrySource.clear();
    handle.geometryLayer.setStyle(featureStyle(selectedTrack, selectedSegment));

    if (documents.length === 0) {
        performance.mark("routeTrace.map.render.end");
        return;
    }

    documents.forEach(document => {
    const geometry = document.geometry;
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
                documentColour: document.colour,
                activeDocument: document.isActive,
                selectedDocument: document.isSelected,
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
            documentColour: document.colour,
            activeDocument: document.isActive,
            selectedDocument: document.isSelected,
        }));
    });
    geometry.waypoints.forEach((waypoint) => {
        handle.geometrySource.addFeature(new Feature({
            geometry: new Point(fromLonLat(waypoint)),
            kind: "waypoint",
            documentColour: document.colour,
            activeDocument: document.isActive,
            selectedDocument: document.isSelected,
        }));
    });
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
    performance.mark("routeTrace.map.render.end");
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
        const featureSelectedTrack = feature.get("selectedTrack") as number | null | undefined;
        const featureSelectedSegment = feature.get("selectedSegment") as number | null | undefined;
        const incrementallySelected = kind === "track" && trackIndex === featureSelectedTrack &&
            (featureSelectedSegment === null || segmentIndex === featureSelectedSegment);
        const colour = feature.get("documentColour") as string;
        const activeDocument = feature.get("activeDocument") as boolean;
        const selectedDocument = feature.get("selectedDocument") as boolean;

        if (kind === "waypoint") {
            return new Style({
                image: new CircleStyle({
                    radius: 7,
                    fill: new Fill({ color: colour }),
                    stroke: new Stroke({ color: "#ffffff", width: 2 }),
                }),
            });
        }

        if (kind === "route") {
            return new Style({ stroke: new Stroke({ color: colour, width: activeDocument ? 5 : 3, lineDash: [8, 6] }) });
        }

        return new Style({
            image: new CircleStyle({
                radius: selected || incrementallySelected ? 7 : 5,
                fill: new Fill({ color: colour }),
                stroke: new Stroke({ color: "#ffffff", width: 2 }),
            }),
            stroke: new Stroke({ color: colour, width: selected || incrementallySelected ? 7 : activeDocument ? 5 : 3 }),
            zIndex: selected || incrementallySelected ? 20 : selectedDocument ? 15 : activeDocument ? 10 : 1,
        });
    };
}
