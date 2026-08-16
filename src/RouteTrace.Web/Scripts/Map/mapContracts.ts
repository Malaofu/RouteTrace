export type Wgs84Bounds = [west: number, south: number, east: number, north: number];

export type Wgs84Coordinate = [longitude: number, latitude: number];

export interface ImportedGeometry {
    tracks: Array<{ segments: Wgs84Coordinate[][] }>;
    routes: Wgs84Coordinate[][];
    waypoints: Array<{
        coordinate: Wgs84Coordinate;
        name: string | null;
        symbol: string | null;
        description: string | null;
        elevationMetres: number | null;
    }>;
    endpoints: Array<{
        ownerKind: "track" | "route";
        ownerIndex: number;
        endpointKind: "start" | "finish";
        coordinate: Wgs84Coordinate;
        overlap: boolean;
    }>;
}

export interface MarkerConfig {
    defaultIcon: string;
    pinScale: number;
    selectedPinScale: number;
    assets: {
        pinFill: string;
        pinOutline: string;
        finish: string;
    };
    symbols: Record<string, string>;
}

export interface DocumentPresentation {
    kind: string;
    primaryIndex: number;
    secondaryIndex: number;
    visible: boolean;
    colour: string;
}

export interface DotNetViewport {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}
