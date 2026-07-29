type ThemePreference = "light" | "dark" | "auto";

const storageKey = "route-trace-theme";
const systemTheme = matchMedia("(prefers-color-scheme: dark)");
let listening = false;

function isThemePreference(value: string | null): value is ThemePreference {
    return value === "light" || value === "dark" || value === "auto";
}

function storedPreference(): ThemePreference {
    const value = localStorage.getItem(storageKey);
    return isThemePreference(value) ? value : "auto";
}

function apply(preference: ThemePreference): void {
    const effectiveTheme =
        preference === "auto"
            ? systemTheme.matches ? "dark" : "light"
            : preference;

    document.documentElement.dataset.themePreference = preference;
    document.documentElement.dataset.theme = effectiveTheme;
}

function systemThemeChanged(): void {
    const preference = storedPreference();
    if (preference === "auto") {
        apply(preference);
    }
}

export function initialize(): ThemePreference {
    const preference = storedPreference();
    apply(preference);

    if (!listening) {
        systemTheme.addEventListener("change", systemThemeChanged);
        listening = true;
    }

    return preference;
}

export function setPreference(preference: ThemePreference): void {
    localStorage.setItem(storageKey, preference);
    apply(preference);
}

export function dispose(): void {
    if (listening) {
        systemTheme.removeEventListener("change", systemThemeChanged);
        listening = false;
    }
}
