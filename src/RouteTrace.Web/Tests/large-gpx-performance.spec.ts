import { expect, test } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const fixturePath = path.resolve(
    "../../tests/RouteTrace.TestData/FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");

test("loads and renders the full-density GPX with measured phases", async ({ page }) => {
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    await page.locator("input[type=file]").setInputFiles(fixturePath);

    await expect(page.getByText(/6987 point\(s\)/)).toBeVisible();
    await page.waitForFunction(() =>
        performance.getEntriesByName("routeTrace.map.render.end").length > 0);

    const timings = await page.evaluate(() => {
        const mark = (name: string) => performance.getEntriesByName(name).at(-1)!.startTime;
        const start = mark("routeTrace.import.start");
        const parsed = mark("routeTrace.import.parsed");
        const busyRendered = mark("routeTrace.import.busy-rendered");
        const mapStart = mark("routeTrace.map.render.start");
        const mapEnd = mark("routeTrace.map.render.end");
        const inspectorRendered = mark("routeTrace.inspector.rendered");
        return {
            parseMs: parsed - start,
            busyFeedbackMs: busyRendered - start,
            propagationAndInteropMs: mapStart - parsed,
            mapRenderMs: mapEnd - mapStart,
            inspectorRenderMs: inspectorRendered - parsed,
            totalMs: Math.max(mapEnd, inspectorRendered) - start,
        };
    });

    console.log(`GPX UI timings: ${JSON.stringify(timings)}`);
    expect(timings.busyFeedbackMs).toBeLessThan(100);
    expect(timings.totalMs).toBeLessThan(2_000);
});

test("profiles costly GPX features in the browser", async ({ page }) => {
    test.slow();
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    const source = fs.readFileSync(fixturePath, "utf8");
    const variants = [
        ["full", source],
        ["without extensions", source.replace(/\s*<extensions>[\s\S]*?<\/extensions>/g, "")],
        ["without timestamps", source.replace(/\s*<time>[^<]*<\/time>/g, "")],
        ["geometry only", source
            .replace(/\s*<extensions>[\s\S]*?<\/extensions>/g, "")
            .replace(/\s*<(?:time|ele)>[^<]*<\/(?:time|ele)>/g, "")],
    ] as const;

    const results: Record<string, number> = {};
    for (const [name, contents] of variants) {
        await page.goto("/");
        await page.locator("input[type=file]").setInputFiles({
            name: `${name}.gpx`,
            mimeType: "application/gpx+xml",
            buffer: Buffer.from(contents),
        });
        await page.waitForFunction(() =>
            performance.getEntriesByName("routeTrace.import.parsed").length > 0);
        results[name] = await page.evaluate(() => {
            const start = performance.getEntriesByName("routeTrace.import.start").at(-1)!.startTime;
            const parsed = performance.getEntriesByName("routeTrace.import.parsed").at(-1)!.startTime;
            return parsed - start;
        });
    }

    console.log(`GPX parser profile: ${JSON.stringify(results)}`);
    expect(results.full).toBeGreaterThan(0);
});
