import { mkdir, readdir, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import * as sass from "sass";

const projectRoot = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    "..");
const scopedOutputRoot = path.join(
    projectRoot,
    ".generated",
    "scopedcss-input");
const ignoredDirectories = new Set([
    ".git",
    ".generated",
    "bin",
    "node_modules",
    "obj",
    "wwwroot"
]);

async function findComponentStyles(directory) {
    const entries = await readdir(directory, { withFileTypes: true });
    const files = [];

    for (const entry of entries) {
        const entryPath = path.join(directory, entry.name);

        if (entry.isDirectory()) {
            if (!ignoredDirectories.has(entry.name)) {
                files.push(...await findComponentStyles(entryPath));
            }

            continue;
        }

        if (entry.name.endsWith(".razor.scss")) {
            files.push(entryPath);
        }
    }

    return files;
}

async function compile(inputPath, outputPath) {
    const result = await sass.compileAsync(inputPath, {
        style: "expanded"
    });

    await mkdir(path.dirname(outputPath), { recursive: true });
    await writeFile(outputPath, result.css);
}

await rm(scopedOutputRoot, { recursive: true, force: true });

await compile(
    path.join(projectRoot, "Styles", "app.scss"),
    path.join(projectRoot, "wwwroot", "generated", "app.css"));

for (const inputPath of await findComponentStyles(projectRoot)) {
    const relativePath = path.relative(projectRoot, inputPath);
    const outputPath = path.join(
        scopedOutputRoot,
        `${relativePath.slice(0, -".scss".length)}.css`);

    await compile(inputPath, outputPath);
}
