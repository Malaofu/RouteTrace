import { expect, test } from "@playwright/test";
import path from "node:path";

const fixturePath = path.resolve("../../tests/RouteTrace.TestData/FX-GPX-004-gpx-studio-supplemented.gpx");

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
    await expect(menu.getByRole("menuitem", { name: /New segment/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Duplicate/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Copy/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Cut/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Paste/ })).toBeDisabled();
    await expect(menu.getByRole("menuitem", { name: /Center/ })).toBeVisible();
    const menuBox = await menu.boundingBox();
    const targetBox = await firstChild.boundingBox();
    expect(menuBox!.x).toBeGreaterThanOrEqual(targetBox!.x);
    expect(menuBox!.y).toBeGreaterThanOrEqual(targetBox!.y);

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
});

test("context menu remains keyboard accessible without an overflow button", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    const documentLabel = page.getByRole("complementary", { name: "Document explorer" }).locator(".document-explorer__label").first();
    await documentLabel.focus();
    await page.keyboard.press("Shift+F10");
    await expect(page.getByRole("menu", { name: /Actions for/ })).toBeVisible();
});
