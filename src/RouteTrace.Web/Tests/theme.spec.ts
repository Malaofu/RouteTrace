import { expect, test } from "@playwright/test";
import type { Locator, Page } from "@playwright/test";
import path from "node:path";

const fixturePath = path.resolve(
    "../../tests/RouteTrace.TestData/FX-GPX-001-minimal-track.gpx");

interface ComputedThemeStyles {
    background: string;
    border: string;
    color: string;
}

interface ChromeThemeStyles {
    applicationMenu: ComputedThemeStyles;
    documentExplorer: ComputedThemeStyles;
    contextMenu: ComputedThemeStyles;
    infoDialog: ComputedThemeStyles;
    infoInput: ComputedThemeStyles;
    editExitDialog: ComputedThemeStyles;
}

async function computedThemeStyles(locator: Locator): Promise<ComputedThemeStyles> {
    return locator.evaluate(element => {
        const styles = getComputedStyle(element);
        return {
            background: styles.backgroundColor,
            border: styles.borderColor,
            color: styles.color,
        };
    });
}

async function chromeThemeStyles(page: Page): Promise<ChromeThemeStyles> {
    const applicationMenu = page.locator(".application-menu");
    await page.getByRole("button", { name: "File", exact: true }).click();
    const fileMenu = page.getByRole("menu", { name: "File" });
    await expect(fileMenu).toBeVisible();
    const menuStyles = await computedThemeStyles(applicationMenu);
    await page.keyboard.press("Escape");

    const explorer = page.getByRole("complementary", { name: "Document explorer" });
    const explorerStyles = await computedThemeStyles(explorer);
    await explorer.locator(".document-explorer__label").first().click({ button: "right" });
    const contextMenu = page.getByRole("menu", { name: /Actions for/ });
    const contextStyles = await computedThemeStyles(contextMenu);
    await contextMenu.getByRole("menuitem", { name: /Info/ }).click();
    const infoDialog = page.getByRole("dialog", { name: "Info" });
    const dialogStyles = await computedThemeStyles(infoDialog);
    const inputStyles = await computedThemeStyles(infoDialog.getByLabel("Name"));
    await infoDialog.getByRole("button", { name: "Cancel" }).click();

    const segment = explorer.locator(".document-explorer__label", { hasText: "Segment 1" });
    await segment.click({ button: "right" });
    await page.getByRole("menuitem", { name: /Edit/ }).click();
    await page.getByRole("button", { name: "Reverse" }).click();
    await page.keyboard.press("Escape");
    const editExitDialog = page.getByRole("dialog", { name: "Keep route changes?" });
    const editExitStyles = await computedThemeStyles(editExitDialog);
    await editExitDialog.getByRole("button", { name: "Discard changes" }).click();

    return {
        applicationMenu: menuStyles,
        documentExplorer: explorerStyles,
        contextMenu: contextStyles,
        infoDialog: dialogStyles,
        infoInput: inputStyles,
        editExitDialog: editExitStyles,
    };
}

test("theme selection persists and auto follows the system preference", async ({ page }) => {
    await page.emulateMedia({ colorScheme: "light" });
    await page.goto("/");

    const root = page.locator("html");
    await expect(root).toHaveAttribute("data-theme-preference", "auto");
    await expect(root).toHaveAttribute("data-theme", "light");

    await page.getByRole("button", { name: "Dark theme" }).click();
    await expect(page.getByRole("button", { name: "Dark theme" })).toHaveAttribute("aria-pressed", "true");
    await expect(root).toHaveAttribute("data-theme-preference", "dark");
    await expect(root).toHaveAttribute("data-theme", "dark");

    await page.reload();
    await expect(root).toHaveAttribute("data-theme-preference", "dark");
    await expect(root).toHaveAttribute("data-theme", "dark");

    await page.getByRole("button", { name: "Auto theme" }).click();
    await expect(root).toHaveAttribute("data-theme-preference", "auto");
    await expect(root).toHaveAttribute("data-theme", "light");

    await page.emulateMedia({ colorScheme: "dark" });
    await expect(root).toHaveAttribute("data-theme", "dark");
});

test("application chrome and dialogs follow the selected theme", async ({ page }) => {
    await page.emulateMedia({ colorScheme: "light" });
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    await expect(page.getByText(/Imported FX-GPX-001/)).toBeVisible();

    const light = await chromeThemeStyles(page);
    await page.getByRole("button", { name: "Dark theme" }).click();
    const dark = await chromeThemeStyles(page);

    for (const area of Object.keys(light) as (keyof ChromeThemeStyles)[]) {
        expect(dark[area].background, `${area} background`).not.toBe(light[area].background);
        expect(dark[area].color, `${area} text`).not.toBe(light[area].color);
        expect(dark[area].border, `${area} border`).not.toBe(light[area].border);
    }
});
