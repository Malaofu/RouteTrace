import OlMap from "ol/Map.js";
import View from "ol/View.js";
import Point from "ol/geom/Point.js";
import LineString from "ol/geom/LineString.js";
import { defaults as defaultControls } from "ol/control/defaults.js";
import VectorLayer from "ol/layer/Vector.js";
import TileLayer from "ol/layer/Tile.js";
import { fromLonLat, toLonLat, transformExtent } from "ol/proj.js";
import OSM from "ol/source/OSM.js";
import VectorSource from "ol/source/Vector.js";
import Modify from "ol/interaction/Modify.js";
import type {
    DotNetViewport,
    MarkerConfig,
    Wgs84Bounds,
    Wgs84Coordinate,
} from "./mapContracts.js";
import { configureMarkerStyles, createFeatureStyle } from "./mapStyles.js";
import {
    refreshEditingPointPixels,
    synchronizeEditingPointsFromLine,
} from "./manualRouteEditing.js";

export interface MapHandle {
    map: OlMap;
    resizeObserver: ResizeObserver;
    geometrySource: VectorSource;
    geometryLayer: VectorLayer<VectorSource>;
    editingLineSource: VectorSource;
    editingPointSource: VectorSource;
    editingLineLayer: VectorLayer<VectorSource>;
    editingPointLayer: VectorLayer<VectorSource>;
    editingModify: Modify;
    editingEnabled: boolean;
    editingPointAddEnabled: boolean;
    contextMenuListener: ((event: MouseEvent) => void) | null;
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
    const editingLineSource = new VectorSource();
    const editingPointSource = new VectorSource();
    const editingLineLayer = new VectorLayer({ source: editingLineSource });
    const editingPointLayer = new VectorLayer({ source: editingPointSource });
    const editingModify = new Modify({
        source: editingLineSource,
        pixelTolerance: 12,
        deleteCondition: () => false,
    });
    editingModify.setActive(false);
    const map = new OlMap({
        target,
        layers: [createBaseLayer(), geometryLayer, editingLineLayer, editingPointLayer],
        controls: defaultControls({ attributionOptions: { collapsible: false } }),
        view: new View({
            center: fromLonLat([10, 56]),
            zoom: 5,
        }),
    });

    const resizeObserver = new ResizeObserver(() => map.updateSize());
    resizeObserver.observe(target);
    const handle: MapHandle = {
        map,
        resizeObserver,
        geometrySource,
        geometryLayer,
        editingLineSource,
        editingPointSource,
        editingLineLayer,
        editingPointLayer,
        editingModify,
        editingEnabled: false,
        editingPointAddEnabled: false,
        contextMenuListener: null,
        dotNetViewport,
        hoveredWaypointKey: null,
    };
    map.addInteraction(editingModify);
    map.on("click", event => notifyEditingClick(handle, event.pixel, event.coordinate));
    editingModify.on("modifystart", () => {
        map.getView().cancelAnimations();
        editingPointLayer.setOpacity(0.35);
        map.getTargetElement().dataset.editingLive = "true";
    });
    editingModify.on("modifyend", event => {
        editingPointLayer.setOpacity(1);
        map.getTargetElement().dataset.editingLive = "false";
        synchronizeEditingPointsFromLine(handle);
        const feature = event.features.item(0);
        const geometry = feature?.getGeometry();
        const coordinates = geometry instanceof LineString
            ? geometry.getCoordinates().map(coordinate => toLonLat(coordinate))
            : geometry instanceof Point
                ? [toLonLat(geometry.getCoordinates())]
                : null;
        if (coordinates) void handle.dotNetViewport.invokeMethodAsync("ReplaceEditingPoints", coordinates);
    });
    handle.contextMenuListener = (event: MouseEvent) => notifyEditingContextMenu(handle, event);
    map.getViewport().addEventListener("contextmenu", handle.contextMenuListener);
    map.on("pointermove", event => notifyWaypointHover(handle, event.pixel));
    map.on("moveend", () => refreshEditingPointPixels(handle));
    maps.set(elementId, handle);
    fitMapBounds(elementId, [7.5, 54.5, 15.5, 57.8], 32);
}

function notifyEditingClick(handle: MapHandle, pixel: number[], coordinate: number[]): void {
    if (!handle.editingEnabled) return;
    void handle.dotNetViewport.invokeMethodAsync("HideEditingPointMenu");
    const point = handle.map.forEachFeatureAtPixel(
        pixel,
        feature => feature.get("kind") === "editing-point" ? feature : undefined,
        { hitTolerance: 8, layerFilter: layer => layer === handle.editingPointLayer },
    );
    if (point) {
        void handle.dotNetViewport.invokeMethodAsync("SelectEditingPoint", point.get("editIndex") as number);
        return;
    }

    const line = handle.map.forEachFeatureAtPixel(
        pixel,
        feature => feature.get("kind") === "editing-line" ? feature : undefined,
        { hitTolerance: 8, layerFilter: layer => layer === handle.editingLineLayer },
    );
    if (line) return;

    if (!handle.editingPointAddEnabled) return;
    const wgs84 = toLonLat(coordinate);
    void handle.dotNetViewport.invokeMethodAsync("AddEditingPoint", wgs84[0], wgs84[1]);
}

function notifyEditingContextMenu(handle: MapHandle, event: MouseEvent): void {
    if (!handle.editingEnabled) return;
    const pixel = handle.map.getEventPixel(event);
    const point = handle.map.forEachFeatureAtPixel(
        pixel,
        feature => feature.get("kind") === "editing-point" ? feature : undefined,
        { hitTolerance: 8, layerFilter: layer => layer === handle.editingPointLayer },
    );
    if (!point) return;

    event.preventDefault();
    void handle.dotNetViewport.invokeMethodAsync(
        "ShowEditingPointMenu",
        point.get("editIndex") as number,
        pixel[0],
        pixel[1],
    );
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
    if (handle.contextMenuListener) {
        handle.map.getViewport().removeEventListener("contextmenu", handle.contextMenuListener);
    }
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
