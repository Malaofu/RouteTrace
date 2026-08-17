import { expect, test } from "@playwright/test";
import type { Browser, Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const fixturePath = path.resolve(
    "../../tests/RouteTrace.TestData/FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");
const busyFeedbackBudgetMs = 100;
const publishedCompletionBudgetMs = 1_000;
const developmentCompletionBudgetMs = 6_000;

interface ImportTimings {
    busyFeedbackMs: number;
    totalMs: number;
}

interface PendingImportTimings {
    startedAt: number | null;
    busyFeedbackMs: number | null;
    totalMs: number | null;
}

type TimingWindow = Window & {
    routeTraceImportTimings?: PendingImportTimings;
};

async function beginImportObservation(page: Page): Promise<void> {
    await page.locator("input[type=file]").waitFor({ state: "attached" });
    await page.evaluate(() => {
        const timingWindow = window as TimingWindow;
        const timings: PendingImportTimings = {
            startedAt: null,
            busyFeedbackMs: null,
            totalMs: null,
        };
        timingWindow.routeTraceImportTimings = timings;
        document.querySelector("input[type=file]")!.addEventListener(
            "change",
            () => timings.startedAt = performance.now(),
            { capture: true, once: true });

        const observer = new MutationObserver(() => {
            if (timings.startedAt === null) return;

            const notice = document.querySelector(".application-menu__notice")?.textContent ?? "";
            if (notice.includes("Reading and processing locally") &&
                timings.busyFeedbackMs === null) {
                timings.busyFeedbackMs = performance.now() - timings.startedAt;
            }
            if (notice.startsWith("Imported ") &&
                timings.totalMs === null) {
                timings.totalMs = performance.now() - timings.startedAt;
                observer.disconnect();
            }
        });
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true,
        });
    });
}

async function completedImportTimings(page: Page): Promise<ImportTimings> {
    await page.waitForFunction(() => {
        const timings = (window as TimingWindow).routeTraceImportTimings;
        return timings?.busyFeedbackMs !== null && timings?.totalMs !== null;
    });
    return page.evaluate(() => {
        const timings = (window as TimingWindow).routeTraceImportTimings!;
        return {
            busyFeedbackMs: timings.busyFeedbackMs!,
            totalMs: timings.totalMs!,
        };
    });
}

async function measureImport(browser: Browser): Promise<ImportTimings> {
    const context = await browser.newContext();
    const page = await context.newPage();
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    await beginImportObservation(page);
    await page.locator("input[type=file]").setInputFiles(fixturePath);

    await expect(page.getByText(/6987 point\(s\)/)).toBeVisible({ timeout: 15_000 });
    const timings = await completedImportTimings(page);
    await context.close();
    return timings;
}

function median(values: number[]): number {
    const ordered = values.toSorted((left, right) => left - right);
    return ordered[Math.floor(ordered.length / 2)];
}

test("loads the full-density GPX within coarse browser budgets", async ({ browser }) => {
    const samples: ImportTimings[] = [];
    for (let sample = 0; sample < 3; sample++) samples.push(await measureImport(browser));
    const medianBusyFeedbackMs = median(samples.map(sample => sample.busyFeedbackMs));
    const medianTotalMs = median(samples.map(sample => sample.totalMs));

    console.log(`GPX UI timing samples: ${JSON.stringify(samples)}`);
    console.log(`GPX UI medians: ${JSON.stringify({ busyFeedbackMs: medianBusyFeedbackMs, totalMs: medianTotalMs })}`);
    expect(medianBusyFeedbackMs).toBeLessThan(busyFeedbackBudgetMs);
    const completionBudgetMs = process.env.ROUTETRACE_PUBLISHED_ROOT
        ? publishedCompletionBudgetMs
        : developmentCompletionBudgetMs;
    expect(medianTotalMs).toBeLessThan(completionBudgetMs);
});

test("exports the full-density GPX within the UI budget", async ({ page }) => {
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    await page.locator("input[type=file]").setInputFiles(fixturePath);
    await expect(page.getByText(/6987 point\(s\)/)).toBeVisible({ timeout: 15_000 });

    await page.getByRole("button", { name: "File", exact: true }).click();
    const downloadPromise = page.waitForEvent("download");
    const startedAt = Date.now();
    await page.getByRole("menuitem", { name: /Download GPX/ }).click();
    const download = await downloadPromise;
    const totalMs = Date.now() - startedAt;
    expect(download.suggestedFilename()).toBe("FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");

    console.log(`GPX export UI timing: ${JSON.stringify({ totalMs })}`);
    const completionBudgetMs = process.env.ROUTETRACE_PUBLISHED_ROOT ? 500 : 5_000;
    expect(totalMs).toBeLessThan(completionBudgetMs);
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
        await beginImportObservation(page);
        await page.locator("input[type=file]").setInputFiles({
            name: `${name}.gpx`,
            mimeType: "application/gpx+xml",
            buffer: Buffer.from(contents),
        });
        await expect(page.getByText(/6987 point\(s\)/)).toBeVisible({ timeout: 15_000 });
        results[name] = (await completedImportTimings(page)).totalMs;
    }

    console.log(`GPX import variant timings: ${JSON.stringify(results)}`);
    expect(results.full).toBeGreaterThan(0);
});
