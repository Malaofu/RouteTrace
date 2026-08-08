type ThemePreference = "light" | "dark" | "auto";

const storageKey = "route-trace-theme";
const storedValue = localStorage.getItem(storageKey);
const preference: ThemePreference =
    storedValue === "light" || storedValue === "dark" || storedValue === "auto"
        ? storedValue
        : "auto";
const followsDarkTheme = matchMedia("(prefers-color-scheme: dark)").matches;
const effectiveTheme =
    preference === "auto"
        ? followsDarkTheme ? "dark" : "light"
        : preference;

document.documentElement.dataset.themePreference = preference;
document.documentElement.dataset.theme = effectiveTheme;

interface RouteTraceBrowserHelpers {
    waitForAnimationFrame(): Promise<void>;
}

interface Window {
    routeTrace: RouteTraceBrowserHelpers;
}

window.routeTrace = {
    waitForAnimationFrame: () => new Promise(resolve => requestAnimationFrame(() => resolve())),
};
