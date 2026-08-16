import Feature from "ol/Feature.js";
import LineString from "ol/geom/LineString.js";
import Point from "ol/geom/Point.js";
import { fromLonLat } from "ol/proj.js";
import type { DocumentPresentation, ImportedGeometry } from "./mapContracts.js";
import type { MapHandle } from "./mapLifecycle.js";

export function beginDocumentUpdate(): void {
    performance.mark("routeTrace.map.render.start");
}

export function removeDocument(handle: MapHandle, documentId: string): void {
    handle.geometrySource.getFeatures()
        .filter(feature => feature.get("documentId") === documentId)
        .forEach(feature => handle.geometrySource.removeFeature(feature));
}

export function upsertDocument(
    handle: MapHandle,
    documentId: string,
    geometry: ImportedGeometry,
    colour: string,
    symbolIcons: Record<string, string>,
    defaultIcon: string,
): void {
    removeDocument(handle, documentId);
    const properties = { documentId, documentColour: colour };

    geometry.tracks.forEach((track, trackIndex) => {
        track.segments.forEach((segment, segmentIndex) => {
            if (segment.length === 0) return;
            handle.geometrySource.addFeature(new Feature({
                ...properties,
                geometry: segment.length === 1
                    ? new Point(fromLonLat(segment[0]))
                    : new LineString(segment.map(coordinate => fromLonLat(coordinate))),
                kind: "track",
                trackIndex,
                segmentIndex,
            }));
        });
    });

    geometry.routes.forEach((route, routeIndex) => {
        if (route.length === 0) return;
        handle.geometrySource.addFeature(new Feature({
            ...properties,
            geometry: route.length === 1
                ? new Point(fromLonLat(route[0]))
                : new LineString(route.map(coordinate => fromLonLat(coordinate))),
            kind: "route",
            routeIndex,
        }));
    });

    geometry.waypoints.forEach((waypoint, waypointIndex) => {
        const symbol = waypoint.symbol?.trim().toLowerCase() ?? "";
        handle.geometrySource.addFeature(new Feature({
            ...properties,
            geometry: new Point(fromLonLat(waypoint.coordinate)),
            kind: "waypoint",
            waypointIndex,
            symbolKey: symbolIcons[symbol] ?? defaultIcon,
            waypointCoordinate: waypoint.coordinate,
        }));
    });

    geometry.endpoints.forEach(endpoint => handle.geometrySource.addFeature(new Feature({
        ...properties,
        geometry: new Point(fromLonLat(endpoint.coordinate)),
        kind: "endpoint",
        ownerKind: endpoint.ownerKind,
        ownerIndex: endpoint.ownerIndex,
        endpointKind: endpoint.endpointKind,
        endpointOverlap: endpoint.overlap,
    })));
}

export function setDocumentPresentation(
    handle: MapHandle,
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
    handle.geometrySource.getFeatures()
        .filter(feature => feature.get("documentId") === documentId)
        .forEach(feature => {
            const kind = feature.get("kind") as string;
            const presentationKind = kind === "endpoint"
                ? feature.get("ownerKind") as string
                : kind;
            const primaryIndex = featurePrimaryIndex(feature, kind);
            const secondaryIndex = presentationKind === "track" && kind !== "endpoint"
                ? feature.get("segmentIndex") as number
                : -1;
            const trackEndpointItems = kind === "endpoint" && presentationKind === "track"
                ? presentation.filter(item =>
                    item.kind === presentationKind && item.primaryIndex === primaryIndex)
                : [];
            const override = trackEndpointItems.find(item => item.visible) ??
                presentation.find(item =>
                    item.kind === presentationKind &&
                    item.primaryIndex === primaryIndex &&
                    item.secondaryIndex === secondaryIndex);

            feature.setProperties({
                documentColour: override?.colour ?? colour,
                presentationVisible: trackEndpointItems.length > 0
                    ? trackEndpointItems.some(item => item.visible)
                    : override?.visible ?? true,
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

export function endDocumentUpdate(handle: MapHandle, fitGeometry: boolean): void {
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

export function focusSelection(
    handle: MapHandle,
    documentId: string,
    trackIndex: number | null,
    segmentIndex: number | null,
    routeIndex: number | null,
    waypointIndex: number | null,
    wholeDocument: boolean,
    waypointGroup: boolean,
): void {
    const features = handle.geometrySource.getFeatures().filter(feature => {
        if (feature.get("documentId") !== documentId ||
            feature.get("presentationVisible") === false) {
            return false;
        }
        if (wholeDocument) return true;

        switch (feature.get("kind") as string) {
            case "track":
                return trackIndex !== null &&
                    feature.get("trackIndex") === trackIndex &&
                    (segmentIndex === null || feature.get("segmentIndex") === segmentIndex);
            case "route":
                return routeIndex !== null && feature.get("routeIndex") === routeIndex;
            case "waypoint":
                return waypointIndex !== null
                    ? feature.get("waypointIndex") === waypointIndex
                    : waypointGroup;
            default:
                return false;
        }
    });
    if (features.length === 0) return;

    const extent = features[0].getGeometry()!.getExtent().slice() as [number, number, number, number];
    features.slice(1).forEach(feature => {
        const next = feature.getGeometry()!.getExtent();
        extent[0] = Math.min(extent[0], next[0]);
        extent[1] = Math.min(extent[1], next[1]);
        extent[2] = Math.max(extent[2], next[2]);
        extent[3] = Math.max(extent[3], next[3]);
    });
    handle.map.getView().fit(extent, {
        duration: 250,
        maxZoom: 18,
        padding: [64, 64, 64, 64],
    });
}

function featurePrimaryIndex(feature: Feature, kind: string): number {
    switch (kind) {
        case "endpoint":
            return feature.get("ownerIndex") as number;
        case "track":
            return feature.get("trackIndex") as number;
        case "route":
            return feature.get("routeIndex") as number;
        default:
            return feature.get("waypointIndex") as number;
    }
}
