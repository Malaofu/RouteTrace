import { defineConfig } from "@playwright/test";

export default defineConfig({
    testDir: "Tests",
    timeout: 30_000,
    use: {
        baseURL: "http://127.0.0.1:5187",
        browserName: "chromium",
        channel: process.platform === "win32" ? "msedge" : undefined,
        headless: true,
    },
    webServer: {
        command: `dotnet run --no-build --configuration ${process.env.CI ? "Release" : "Debug"} --urls http://127.0.0.1:5187`,
        url: "http://127.0.0.1:5187",
        reuseExistingServer: false,
        timeout: 30_000,
    },
});
