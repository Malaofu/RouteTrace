import { Circle as CircleStyle, Fill, Icon, Stroke, Style } from "ol/style.js";
import type { StyleFunction } from "ol/style/Style.js";
import type { MarkerConfig } from "./mapContracts.js";

const iconCache = new Map<string, Icon>();
// esbuild preserves import.meta.url as the generated entry module URL.
const markerAssetRoot = new URL("../images/map-markers/", import.meta.url);
const endpointMarkerRadius = 10;
const endpointMarkerSize = endpointMarkerRadius * 2;
let markerConfig: MarkerConfig | null = null;

export function configureMarkerStyles(config: MarkerConfig): void {
    markerConfig = config;
}

export function createFeatureStyle(
    selectedTrack: number | null,
    selectedSegment: number | null,
): StyleFunction {
    return (feature): Style | Style[] | undefined => {
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

        switch (kind) {
            case "waypoint":
                return poiStyles(feature.get("symbolKey") as string, colour, semanticSelected);
            case "endpoint":
                return endpointStyle(
                    feature.get("endpointKind") === "start",
                    feature.get("endpointOverlap") === true,
                );
            case "route":
                return routeStyle(colour, semanticSelected, activeDocument);
            default:
                return trackStyle(
                    colour,
                    selected || incrementallySelected || selectedWholeDocument === true,
                    selectedDocument,
                    activeDocument,
                );
        }
    };
}

function poiStyles(symbolKey: string, colour: string, selected: boolean): Style[] {
    const markers = configuredMarkers();
    const scale = selected ? markers.selectedPinScale : markers.pinScale;
    const common = { scale, anchor: [0.5, 1] as [number, number] };
    return [
        new Style({
            image: cachedIcon(`pin-fill|${colour}|${scale}`, {
                ...common,
                src: markerAsset(markers.assets.pinFill),
                color: colour,
            }),
            zIndex: 30,
        }),
        new Style({
            image: cachedIcon(`pin-outline|${scale}`, {
                ...common,
                src: markerAsset(markers.assets.pinOutline),
            }),
            zIndex: 31,
        }),
        new Style({
            image: cachedIcon(`${symbolKey}|${scale}`, {
                ...common,
                src: markerAsset(symbolKey),
            }),
            zIndex: 32,
        }),
    ];
}

function endpointStyle(start: boolean, overlap: boolean): Style {
    return new Style({
        image: start
            ? new CircleStyle({
                radius: endpointMarkerRadius,
                displacement: overlap ? [-8, 0] : [0, 0],
                fill: new Fill({ color: "#16803c" }),
                stroke: new Stroke({ color: "#ffffff", width: 2 }),
            })
            : finishIcon(overlap),
        zIndex: start ? 41 : 42,
    });
}

function routeStyle(colour: string, selected: boolean, active: boolean): Style {
    return new Style({
        stroke: new Stroke({
            color: colour,
            width: selected ? 7 : active ? 5 : 3,
            lineDash: [8, 6],
        }),
        zIndex: selected ? 20 : 1,
    });
}

function trackStyle(
    colour: string,
    selected: boolean,
    selectedDocument: boolean,
    activeDocument: boolean,
): Style {
    return new Style({
        image: new CircleStyle({
            radius: selected ? 7 : 5,
            fill: new Fill({ color: colour }),
            stroke: new Stroke({ color: "#ffffff", width: 2 }),
        }),
        stroke: new Stroke({
            color: colour,
            width: selected ? 7 : activeDocument ? 5 : 3,
        }),
        zIndex: selected ? 20 : selectedDocument ? 15 : activeDocument ? 10 : 1,
    });
}

function finishIcon(overlap: boolean): Icon {
    const markers = configuredMarkers();
    return cachedIcon(`finish|${overlap}`, {
        src: markerAsset(markers.assets.finish),
        width: endpointMarkerSize,
        height: endpointMarkerSize,
        displacement: overlap ? [8, 0] : [0, 0],
    });
}

function markerAsset(name: string): string {
    if (!/^[a-z0-9-]+$/.test(name)) {
        throw new Error(`Invalid marker asset name '${name}'.`);
    }

    return new URL(`${name}.svg`, markerAssetRoot).href;
}

function cachedIcon(key: string, options: ConstructorParameters<typeof Icon>[0]): Icon {
    let icon = iconCache.get(key);
    if (!icon) {
        icon = new Icon(options);
        iconCache.set(key, icon);
    }

    return icon;
}

function configuredMarkers(): MarkerConfig {
    if (!markerConfig) {
        throw new Error("Map marker configuration has not been initialized.");
    }

    return markerConfig;
}
