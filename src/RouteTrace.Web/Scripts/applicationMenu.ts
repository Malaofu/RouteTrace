type MenuReference = {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
};

let reference: MenuReference | null = null;
let fileInput: HTMLInputElement | null = null;
let fileDragDepth = 0;
let editingActive = false;

function handleKeyDown(event: KeyboardEvent): void {
    if (editingActive && event.key === "Escape") {
        event.preventDefault();
        void reference?.invokeMethodAsync("RunShortcutAsync", "editClose");
        return;
    }

    if (!(event.ctrlKey || event.metaKey) || event.altKey) return;

    if (editingActive && event.key.toLowerCase() === "z") {
        event.preventDefault();
        void reference?.invokeMethodAsync("RunShortcutAsync", event.shiftKey ? "editRedo" : "editUndo");
        return;
    }
    if (editingActive && event.key.toLowerCase() === "y") {
        event.preventDefault();
        void reference?.invokeMethodAsync("RunShortcutAsync", "editRedo");
        return;
    }

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

function isFileDrag(event: DragEvent): boolean {
    return event.dataTransfer?.types.includes("Files") === true;
}

function showDropTarget(visible: boolean): void {
    void reference?.invokeMethodAsync("SetDropTargetVisible", visible);
}

function handleDragEnter(event: DragEvent): void {
    if (!isFileDrag(event)) return;

    event.preventDefault();
    fileDragDepth++;
    if (fileDragDepth === 1) showDropTarget(true);
}

function handleDragOver(event: DragEvent): void {
    if (!isFileDrag(event)) return;

    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = "copy";
}

function handleDragLeave(event: DragEvent): void {
    if (!isFileDrag(event)) return;

    fileDragDepth = Math.max(0, fileDragDepth - 1);
    if (fileDragDepth === 0) showDropTarget(false);
}

function handleDrop(event: DragEvent): void {
    if (!isFileDrag(event)) return;

    event.preventDefault();
    fileDragDepth = 0;
    showDropTarget(false);

    const file = event.dataTransfer?.files[0];
    if (!file || !fileInput) return;

    const transfer = new DataTransfer();
    transfer.items.add(file);
    fileInput.files = transfer.files;
    fileInput.dispatchEvent(new Event("change", { bubbles: true }));
}

export function attachApplicationMenu(
    dotNetReference: MenuReference,
    input: HTMLInputElement,
): void {
    reference = dotNetReference;
    fileInput = input;
    document.addEventListener("keydown", handleKeyDown);
    document.addEventListener("click", handleDocumentClick);
    document.addEventListener("dragenter", handleDragEnter);
    document.addEventListener("dragover", handleDragOver);
    document.addEventListener("dragleave", handleDragLeave);
    document.addEventListener("drop", handleDrop);
}

export function detachApplicationMenu(): void {
    document.removeEventListener("keydown", handleKeyDown);
    document.removeEventListener("click", handleDocumentClick);
    document.removeEventListener("dragenter", handleDragEnter);
    document.removeEventListener("dragover", handleDragOver);
    document.removeEventListener("dragleave", handleDragLeave);
    document.removeEventListener("drop", handleDrop);
    reference = null;
    fileInput = null;
    fileDragDepth = 0;
    editingActive = false;
}

export function openFilePicker(input: HTMLInputElement): void {
    input.click();
}

export function setEditingActive(active: boolean): void {
    editingActive = active;
}
