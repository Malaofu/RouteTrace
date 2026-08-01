export async function downloadStream(
    fileName: string,
    contentType: string,
    streamReference: { arrayBuffer(): Promise<ArrayBuffer> },
): Promise<void> {
    const data = await streamReference.arrayBuffer();
    const url = URL.createObjectURL(new Blob([data], { type: contentType }));
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
}
