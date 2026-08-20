import Feature, { type FeatureLike } from "ol/Feature.js";
import LineString from "ol/geom/LineString.js";
import Point from "ol/geom/Point.js";
import { fromLonLat } from "ol/proj.js";
import { Circle as CircleStyle, Fill, RegularShape, Stroke, Style } from "ol/style.js";
import type { Wgs84Coordinate } from "./mapContracts.js";
import type { MapHandle } from "./mapLifecycle.js";
import { endpointStyle } from "./mapStyles.js";

const lineStroke = new Stroke({ color: "#f97316", width: 5 });
const arrowSpacingPixels = 140;
const maximumDirectionArrows = 20;

export function setManualRouteEditing(
    handle: MapHandle,
    enabled: boolean,
    pointAddEnabled: boolean,
    geometry: Wgs84Coordinate[],
    anchors: Wgs84Coordinate[],
    selectedIndex: number | null,
): void {
    handle.editingEnabled = enabled;
    handle.editingPointAddEnabled = pointAddEnabled;
    if (!enabled) handle.map.getTargetElement().dataset.editingLive = "false";
    handle.editingModify.setActive(enabled && anchors.length > 0);
    handle.editingLineSource.clear();
    handle.editingPointSource.clear();
    if (!enabled) {
        handle.map.getTargetElement().dataset.editingDirectionArrows = "0";
        return;
    }

    const projectedGeometry = geometry.map(point => fromLonLat(point));
    if (projectedGeometry.length > 0) {
        const line = new Feature({
            geometry: projectedGeometry.length === 1
                ? new Point(projectedGeometry[0]!)
                : new LineString(projectedGeometry),
            kind: "editing-line",
        });
        line.setStyle(directionStyles);
        handle.editingLineSource.addFeature(line);
    }

    const closesLoop = geometry.length >= 3 &&
        geometry[0]?.[0] === geometry.at(-1)?.[0] &&
        geometry[0]?.[1] === geometry.at(-1)?.[1];
    const projectedAnchors = anchors.map(point => fromLonLat(point));
    let geometrySearchStart = 0;
    projectedAnchors.forEach((coordinate, index) => {
        const geometryIndex = findCoordinateIndex(projectedGeometry, coordinate, geometrySearchStart);
        geometrySearchStart = Math.max(geometrySearchStart, geometryIndex + 1);
        const point = new Feature({
            geometry: new Point(coordinate),
            kind: "editing-point",
            editIndex: index,
            geometryIndex,
        });
        point.setStyle(editingPointStyles(
            index,
            anchors.length,
            index === selectedIndex,
            closesLoop,
        ));
        handle.editingPointSource.addFeature(point);
    });
    refreshEditingPointPixels(handle);
}

export function refreshEditingPointPixels(handle: MapHandle): void {
    const pixels = handle.editingPointSource.getFeatures().map(feature => {
        const geometry = feature.getGeometry();
        return geometry instanceof Point ? handle.map.getPixelFromCoordinate(geometry.getCoordinates()) : null;
    }).filter(pixel => pixel !== null);
    const target = handle.map.getTargetElement();
    target.dataset.editingPointPixels = JSON.stringify(pixels);
    const line = handle.editingLineSource.getFeatures()[0]?.getGeometry();
    const resolution = handle.map.getView().getResolution();
    target.dataset.editingDirectionArrows = line instanceof LineString && resolution
        ? directionArrowPlacements(line.getCoordinates(), resolution).length.toString()
        : "0";
}

function findCoordinateIndex(geometry: number[][], anchor: number[], start: number): number {
    for (let index = start; index < geometry.length; index++) {
        const candidate = geometry[index];
        if (candidate?.[0] === anchor[0] && candidate?.[1] === anchor[1]) return index;
    }
    return start;
}

function directionStyles(feature: FeatureLike, resolution: number): Style[] {
    const geometry = feature.getGeometry();
    if (!(geometry instanceof LineString)) return [];

    const styles = [new Style({ stroke: lineStroke, zIndex: 50 })];
    for (const placement of directionArrowPlacements(geometry.getCoordinates(), resolution)) {
        styles.push(new Style({
            geometry: new Point(placement.coordinate),
            image: new RegularShape({
                points: 3,
                radius: 7,
                rotation: placement.rotation,
                angle: Math.PI / 2,
                fill: new Fill({ color: "#f97316" }),
                stroke: new Stroke({ color: "#ffffff", width: 1.5 }),
            }),
            zIndex: 51,
        }));
    }
    return styles;
}

interface DirectionArrowPlacement {
    coordinate: number[];
    rotation: number;
}

function directionArrowPlacements(coordinates: number[][], resolution: number): DirectionArrowPlacement[] {
    if (coordinates.length < 2 || !Number.isFinite(resolution) || resolution <= 0) return [];
    const segments: { start: number[]; finish: number[]; length: number }[] = [];
    let totalLength = 0;
    for (let index = 1; index < coordinates.length; index++) {
        const start = coordinates[index - 1];
        const finish = coordinates[index];
        if (!start || !finish) continue;
        const length = Math.hypot(finish[0]! - start[0]!, finish[1]! - start[1]!);
        if (length <= 0) continue;
        segments.push({ start, finish, length });
        totalLength += length;
    }
    const spacing = Math.max(
        arrowSpacingPixels * resolution,
        totalLength / (maximumDirectionArrows + .5),
    );
    const placements: DirectionArrowPlacement[] = [];
    let travelled = 0;
    let nextArrow = spacing / 2;
    for (const segment of segments) {
        while (nextArrow <= travelled + segment.length && placements.length < maximumDirectionArrows) {
            const fraction = (nextArrow - travelled) / segment.length;
            const deltaX = segment.finish[0]! - segment.start[0]!;
            const deltaY = segment.finish[1]! - segment.start[1]!;
            placements.push({
                coordinate: [
                    segment.start[0]! + deltaX * fraction,
                    segment.start[1]! + deltaY * fraction,
                ],
                rotation: -Math.atan2(deltaY, deltaX),
            });
            nextArrow += spacing;
        }
        travelled += segment.length;
    }
    return placements;
}

function editingPointStyles(
    index: number,
    count: number,
    selected: boolean,
    closesLoop: boolean,
): Style[] {
    const styles: Style[] = [];
    if (index === 0) styles.push(endpointStyle(true, closesLoop || count === 1));
    if ((closesLoop && index === 0) || (!closesLoop && index === count - 1)) {
        styles.push(endpointStyle(false, closesLoop || count === 1));
    }
    if (index > 0 && index < count - 1) styles.push(editingPointStyle(selected));
    if (selected) styles.push(editingSelectionStyle());
    return styles;
}

function editingPointStyle(selected: boolean): Style {
    return new Style({
        image: new CircleStyle({
            radius: selected ? 9 : 7,
            fill: new Fill({ color: selected ? "#ffffff" : "#f97316" }),
            stroke: new Stroke({ color: selected ? "#f97316" : "#ffffff", width: 3 }),
        }),
        zIndex: selected ? 61 : 60,
    });
}

function editingSelectionStyle(): Style {
    return new Style({
        image: new CircleStyle({
            radius: 13,
            fill: new Fill({ color: "transparent" }),
            stroke: new Stroke({ color: "#f97316", width: 3 }),
        }),
        zIndex: 62,
    });
}
