import { expect, test } from "@playwright/test";
import path from "node:path";

const fixturePath = path.resolve(
    "../../tests/RouteTrace.TestData/FX-GPX-001-minimal-track.gpx");

test("file menu exposes availability and keyboard dismissal", async ({ page }) => {
    await page.goto("/");

    const fileButton = page.getByRole("button", { name: "File", exact: true });
    await fileButton.click();
    await expect(page.getByRole("menu")).toBeVisible();
    await expect(page.getByRole("menuitem", { name: /Download GPX/ })).toBeDisabled();

    await page.keyboard.press("Escape");
    await expect(page.getByRole("menu")).toBeHidden();
    await expect(fileButton).toBeFocused();

    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    await expect(page.getByText(/Imported FX-GPX-001/)).toBeVisible();

    const menuBox = await page.locator(".application-menu").boundingBox();
    const inspectorBox = await page.getByRole("complementary", { name: "GPX inspector" }).boundingBox();
    expect(menuBox).not.toBeNull();
    expect(inspectorBox).not.toBeNull();
    expect(Math.abs(menuBox!.x + menuBox!.width / 2 - page.viewportSize()!.width / 2)).toBeLessThan(2);
    expect(menuBox!.x).toBeGreaterThanOrEqual(inspectorBox!.x + inspectorBox!.width);

    const logoBox = await page.getByRole("link", { name: "Route Trace home" }).boundingBox();
    expect(logoBox).not.toBeNull();
    expect(inspectorBox!.y).toBeGreaterThanOrEqual(logoBox!.y + logoBox!.height);

    await fileButton.click();
    await expect(page.getByRole("menuitem", { name: /Download GPX/ })).toBeEnabled();
});

test("View menu toggles the inspector through menu and keyboard", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    const inspector = page.getByRole("complementary", { name: "GPX inspector" });
    const explorer = page.getByLabel("Document explorer");
    await expect(inspector).toBeVisible();
    await expect(explorer).toBeVisible();
    const map = page.getByRole("application", { name: "Interactive route map" });
    const mapWidthWithExplorer = (await map.boundingBox())!.width;

    await page.getByRole("button", { name: "View", exact: true }).click();
    const inspectorToggle = page.getByRole("menuitemcheckbox", { name: /Inspector/ });
    await expect(inspectorToggle).toHaveAttribute("aria-checked", "true");
    await inspectorToggle.click();
    await expect(inspector).toBeHidden();

    await page.keyboard.press("Control+i");
    await expect(inspector).toBeVisible();

    await page.keyboard.press("Control+e");
    await expect(explorer).toBeHidden();
    await expect.poll(async () => (await map.boundingBox())!.width).toBeGreaterThan(mapWidthWithExplorer + 300);
    await page.keyboard.press("Control+e");
    await expect(explorer).toBeVisible();
});

test("successful import notice dismisses automatically", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);

    const notice = page.getByText(/Imported FX-GPX-001/);
    await expect(notice).toBeVisible();
    await expect(notice).toBeHidden({ timeout: 7_000 });
});

test("file menu dismisses without affecting the map", async ({ page }) => {
    await page.goto("/");
    await page.getByRole("button", { name: "File", exact: true }).click();
    await page.getByRole("main").click({ position: { x: 500, y: 500 } });
    await expect(page.getByRole("menu")).toBeHidden();
    await expect(page.getByRole("application", { name: "Interactive route map" })).toBeVisible();
});
