import { expect, test } from "@playwright/test";
import path from "node:path";

const fixtures = path.resolve(process.cwd(), "../../tests/RouteTrace.TestData");

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
    await expect(map).toHaveAttribute("data-visible-documents", "3");
    await expect(page.getByLabel("Active document").locator("option")).toHaveCount(3);

    await page.getByLabel("Active document").selectOption({ index: 0 });
    await expect(map).toHaveAttribute("data-visible-documents", "3");

    const firstRow = page.getByRole("listitem").filter({ hasText: "Minimal elevated track" });
    await firstRow.getByRole("button", { name: "Select" }).click();
    await expect(firstRow).toHaveClass(/workspace-panel__document--selected/);

    await page.getByLabel("Show Multiple tracks and segments").uncheck();
    await expect(map).toHaveAttribute("data-visible-documents", "2");
    await page.getByLabel("Show Multiple tracks and segments").check();

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: "File", exact: true }).click();
    await page.getByRole("menuitem", { name: /Download GPX/ }).click();
    expect((await downloadPromise).suggestedFilename()).toBe("Minimal elevated track.gpx");

    await firstRow.getByRole("button", { name: "Close" }).click();
    await expect(page.getByLabel("Active document").locator("option")).toHaveCount(2);
    await expect(map).toHaveAttribute("data-visible-documents", "2");
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

    const largeRow = page.getByRole("listitem").filter({ hasText: "FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx" });
    const smallRow = page.getByRole("listitem").filter({ hasText: "Minimal elevated track" });
    await largeRow.getByRole("button", { name: "Select" }).click();

    const started = Date.now();
    await smallRow.getByRole("button", { name: "Select" }).click();
    await expect(smallRow).toHaveClass(/workspace-panel__document--selected/);
    const elapsed = Date.now() - started;

    expect(elapsed).toBeLessThan(process.env.ROUTETRACE_PUBLISHED_ROOT ? 500 : 2_000);
    await expect(page.getByRole("application", { name: "Interactive route map" }))
        .toHaveAttribute("data-visible-documents", "2");
});
