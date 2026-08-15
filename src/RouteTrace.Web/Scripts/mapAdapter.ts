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
    geometry.routes.forEach((route, routeIndex) => {
        if (route.length === 0) return;
        source.addFeature(new Feature({
            ...properties,
            geometry: route.length === 1
                ? new Point(fromLonLat(route[0]))
                : new LineString(route.map(coordinate => fromLonLat(coordinate))),
            kind: "route",
            routeIndex,
        }));
    });
    geometry.waypoints.forEach((waypoint, waypointIndex) => source.addFeature(new Feature({
        ...properties,
        geometry: new Point(fromLonLat(waypoint)),
        kind: "waypoint",
        waypointIndex,
    })));
}

export function setDocumentPresentation(
    elementId: string,
    documentId: string,
    colour: string,
    active: boolean,
    selected: boolean,
    presentation: Array<{ kind: string; primaryIndex: number; secondaryIndex: number; visible: boolean; colour: string }>,
    selectedTrack: number | null,
    selectedSegment: number | null,
    selectedRoute: number | null,
    selectedWaypoint: number | null,
    selectedWholeDocument: boolean,
    selectedWaypointGroup: boolean,
): void {
    const handle = getMap(elementId);
    handle.geometrySource.getFeatures()
        .filter(feature => feature.get("documentId") === documentId)
        .forEach(feature => {
            const kind = feature.get("kind") as string;
            const primaryIndex = kind === "track" ? feature.get("trackIndex") : kind === "route" ? feature.get("routeIndex") : feature.get("waypointIndex");
            const secondaryIndex = kind === "track" ? feature.get("segmentIndex") : -1;
            const override = presentation.find(item => item.kind === kind && item.primaryIndex === primaryIndex && item.secondaryIndex === secondaryIndex);
            feature.setProperties({
            documentColour: override?.colour ?? colour,
            presentationVisible: override?.visible ?? true,
            activeDocument: active,
            selectedDocument: selected,
            selectedTrack,
            selectedSegment,
            selectedRoute,
            selectedWaypoint,
            selectedWholeDocument,
            selectedWaypointGroup,
        }, true);
        });
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

export function focusSelection(elementId: string, documentId: string, trackIndex: number | null, segmentIndex: number | null, routeIndex: number | null, waypointIndex: number | null, wholeDocument: boolean, waypointGroup: boolean): void {
    const handle = getMap(elementId);
    const features = handle.geometrySource.getFeatures().filter(feature => {
        if (feature.get("documentId") !== documentId || feature.get("presentationVisible") === false) return false;
        if (wholeDocument) return true;
        const kind = feature.get("kind") as string;
        if (trackIndex !== null) return kind === "track" && feature.get("trackIndex") === trackIndex && (segmentIndex === null || feature.get("segmentIndex") === segmentIndex);
        if (routeIndex !== null) return kind === "route" && feature.get("routeIndex") === routeIndex;
        if (waypointIndex !== null) return kind === "waypoint" && feature.get("waypointIndex") === waypointIndex;
        return waypointGroup && kind === "waypoint";
    });
    if (features.length === 0) return;
    const extent = features[0].getGeometry()!.getExtent().slice() as [number, number, number, number];
    features.slice(1).forEach(feature => {
        const next = feature.getGeometry()!.getExtent();
        extent[0] = Math.min(extent[0], next[0]); extent[1] = Math.min(extent[1], next[1]);
        extent[2] = Math.max(extent[2], next[2]); extent[3] = Math.max(extent[3], next[3]);
    });
    handle.map.getView().fit(extent, { duration: 250, maxZoom: 18, padding: [64, 64, 64, 64] });
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
    geometry.routes.forEach((route, routeIndex) => {
        if (route.length === 0) return;
        handle.geometrySource.addFeature(new Feature({
            geometry: route.length === 1
                ? new Point(fromLonLat(route[0]))
                : new LineString(route.map(coordinate => fromLonLat(coordinate))),
            kind: "route",
            routeIndex,
            documentColour: document.colour,
            activeDocument: document.isActive,
            selectedDocument: document.isSelected,
        }));
    });
    geometry.waypoints.forEach((waypoint, waypointIndex) => {
        handle.geometrySource.addFeature(new Feature({
            geometry: new Point(fromLonLat(waypoint)),
            kind: "waypoint",
            waypointIndex,
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
    return (feature): Style | undefined => {
        if (feature.get("presentationVisible") === false) return undefined;
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
        const routeIndex = feature.get("routeIndex") as number | undefined;
        const waypointIndex = feature.get("waypointIndex") as number | undefined;
        const selectedRoute = feature.get("selectedRoute") as number | null | undefined;
        const selectedWaypoint = feature.get("selectedWaypoint") as number | null | undefined;
        const selectedWholeDocument = feature.get("selectedWholeDocument") as boolean | undefined;
        const selectedWaypointGroup = feature.get("selectedWaypointGroup") as boolean | undefined;
        const semanticSelected =
            selectedWholeDocument === true ||
            (selectedRoute !== null && selectedRoute !== undefined && routeIndex === selectedRoute) ||
            (selectedWaypoint !== null && selectedWaypoint !== undefined && waypointIndex === selectedWaypoint) ||
            (selectedWaypointGroup === true && kind === "waypoint");

        if (kind === "waypoint") {
            return new Style({
                image: new CircleStyle({
                    radius: semanticSelected ? 9 : 7,
                    fill: new Fill({ color: colour }),
                    stroke: new Stroke({ color: "#ffffff", width: 2 }),
                }),
            });
        }

        if (kind === "route") {
            return new Style({ stroke: new Stroke({ color: colour, width: semanticSelected ? 7 : activeDocument ? 5 : 3, lineDash: [8, 6] }), zIndex: semanticSelected ? 20 : 1 });
        }

        return new Style({
            image: new CircleStyle({
                radius: selected || incrementallySelected || selectedWholeDocument ? 7 : 5,
                fill: new Fill({ color: colour }),
                stroke: new Stroke({ color: "#ffffff", width: 2 }),
            }),
            stroke: new Stroke({ color: colour, width: selected || incrementallySelected || selectedWholeDocument ? 7 : activeDocument ? 5 : 3 }),
            zIndex: selected || incrementallySelected || selectedWholeDocument ? 20 : selectedDocument ? 15 : activeDocument ? 10 : 1,
        });
    };
}
