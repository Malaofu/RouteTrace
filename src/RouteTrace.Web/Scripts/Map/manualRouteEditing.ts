import Feature, { type FeatureLike } from "ol/Feature.js";
import LineString from "ol/geom/LineString.js";
import Point from "ol/geom/Point.js";
import { fromLonLat } from "ol/proj.js";
import { Circle as CircleStyle, Fill, RegularShape, Stroke, Style } from "ol/style.js";
import type { Wgs84Coordinate } from "./mapContracts.js";
import type { MapHandle } from "./mapLifecycle.js";
import { endpointStyle } from "./mapStyles.js";

const lineStroke = new Stroke({ color: "#f97316", width: 5 });

export function setManualRouteEditing(
    handle: MapHandle,
    enabled: boolean,
    pointAddEnabled: boolean,
    points: Wgs84Coordinate[],
    selectedIndex: number | null,
): void {
    handle.editingEnabled = enabled;
    handle.editingPointAddEnabled = pointAddEnabled;
    if (!enabled) handle.map.getTargetElement().dataset.editingLive = "false";
    handle.editingModify.setActive(enabled && points.length > 0);
    handle.editingLineSource.clear();
    handle.editingPointSource.clear();
    if (!enabled) return;

    const projected = points.map(point => fromLonLat(point));
    if (projected.length > 0) {
        const line = new Feature({
            geometry: projected.length === 1 ? new Point(projected[0]!) : new LineString(projected),
            kind: "editing-line",
        });
        line.setStyle(directionStyles);
        handle.editingLineSource.addFeature(line);
    }

    const closesLoop = points.length >= 3 &&
        points[0]?.[0] === points.at(-1)?.[0] &&
        points[0]?.[1] === points.at(-1)?.[1];
    const editableCount = points.length - (closesLoop ? 1 : 0);
    projected.slice(0, editableCount).forEach((coordinate, index) => {
        const point = new Feature({
            geometry: new Point(coordinate),
            kind: "editing-point",
            editIndex: index,
        });
        point.setStyle(editingPointStyles(
            index,
            editableCount,
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
    handle.map.getTargetElement().dataset.editingPointPixels = JSON.stringify(pixels);
}

export function synchronizeEditingPointsFromLine(handle: MapHandle): void {
    const geometry = handle.editingLineSource.getFeatures()[0]?.getGeometry();
    if (!(geometry instanceof LineString)) return;

    const coordinates = geometry.getCoordinates();
    const closesLoop = coordinates.length >= 3 &&
        coordinates[0]?.[0] === coordinates.at(-1)?.[0] &&
        coordinates[0]?.[1] === coordinates.at(-1)?.[1];
    const editableCount = coordinates.length - (closesLoop ? 1 : 0);
    handle.editingPointSource.clear();
    coordinates.slice(0, editableCount).forEach((coordinate, index) => {
        const point = new Feature({
            geometry: new Point(coordinate),
            kind: "editing-point",
            editIndex: index,
        });
        point.setStyle(editingPointStyles(index, editableCount, false, closesLoop));
        handle.editingPointSource.addFeature(point);
    });
    refreshEditingPointPixels(handle);
}

function directionStyles(feature: FeatureLike): Style[] {
    const geometry = feature.getGeometry();
    if (!(geometry instanceof LineString)) return [];

    const coordinates = geometry.getCoordinates();
    const styles = [new Style({ stroke: lineStroke, zIndex: 50 })];
    for (let index = 1; index < coordinates.length; index++) {
        const start = coordinates[index - 1];
        const finish = coordinates[index];
        if (!start || !finish) continue;
        const startX = start[0]!;
        const startY = start[1]!;
        const finishX = finish[0]!;
        const finishY = finish[1]!;
        const midpoint = [(startX + finishX) / 2, (startY + finishY) / 2];
        const rotation = -Math.atan2(finishY - startY, finishX - startX);
        styles.push(new Style({
            geometry: new Point(midpoint),
            image: new RegularShape({
                points: 3,
                radius: 8,
                rotation,
                angle: Math.PI / 2,
                fill: new Fill({ color: "#f97316" }),
                stroke: new Stroke({ color: "#ffffff", width: 1.5 }),
            }),
            zIndex: 51,
        }));
    }
    return styles;
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
