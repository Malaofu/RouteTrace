import { expect, test } from "@playwright/test";
import path from "node:path";

const fixturePath = path.resolve("../../tests/RouteTrace.TestData/FX-GPX-004-gpx-studio-supplemented.gpx");

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

test("context actions follow selection, pointer position, and outside dismissal", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);

    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    await expect(explorer.getByRole("button", { name: "More actions" })).toHaveCount(0);
    const labels = explorer.locator(".document-explorer__label");
    const firstChild = labels.nth(1);
    const secondChild = labels.nth(2);
    await firstChild.click();
    await secondChild.click({ modifiers: ["Control"] });
    await expect(explorer.locator(".document-explorer__row--active")).toHaveCount(2);

    await firstChild.click({ button: "right", position: { x: 12, y: 10 } });
    await expect(explorer.locator(".document-explorer__row--active")).toHaveCount(2);
    let menu = page.getByRole("menu", { name: /Actions for/ });
    await expect(menu.getByRole("menuitem", { name: /Appearance/ })).toBeVisible();
    await expect(menu.getByRole("menuitem", { name: /New segment/ })).toBeEnabled();
    await expect(menu.getByRole("menuitem", { name: /Duplicate/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Copy/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Cut/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Paste/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Center/ })).toBeVisible();
    await expect(menu.getByRole("menuitem", { name: /Select all/ })).toHaveCount(0);
    const menuBox = await menu.boundingBox();
    const targetBox = await firstChild.boundingBox();
    if (!menuBox || !targetBox) throw new Error("Expected visible menu and target bounds.");
    expect(menuBox.x).toBeGreaterThanOrEqual(targetBox.x);
    expect(menuBox.y).toBeGreaterThanOrEqual(targetBox.y);

    await page.locator(".document-explorer__menu-dismiss").click({ position: { x: 2, y: 2 } });
    await expect(menu).toBeHidden();

    await labels.first().click({ button: "right" });
    await expect(explorer.locator(".document-explorer__document").first().locator(":scope > .document-explorer__row--active")).toHaveCount(1);
    menu = page.getByRole("menu", { name: /Actions for/ });
    await menu.getByRole("menuitem", { name: /Info/ }).click();
    const dialog = page.getByRole("dialog", { name: "Info" });
    await dialog.getByLabel("Name").fill("Renamed GPX");
    await dialog.getByLabel("Description").fill("Updated locally");
    await dialog.getByRole("button", { name: "Save" }).click();
    await expect(explorer.getByRole("button", { name: "Renamed GPX", exact: true })).toBeVisible();
    await expect(dialog).toBeHidden();
    expect(await storedWorkspacePayload(page)).toContain("Renamed GPX");
    await page.reload();
    await expect(page.getByRole("complementary", { name: "Document explorer" }).getByRole("button", { name: "Renamed GPX", exact: true })).toBeVisible();
});

test("explorer context menus create and delete the document route segment hierarchy", async ({ page }) => {
    await page.goto("/");
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    await explorer.locator(".document-explorer__label").first().click({ button: "right" });
    await page.getByRole("menuitem", { name: "Delete" }).click();
    const explorerBox = await explorer.boundingBox();
    if (!explorerBox) throw new Error("Expected explorer bounds.");
    const clickPosition = { x: 24, y: explorerBox.height - 40 };
    const backgroundClick = {
        x: explorerBox.x + clickPosition.x,
        y: explorerBox.y + clickPosition.y,
    };
    await explorer.click({ button: "right", position: clickPosition });
    const backgroundMenu = page.getByRole("menu", { name: "Actions for document explorer" });
    await expect(backgroundMenu).toBeVisible();
    const backgroundMenuBox = await backgroundMenu.boundingBox();
    if (!backgroundMenuBox) throw new Error("Expected background menu bounds.");
    expect(backgroundMenuBox.y).toBeGreaterThan(backgroundClick.y - 64);
    expect(backgroundMenuBox.y + backgroundMenuBox.height).toBeLessThanOrEqual(explorerBox.y + explorerBox.height + 1);
    await backgroundMenu.getByRole("menuitem", { name: "New document" }).click();
    const document = explorer.locator(".document-explorer__label", { hasText: "Untitled document" });
    await expect(document).toBeVisible();

    await document.click({ button: "right" });
    await page.getByRole("menuitem", { name: "New route" }).click();
    const route = explorer.locator(".document-explorer__label", { hasText: "Route 1" });
    await expect(route).toBeVisible();

    await route.click({ button: "right" });
    await page.getByRole("menuitem", { name: "New segment" }).click();
    const segment = explorer.locator(".document-explorer__label", { hasText: "Segment 1" });
    await expect(segment).toBeVisible();

    await segment.click({ button: "right" });
    await expect(page.getByRole("menuitem", { name: /Edit/ })).toBeVisible();
    await page.getByRole("menuitem", { name: "Delete" }).click();
    await expect(segment).toHaveCount(0);

    await route.click({ button: "right" });
    await page.getByRole("menuitem", { name: "Delete" }).click();
    await expect(route).toHaveCount(0);
});

test("effective colour strips update and child colours can reset to their parent", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    const labels = explorer.locator(".document-explorer__label");
    const trackRow = labels.nth(1).locator("..");
    const segmentLabel = labels.nth(2);
    const segmentRow = segmentLabel.locator("..");
    const inheritedColour = await trackRow.evaluate(element => (element as HTMLElement).style.getPropertyValue("--document-colour"));

    await segmentLabel.click({ button: "right" });
    await page.getByRole("menuitem", { name: /Appearance/ }).click();
    await page.getByRole("dialog", { name: "Appearance" }).getByLabel("Colour").fill("#ff0000");
    await page.getByRole("button", { name: "Apply" }).click();
    await expect(segmentRow).toHaveAttribute("style", /#ff0000/);

    await segmentLabel.click({ button: "right" });
    await page.getByRole("menuitem", { name: /Appearance/ }).click();
    await page.getByRole("button", { name: "Reset to parent" }).click();
    await expect(segmentRow).toHaveAttribute("style", new RegExp(inheritedColour));
});

test("context menu remains keyboard accessible without an overflow button", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    const documentLabel = page.getByRole("complementary", { name: "Document explorer" }).locator(".document-explorer__label").first();
    await documentLabel.focus();
    await page.keyboard.press("Shift+F10");
    await expect(page.getByRole("menu", { name: /Actions for/ })).toBeVisible();
});
