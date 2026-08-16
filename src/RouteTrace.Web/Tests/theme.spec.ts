import { expect, test } from "@playwright/test";

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
