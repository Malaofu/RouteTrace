type MenuReference = {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
};

let reference: MenuReference | null = null;

function handleKeyDown(event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey) || event.altKey) return;

    const command = event.key.toLowerCase() === "o"
        ? "open"
        : event.key.toLowerCase() === "s"
            ? "export"
            : event.key.toLowerCase() === "i"
                ? "inspector"
                : event.key.toLowerCase() === "e" ? "explorer" : null;
    if (command === null) return;

    event.preventDefault();
    void reference?.invokeMethodAsync("RunShortcutAsync", command);
}

function handleDocumentClick(event: MouseEvent): void {
    const target = event.target as Element | null;
    if (target?.closest(".application-menu") === null) {
        void reference?.invokeMethodAsync("DismissMenu");
    }
}

export function attachApplicationMenu(dotNetReference: MenuReference): void {
    reference = dotNetReference;
    document.addEventListener("keydown", handleKeyDown);
    document.addEventListener("click", handleDocumentClick);
}

export function detachApplicationMenu(): void {
    document.removeEventListener("keydown", handleKeyDown);
    document.removeEventListener("click", handleDocumentClick);
    reference = null;
}

export function openFilePicker(input: HTMLInputElement): void {
    input.click();
}
