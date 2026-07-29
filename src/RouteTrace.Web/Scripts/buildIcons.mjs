import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { Resvg } from "@resvg/resvg-js";

const projectRoot = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    "..");
const sourcePath = path.join(
    projectRoot,
    "wwwroot",
    "images",
    "route-trace-icon.svg");
const outputDirectory = path.join(
    projectRoot,
    "wwwroot",
    "generated");
const source = await readFile(sourcePath);

await mkdir(outputDirectory, { recursive: true });

for (const size of [192, 512]) {
    const renderer = new Resvg(source, {
        fitTo: {
            mode: "width",
            value: size
        }
    });
    const outputPath = path.join(
        outputDirectory,
        `route-trace-icon-${size}.png`);

    await writeFile(outputPath, renderer.render().asPng());
}
