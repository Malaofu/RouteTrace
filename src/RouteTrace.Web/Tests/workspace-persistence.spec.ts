import { expect, test } from "@playwright/test";
import path from "node:path";

const fixtures = path.resolve(process.cwd(), "../../tests/RouteTrace.TestData");

test("automatically restores a multi-document workspace after reload", async ({ page }) => {
    await page.goto("/");

    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles(path.join(fixtures, "FX-GPX-001-minimal-track.gpx"));
    await expect(page.getByText(/Imported FX-GPX-001/)).toBeVisible();
    await fileInput.setInputFiles(path.join(fixtures, "FX-GPX-003-multiple-tracks-segments.gpx"));
    await expect(page.getByText(/Imported FX-GPX-003/)).toBeVisible();

    await page.getByLabel("Name").fill("Persistence test");
    await page.getByLabel("Name").press("Tab");
    await expect(page.getByText("Renamed to Persistence test.")).toBeVisible();

    await page.reload();
    await expect(page.getByLabel("Name")).toHaveValue("Persistence test");
    await expect(page.getByLabel("Document explorer").locator(".document-explorer__document")).toHaveCount(2);
    const savedWorkspace = page.getByRole("listitem").filter({ hasText: "Persistence test" });
    await expect(savedWorkspace).toBeVisible();

    page.once("dialog", dialog => dialog.accept());
    await savedWorkspace.getByRole("button", { name: "Delete" }).click();
    await expect(page.getByText("Deleted Persistence test.")).toBeVisible();
    await expect(page.getByText("No saved workspaces.")).toBeVisible();
});
