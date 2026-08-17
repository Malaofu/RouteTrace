import { expect, test } from "@playwright/test";
import type { JSHandle, Page } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

const fixturePath = path.resolve(
    "../../tests/RouteTrace.TestData/FX-GPX-001-minimal-track.gpx");

async function droppedFile(
    page: Page,
    name: string,
    contents: string,
): Promise<JSHandle<DataTransfer>> {
    return page.evaluateHandle(({ fileName, fileContents }) => {
        const transfer = new DataTransfer();
        transfer.items.add(new File(
            [fileContents],
            fileName,
            { type: "application/gpx+xml" },
        ));
        return transfer;
    }, { fileName: name, fileContents: contents });
}

async function drop(page: Page, transfer: JSHandle<DataTransfer>): Promise<void> {
    await page.dispatchEvent("body", "dragenter", { dataTransfer: transfer });
    await expect(page.getByText("Drop GPX to import")).toBeVisible();
    await page.dispatchEvent("body", "dragover", { dataTransfer: transfer });
    await page.dispatchEvent("body", "drop", { dataTransfer: transfer });
    await expect(page.getByText("Drop GPX to import")).toBeHidden();
}

test("picker and drop imports use the same workflow and feedback", async ({ page }) => {
    const contents = fs.readFileSync(fixturePath, "utf8");
    await page.goto("/");

    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    const feedback = `Imported ${path.basename(fixturePath)}: 3 point(s).`;
    await expect(page.getByText(feedback)).toBeVisible();

    const transfer = await droppedFile(page, path.basename(fixturePath), contents);
    await drop(page, transfer);

    await expect(page.getByText(feedback)).toBeVisible();
    await expect(page.getByLabel("Document explorer").locator(".document-explorer__document")).toHaveCount(2);
});

test("an invalid dropped file preserves the valid workspace", async ({ page }) => {
    await page.goto("/");
    await page.locator('input[type="file"]').setInputFiles(fixturePath);
    await expect(page.getByText(/Imported FX-GPX-001/)).toBeVisible();

    const transfer = await droppedFile(page, "invalid.gpx", "not GPX");
    await drop(page, transfer);

    await expect(page.getByRole("alert")).toBeVisible();
    await expect(page.getByLabel("Document explorer").locator(".document-explorer__document")).toHaveCount(1);
    await expect(page.getByRole("application", { name: "Interactive route map" }))
        .toHaveAttribute("data-visible-documents", "1");
});
