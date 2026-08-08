import { defineConfig } from "@playwright/test";

const publishedRoot = process.env.ROUTETRACE_PUBLISHED_ROOT;

export default defineConfig({
    testDir: "Tests",
    timeout: 30_000,
    workers: 1,
    use: {
        baseURL: "http://127.0.0.1:5187",
        browserName: "chromium",
        channel: process.platform === "win32" ? "msedge" : undefined,
        headless: true,
    },
    webServer: {
        command: publishedRoot
            ? `python -m http.server 5187 --bind 127.0.0.1 --directory "${publishedRoot}"`
            : `dotnet run --no-build --configuration ${process.env.CI ? "Release" : "Debug"} --urls http://127.0.0.1:5187`,
        url: "http://127.0.0.1:5187",
        reuseExistingServer: false,
        timeout: 30_000,
    },
});
