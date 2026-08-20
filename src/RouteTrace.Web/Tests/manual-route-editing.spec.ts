import { expect, test } from "@playwright/test";
import path from "node:path";

const minimalTrack = path.resolve("../../tests/RouteTrace.TestData/FX-GPX-001-minimal-track.gpx");
const structuralGpx = path.resolve("../../tests/RouteTrace.TestData/FX-GPX-005-full-schema-surface.gpx");

test.beforeEach(async ({ page }) => {
    await page.route("https://brouter.de/brouter?**", async route => {
        const request = new URL(route.request().url());
        const anchors = (request.searchParams.get("lonlats") ?? "").split("|").map(value =>
            value.split(",").map(Number));
        const start = anchors[0]!;
        const finish = anchors.at(-1)!;
        const coordinates = Array.from({ length: 31 }, (_, index) => {
            const fraction = index / 30;
            return [
                start[0]! + (finish[0]! - start[0]!) * fraction,
                start[1]! + (finish[1]! - start[1]!) * fraction,
            ];
        });
        await route.fulfill({
            contentType: "application/json",
            body: JSON.stringify({
                type: "FeatureCollection",
                features: [{
                    type: "Feature",
                    geometry: { type: "LineString", coordinates },
                }],
            }),
        });
    });
});

async function storedWorkspacePayload(page: import("@playwright/test").Page): Promise<string> {
    return page.evaluate(async () => {
        const database = await new Promise<IDBDatabase>((resolve, reject) => {
            const request = indexedDB.open("route-trace", 1);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
        try {
            return await new Promise<string>((resolve, reject) => {
                const request = database.transaction("workspaces", "readonly").objectStore("workspaces").getAll();
                request.onsuccess = () => resolve(request.result[0]?.payload ?? "");
                request.onerror = () => reject(request.error);
            });
        } finally {
            database.close();
        }
    });
}

async function editingPointPixels(map: import("@playwright/test").Locator): Promise<number[][]> {
    return JSON.parse(await map.getAttribute("data-editing-point-pixels") ?? "[]") as number[][];
}

function widestSegmentMidpoint(pixels: number[][]): number[] {
    let widest = 0;
    let midpoint = [0, 0];
    for (let index = 1; index < pixels.length; index++) {
        const before = pixels[index - 1]!;
        const after = pixels[index]!;
        const width = Math.hypot(after[0]! - before[0]!, after[1]! - before[1]!);
        if (width > widest) {
            widest = width;
            midpoint = [(before[0]! + after[0]!) / 2, (before[1]! + after[1]!) / 2];
        }
    }
    return midpoint;
}

test("manual route editing supports ordered edits, history, loops, and GPX export", async ({ page }) => {
    let routingRequests = 0;
    page.on("request", request => {
        if (request.url().startsWith("https://brouter.de/brouter?")) routingRequests++;
    });
    await page.goto("/");
    const map = page.getByRole("application", { name: "Interactive route map" });
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    await page.locator('input[type="file"]').setInputFiles(minimalTrack);
    await explorer.locator(".document-explorer__label").first().click({ button: "right" });
    await page.getByRole("menuitem", { name: "Delete" }).click();
    await explorer.click({ button: "right", position: { x: 24, y: 70 } });
    await page.getByRole("menuitem", { name: "New document" }).click();
    await explorer.locator(".document-explorer__label", { hasText: "Untitled document" }).click({ button: "right" });
    await page.getByRole("menuitem", { name: "New route" }).click();
    await explorer.locator(".document-explorer__label", { hasText: "Route 1" }).click({ button: "right" });
    await page.getByRole("menuitem", { name: "New segment" }).click();
    await explorer.locator(".document-explorer__label", { hasText: "Segment 1" }).click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();
    await expect(map).toHaveAttribute("data-editing", "true");
    await expect(map.locator(".ol-viewport")).toBeVisible({ timeout: 15_000 });

    const bounds = await map.boundingBox();
    if (!bounds) throw new Error("Expected map bounds.");
    const clickMap = async (x: number, y: number, pointCount: number) => {
        await page.mouse.click(bounds.x + x, bounds.y + y);
        await expect(map).toHaveAttribute("data-editing-points", pointCount.toString());
    };

    await clickMap(bounds.width * .5, bounds.height * .38, 1);
    await clickMap(bounds.width * .65, bounds.height * .43, 2);
    await clickMap(bounds.width * .8, bounds.height * .34, 3);
    await expect.poll(async () => Number(await map.getAttribute("data-editing-direction-arrows")))
        .toBeGreaterThan(0);
    expect(Number(await map.getAttribute("data-editing-direction-arrows"))).toBeLessThanOrEqual(20);
    await expect(map).toHaveAttribute("data-selected-editing-point", "2");
    const beforeMove = await storedWorkspacePayload(page);
    const anchorsBeforeMove = await editingPointPixels(map);
    const middleAnchor = anchorsBeforeMove[1]!;
    const requestsBeforeMove = routingRequests;
    await page.mouse.move(bounds.x + middleAnchor[0]!, bounds.y + middleAnchor[1]!);
    await page.mouse.down();
    await page.mouse.move(bounds.x + middleAnchor[0]! - 20, bounds.y + middleAnchor[1]! + 20, { steps: 5 });
    await expect(map).toHaveAttribute("data-editing-live", "true");
    await page.mouse.up();
    await expect.poll(() => routingRequests - requestsBeforeMove).toBe(2);
    await expect.poll(() => storedWorkspacePayload(page)).not.toBe(beforeMove);

    let pixels = await editingPointPixels(map);
    await page.mouse.click(
        bounds.x + (pixels[1]![0]! + pixels[2]![0]!) / 2,
        bounds.y + (pixels[1]![1]! + pixels[2]![1]!) / 2,
    );
    await expect(map).toHaveAttribute("data-editing-points", "4");
    await page.getByRole("button", { name: "Delete anchor" }).click();
    await expect(map).toHaveAttribute("data-editing-points", "3");

    await page.getByRole("button", { name: "Reverse" }).click();
    await page.getByRole("button", { name: "Close loop" }).click();
    await expect(map).toHaveAttribute("data-editing-points", "3");
    await page.getByRole("button", { name: "Clear" }).click();
    await expect(map).toHaveAttribute("data-editing-points", "0");
    await clickMap(bounds.width * .75, bounds.height * .8, 1);
    await page.keyboard.press("Control+z");
    await expect(map).toHaveAttribute("data-editing-points", "0");
    await page.keyboard.press("Control+z");
    await expect(map).toHaveAttribute("data-editing-points", "3");
    await page.keyboard.press("Control+y");
    await expect(map).toHaveAttribute("data-editing-points", "0");
    await page.keyboard.press("Control+z");

    const downloadPromise = page.waitForEvent("download");
    await page.keyboard.press("Control+s");
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe("Untitled document.gpx");
    const stream = await download.createReadStream();
    let gpx = "";
    for await (const chunk of stream) gpx += chunk.toString();
    expect(gpx.match(/<trkpt\b/g)?.length).toBeGreaterThan(3);

    await page.getByRole("button", { name: "Done" }).click();
    await expect(map).toHaveAttribute("data-editing", "false");
});

test("an existing segment supports direct insertion, live drag preview, and contextual deletion", async ({ page }) => {
    let routingRequests = 0;
    page.on("request", request => {
        if (request.url().startsWith("https://brouter.de/brouter?")) routingRequests++;
    });
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(minimalTrack);
    await expect(page.getByText(/Imported FX-GPX-001/)).toBeVisible();
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    const segment = explorer.locator(".document-explorer__label", { hasText: "Segment 1" });
    await segment.click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();
    const map = page.getByRole("application", { name: "Interactive route map" });
    await expect(map).toHaveAttribute("data-editing-points", "2");
    await expect(map.locator(".ol-viewport")).toBeVisible();
    await page.keyboard.press("Control+i");

    const bounds = await map.boundingBox();
    if (!bounds) throw new Error("Expected map bounds.");
    await expect.poll(async () => {
        const current = await editingPointPixels(map);
        return Math.hypot(current[1]![0]! - current[0]![0]!, current[1]![1]! - current[0]![1]!);
    }).toBeGreaterThan(50);
    let pixels = await editingPointPixels(map);
    const immediateMidpoint = widestSegmentMidpoint(pixels);
    await page.mouse.click(bounds.x + immediateMidpoint[0]!, bounds.y + immediateMidpoint[1]!);
    await expect(map).toHaveAttribute("data-editing-points", "3");
    await expect.poll(async () => (await editingPointPixels(map)).length).toBe(3);
    await page.keyboard.press("Control+z");
    await expect(map).toHaveAttribute("data-editing-points", "2");
    pixels = await editingPointPixels(map);
    await page.mouse.click(bounds.x + pixels[1]![0]!, bounds.y + pixels[1]![1]!, { button: "right" });
    const pointMenu = page.getByRole("menu", { name: "Editing anchor actions" });
    await expect(pointMenu).toBeVisible();
    await pointMenu.getByRole("menuitem", { name: "Delete anchor" }).click();
    await expect(map).toHaveAttribute("data-editing-points", "1");
    await page.keyboard.press("Control+z");
    await expect(map).toHaveAttribute("data-editing-points", "2");

    await page.mouse.click(bounds.x + bounds.width * .25, bounds.y + bounds.height * .75);
    await expect(map).toHaveAttribute("data-editing-points", "3");
    await page.keyboard.press("Control+z");
    await expect(map).toHaveAttribute("data-editing-points", "2");
    pixels = await editingPointPixels(map);
    const insertionMidpoint = widestSegmentMidpoint(pixels);
    await page.mouse.click(bounds.x + insertionMidpoint[0]!, bounds.y + insertionMidpoint[1]!);
    await expect(map).toHaveAttribute("data-editing-points", "3");

    pixels = await editingPointPixels(map);
    const movedAnchor = pixels[0]!;
    const requestsBeforeMove = routingRequests;
    await page.mouse.move(bounds.x + movedAnchor[0]!, bounds.y + movedAnchor[1]!);
    await page.mouse.down();
    await page.mouse.move(bounds.x + movedAnchor[0]! + 28, bounds.y + movedAnchor[1]! - 44, { steps: 5 });
    await expect(map).toHaveAttribute("data-editing-live", "true");
    await page.mouse.up();
    await expect(map).toHaveAttribute("data-editing-points", "3");
    await expect.poll(() => routingRequests - requestsBeforeMove).toBe(1);
    await page.evaluate(() => new Promise<void>(resolve =>
        requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));

    const downloadPromise = page.waitForEvent("download");
    await page.keyboard.press("Control+s");
    const stream = await (await downloadPromise).createReadStream();
    let gpx = "";
    for await (const chunk of stream) gpx += chunk.toString();
    expect(gpx.match(/<trkpt\b/g)?.length).toBeGreaterThan(3);
    expect(gpx).toContain("<ele>12.5</ele>");
    expect(gpx).toContain("2020-01-01T08:00:00Z");
});

test("an existing GPX route can enter the same editor", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(structuralGpx);
    await expect(page.getByText(/Imported FX-GPX-005/)).toBeVisible();
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    await explorer.locator(".document-explorer__label", { hasText: "Complete route" }).click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();

    await expect(page.getByRole("application", { name: "Interactive route map" }))
        .toHaveAttribute("data-editing-points", "1");
    await expect(page.locator(".manual-route-toolbar__status strong")).toHaveText("Complete route");
});

test("routing profile reroutes the route and remembers the latest bicycle mode", async ({ page }) => {
    const requestedProfiles: string[] = [];
    page.on("request", request => {
        if (request.url().startsWith("https://brouter.de/brouter?")) {
            requestedProfiles.push(new URL(request.url()).searchParams.get("profile") ?? "");
        }
    });
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(minimalTrack);
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    const segment = explorer.locator(".document-explorer__label", { hasText: "Segment 1" });
    await segment.click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();

    const profile = page.getByLabel("Routing profile");
    await expect(profile).toHaveValue("Cycling");
    await expect(profile.locator("option")).toHaveText(["Cycling", "Gravel", "MTB"]);
    await profile.selectOption("Gravel");
    await expect.poll(() => requestedProfiles.at(-1)).toBe("gravel");
    await expect(page.locator('.manual-route-toolbar[data-routing-state="success"]')).toBeVisible();

    await page.reload();
    await segment.click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();
    await expect(page.getByLabel("Routing profile")).toHaveValue("Gravel");
    await page.getByLabel("Routing profile").selectOption("MountainBike");
    await expect.poll(() => requestedProfiles.at(-1)).toBe("mtb");
});

test("routing failure keeps the last valid geometry and reports the failure", async ({ page }) => {
    test.setTimeout(60_000);
    await page.unroute("https://brouter.de/brouter?**");
    await page.route("https://brouter.de/brouter?**", async route => {
        await new Promise(resolve => setTimeout(resolve, 500));
        await route.fulfill({ status: 503, contentType: "text/plain", body: "maintenance" });
    });
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(minimalTrack);
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    await explorer.locator(".document-explorer__label", { hasText: "Segment 1" }).click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();
    const map = page.getByRole("application", { name: "Interactive route map" });
    await expect(map).toHaveAttribute("data-editing-points", "2");
    const before = await storedWorkspacePayload(page);
    const bounds = await map.boundingBox();
    if (!bounds) throw new Error("Expected map bounds.");
    await expect.poll(async () => (await editingPointPixels(map)).length).toBe(2);
    const pixels = await editingPointPixels(map);

    const midpoint = widestSegmentMidpoint(pixels);
    await page.mouse.click(bounds.x + midpoint[0]!, bounds.y + midpoint[1]!);

    await expect(page.locator('.manual-route-toolbar[data-routing-state="routing"]')).toBeVisible();
    await expect(page.locator('.manual-route-toolbar[data-routing-state="failure"]')).toBeVisible();
    await expect(page.getByText(/BRouter returned 503/)).toBeVisible();
    await expect.poll(() => storedWorkspacePayload(page)).toBe(before);
});

test("no-route response is distinct from provider failure", async ({ page }) => {
    test.setTimeout(60_000);
    await page.unroute("https://brouter.de/brouter?**");
    await page.route("https://brouter.de/brouter?**", route => route.fulfill({
        status: 400,
        contentType: "text/plain",
        body: "no track found",
    }));
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(minimalTrack);
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    await explorer.locator(".document-explorer__label", { hasText: "Segment 1" }).click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();
    const map = page.getByRole("application", { name: "Interactive route map" });
    const bounds = await map.boundingBox();
    if (!bounds) throw new Error("Expected map bounds.");
    await expect.poll(async () => (await editingPointPixels(map)).length).toBe(2);
    const midpoint = widestSegmentMidpoint(await editingPointPixels(map));

    await page.mouse.click(bounds.x + midpoint[0]!, bounds.y + midpoint[1]!);

    await expect(page.locator('.manual-route-toolbar[data-routing-state="noroute"]')).toBeVisible();
    await expect(page.getByText("no track found", { exact: true })).toBeVisible();
    await expect(map).toHaveAttribute("data-editing-points", "2");
});

test("escape closes unchanged editing and offers keep or discard after changes", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(minimalTrack);
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    const segment = explorer.locator(".document-explorer__label", { hasText: "Segment 1" });
    const beginEditing = async () => {
        await segment.click({ button: "right" });
        await page.getByRole("menuitem", { name: /Edit/ }).click();
    };
    const map = page.getByRole("application", { name: "Interactive route map" });

    await beginEditing();
    await page.keyboard.press("Escape");
    await expect(map).toHaveAttribute("data-editing", "false");
    await expect(page.getByRole("dialog", { name: "Keep route changes?" })).toHaveCount(0);

    await beginEditing();
    const before = await storedWorkspacePayload(page);
    await page.getByRole("button", { name: "Reverse" }).click();
    await page.keyboard.press("Escape");
    const dialog = page.getByRole("dialog", { name: "Keep route changes?" });
    await expect(dialog).toBeVisible();
    await dialog.getByRole("button", { name: "Discard changes" }).click();
    await expect(map).toHaveAttribute("data-editing", "false");
    await expect.poll(() => storedWorkspacePayload(page)).toBe(before);

    await beginEditing();
    await page.getByRole("button", { name: "Reverse" }).click();
    await page.keyboard.press("Escape");
    await page.getByRole("dialog", { name: "Keep route changes?" })
        .getByRole("button", { name: "Keep changes" }).click();
    await expect(map).toHaveAttribute("data-editing", "false");
    await expect.poll(() => storedWorkspacePayload(page)).not.toBe(before);
});
