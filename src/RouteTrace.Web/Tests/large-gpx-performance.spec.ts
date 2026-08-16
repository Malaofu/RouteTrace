import { expect, test } from "@playwright/test";
import type { Browser } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const fixturePath = path.resolve(
    "../../tests/RouteTrace.TestData/FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");

interface ImportTimings {
    parseMs: number;
    busyFeedbackMs: number;
    propagationAndInteropMs: number;
    mapRenderMs: number;
    inspectorRenderMs: number;
    totalMs: number;
}

async function measureImport(browser: Browser): Promise<ImportTimings> {
    const context = await browser.newContext();
    const page = await context.newPage();
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    await page.locator("input[type=file]").setInputFiles(fixturePath);

    await expect(page.getByText(/6987 point\(s\)/)).toBeVisible();
    await page.waitForFunction(() =>
        performance.getEntriesByName("routeTrace.map.render.end").length > 0);

    const timings: ImportTimings = await page.evaluate(() => {
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
    await context.close();
    return timings;
}

function median(values: number[]): number {
    const ordered = values.toSorted((left, right) => left - right);
    return ordered[Math.floor(ordered.length / 2)];
}

test("loads and renders the full-density GPX with measured phases", async ({ browser }) => {
    const samples: ImportTimings[] = [];
    for (let sample = 0; sample < 3; sample++) samples.push(await measureImport(browser));
    const medianBusyFeedbackMs = median(samples.map(sample => sample.busyFeedbackMs));
    const medianTotalMs = median(samples.map(sample => sample.totalMs));

    console.log(`GPX UI timing samples: ${JSON.stringify(samples)}`);
    console.log(`GPX UI medians: ${JSON.stringify({ busyFeedbackMs: medianBusyFeedbackMs, totalMs: medianTotalMs })}`);
    expect(medianBusyFeedbackMs).toBeLessThan(100);
    const completionBudgetMs = process.env.ROUTETRACE_PUBLISHED_ROOT ? 500 : 2_000;
    expect(medianTotalMs).toBeLessThan(completionBudgetMs);
});

test("exports the full-density GPX within the UI budget", async ({ page }) => {
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    await page.locator("input[type=file]").setInputFiles(fixturePath);
    await expect(page.getByText(/6987 point\(s\)/)).toBeVisible();

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: "File", exact: true }).click();
    await page.getByRole("menuitem", { name: /Download GPX/ }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe("FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");
    await page.waitForFunction(() =>
        performance.getEntriesByName("routeTrace.export.downloaded").length > 0);

    const timings = await page.evaluate(() => {
        const mark = (name: string) => performance.getEntriesByName(name).at(-1)!.startTime;
        const start = mark("routeTrace.export.start");
        const serialized = mark("routeTrace.export.serialized");
        const downloaded = mark("routeTrace.export.downloaded");
        return {
            serializationMs: serialized - start,
            downloadInteropMs: downloaded - serialized,
            totalMs: downloaded - start,
        };
    });

    console.log(`GPX export UI timings: ${JSON.stringify(timings)}`);
    const completionBudgetMs = process.env.ROUTETRACE_PUBLISHED_ROOT ? 500 : 5_000;
    expect(timings.totalMs).toBeLessThan(completionBudgetMs);
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
