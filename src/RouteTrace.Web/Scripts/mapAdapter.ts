import type {
    DocumentPresentation,
    DotNetViewport,
    ImportedGeometry,
    MarkerConfig,
    Wgs84Bounds,
} from "./Map/mapContracts.js";
import {
    endDocumentUpdate as endGeometryUpdate,
    focusSelection as focusGeometrySelection,
    removeDocument as removeGeometryDocument,
    setDocumentPresentation as applyDocumentPresentation,
    upsertDocument as upsertGeometryDocument,
} from "./Map/mapGeometry.js";
import {
    disposeMap,
    fitMapBounds,
    getMap,
    initializeMap,
} from "./Map/mapLifecycle.js";
import { setManualRouteEditing as applyManualRouteEditing } from "./Map/manualRouteEditing.js";

let markerConfig: MarkerConfig | null = null;

export function initialize(
    elementId: string,
    config: MarkerConfig,
    dotNetViewport: DotNetViewport,
): void {
    markerConfig = config;
    initializeMap(elementId, config, dotNetViewport);
}

export function removeDocument(elementId: string, documentId: string): void {
    removeGeometryDocument(getMap(elementId), documentId);
}

export function upsertDocument(
    elementId: string,
    documentId: string,
    geometry: ImportedGeometry,
    colour: string,
): void {
    const config = configuredMarkers();
    upsertGeometryDocument(
        getMap(elementId),
        documentId,
        geometry,
        colour,
        config.symbols,
        config.defaultIcon,
    );
}

export function setDocumentPresentation(
    elementId: string,
    documentId: string,
    colour: string,
    active: boolean,
    selected: boolean,
    presentation: DocumentPresentation[],
    selectedTrack: number | null,
    selectedSegment: number | null,
    selectedRoute: number | null,
    selectedWaypoint: number | null,
    selectedWholeDocument: boolean,
    selectedWaypointGroup: boolean,
): void {
    applyDocumentPresentation(
        getMap(elementId),
        documentId,
        colour,
        active,
        selected,
        presentation,
        selectedTrack,
        selectedSegment,
        selectedRoute,
        selectedWaypoint,
        selectedWholeDocument,
        selectedWaypointGroup,
    );
}

export function endDocumentUpdate(elementId: string, fitGeometry: boolean): void {
    endGeometryUpdate(getMap(elementId), fitGeometry);
}

export function setManualRouteEditing(
    elementId: string,
    enabled: boolean,
    pointAddEnabled: boolean,
    geometry: Array<[number, number]>,
    anchors: Array<[number, number]>,
    selectedIndex: number | null,
): void {
    applyManualRouteEditing(getMap(elementId), enabled, pointAddEnabled, geometry, anchors, selectedIndex);
}

export function focusSelection(
    elementId: string,
    documentId: string,
    trackIndex: number | null,
    segmentIndex: number | null,
    routeIndex: number | null,
    waypointIndex: number | null,
    wholeDocument: boolean,
    waypointGroup: boolean,
    animated: boolean,
): void {
    focusGeometrySelection(
        getMap(elementId),
        documentId,
        trackIndex,
        segmentIndex,
        routeIndex,
        waypointIndex,
        wholeDocument,
        waypointGroup,
        animated,
    );
}

export function fitBounds(
    elementId: string,
    bounds: Wgs84Bounds,
    padding = 48,
): void {
    fitMapBounds(elementId, bounds, padding);
}

export function dispose(elementId: string): void {
    disposeMap(elementId);
}

function configuredMarkers(): MarkerConfig {
    if (!markerConfig) {
        throw new Error("Map marker configuration has not been initialized.");
    }

    return markerConfig;
}
