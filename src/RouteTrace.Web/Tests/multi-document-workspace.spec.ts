import { expect, test } from "@playwright/test";
import path from "node:path";

const fixtures = path.resolve(process.cwd(), "../../tests/RouteTrace.TestData");

test("loads marker assets configured through app settings", async ({ page }) => {
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    const loadedMarkers = new Set<string>();
    page.on("response", response => {
        const match = new URL(response.url()).pathname.match(/\/images\/map-markers\/([^/]+\.svg)$/);
        if (match && response.ok()) loadedMarkers.add(match[1]);
    });
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(
        path.join(fixtures, "FX-GPX-004-gpx-studio-supplemented.gpx"));
    await expect(page.getByText(/Imported FX-GPX-004/)).toBeVisible();

    await expect.poll(() => [...loadedMarkers]).toEqual(expect.arrayContaining([
        "pin-fill.svg",
        "pin-outline.svg",
        "park.svg",
        "shop.svg",
        "parking.svg",
        "finish.svg",
    ]));
});

test("manages three visible documents independently", async ({ page }) => {
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    const fileInput = page.locator('input[type="file"]');
    for (const file of [
        "FX-GPX-001-minimal-track.gpx",
        "FX-GPX-003-multiple-tracks-segments.gpx",
        "FX-GPX-004-gpx-studio-supplemented.gpx",
    ]) {
        await fileInput.setInputFiles(path.join(fixtures, file));
        await expect(page.getByText(new RegExp(`Imported ${file}`))).toBeVisible();
    }

    await fileInput.setInputFiles({
        name: "invalid.gpx",
        mimeType: "application/gpx+xml",
        buffer: Buffer.from("not GPX"),
    });
    await expect(page.getByRole("alert")).toBeVisible();

    const map = page.getByRole("application", { name: "Interactive route map" });
    const explorer = page.getByLabel("Document explorer");
    await expect(map).toHaveAttribute("data-visible-documents", "3");
    await expect(explorer.locator(".document-explorer__document")).toHaveCount(3);

    const firstRow = explorer.locator('.document-explorer__document[data-document-name="Minimal elevated track"]');
    await firstRow.getByRole("button", { name: "Minimal elevated track", exact: true }).click();
    await expect(map).toHaveAttribute("data-visible-documents", "3");
    await expect(firstRow.locator(".document-explorer__row").first()).toHaveClass(/document-explorer__row--active/);

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: "File", exact: true }).click();
    await page.getByRole("menuitem", { name: /Download GPX/ }).click();
    expect((await downloadPromise).suggestedFilename()).toBe("Minimal elevated track.gpx");

});

test("selecting a small document does not reprocess unchanged large geometry", async ({ page }) => {
    test.slow();
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles(path.join(fixtures, "FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx"));
    await expect(page.getByText(/Imported FX-GPX-002-a/)).toBeVisible();
    await fileInput.setInputFiles(path.join(fixtures, "FX-GPX-001-minimal-track.gpx"));
    await expect(page.getByText(/Imported FX-GPX-001/)).toBeVisible();

    const explorer = page.getByLabel("Document explorer");
    const largeRow = explorer.locator('.document-explorer__document[data-document-name="FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx"]');
    const smallRow = explorer.locator('.document-explorer__document[data-document-name="Minimal elevated track"]');
    await largeRow.getByRole("button", { name: "FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx", exact: true }).click();

    const started = Date.now();
    await smallRow.getByRole("button", { name: "Minimal elevated track", exact: true }).click();
    await expect(smallRow.locator(".document-explorer__row").first()).toHaveClass(/document-explorer__row--active/);
    const elapsed = Date.now() - started;

    expect(elapsed).toBeLessThan(process.env.ROUTETRACE_PUBLISHED_ROOT ? 500 : 2_000);
    await expect(page.getByRole("application", { name: "Interactive route map" }))
        .toHaveAttribute("data-visible-documents", "2");
});

test("explorer shows the semantic GPX hierarchy without individual track points", async ({ page }) => {
    await page.route("https://tile.openstreetmap.org/**", route => route.abort());
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(path.join(fixtures, "FX-GPX-005-full-schema-surface.gpx"));

    const explorer = page.getByLabel("Document explorer");
    await expect(explorer.getByText("Complete track", { exact: false })).toBeVisible();
    await expect(explorer.getByText("Complete route", { exact: false })).toBeVisible();
    await expect(explorer.getByText("Points of interest", { exact: false })).toBeVisible();
    await expect(explorer.getByText("Complete track point", { exact: true })).toHaveCount(0);
    await expect(explorer.locator(".document-explorer__colour-node")).toHaveCount(3);
    await expect(explorer.getByRole("img", { name: /visible/ })).toHaveCount(0);
    await expect(explorer.locator(".document-explorer__expander svg")).toHaveCount(3);

    const completeDocument = explorer.locator('.document-explorer__document[data-document-name="Full GPX 1.1 surface"]');
    await completeDocument.locator(".document-explorer__label").filter({ hasText: "Complete track" }).click();
    await expect(completeDocument.locator(".document-explorer__colour-node").first().locator(".document-explorer__row--active")).toHaveCount(2);

    await explorer.getByRole("button", { name: /Points of interest/ }).click();
    await expect(page.getByRole("application", { name: "Interactive route map" })).toHaveAttribute("data-selected-waypoint-group", "true");
    await expect(completeDocument.locator(".document-explorer__row--active")).toHaveCount(2);

    await completeDocument.getByRole("button", { name: "Full GPX 1.1 surface", exact: true }).click();
    await expect(page.getByRole("application", { name: "Interactive route map" })).toHaveAttribute("data-selected-document", "true");
    await expect(completeDocument.locator(".document-explorer__row--active")).toHaveCount(6);

    await explorer.getByRole("button", { name: /Complete route/ }).click();
    await expect(page.getByRole("application", { name: "Interactive route map" })).toHaveAttribute("data-selected-route", "0");
    await expect(explorer.getByRole("button", { name: /Complete route/ }).locator("..")).toHaveClass(/document-explorer__row--active/);

    const trackExpander = explorer.getByRole("button", { name: "Collapse Complete track" });
    await trackExpander.click();
    await explorer.getByRole("button", { name: "Complete waypoint", exact: true }).click();
    await expect(explorer.getByRole("button", { name: "Expand Complete track" })).toHaveAttribute("aria-expanded", "false");
    await expect(page.getByRole("application", { name: "Interactive route map" })).toHaveAttribute("data-selected-waypoint", "0");
});
