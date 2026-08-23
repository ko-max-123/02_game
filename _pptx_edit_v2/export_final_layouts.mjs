import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = process.argv[2] ?? "D:/05_codex/02_game/002_統合スライド/YTC_Game_Project_Definition_v2_校正再提出版.pptx";
const outputDir = process.argv[3] ?? "D:/05_codex/02_game/_pptx_edit_v2/final-layout";

await fs.mkdir(outputDir, { recursive: true });
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
for (let index = 0; index < presentation.slides.items.length; index += 1) {
  const slide = presentation.slides.items[index];
  const padded = String(index + 1).padStart(2, "0");
  const layout = await presentation.export({ slide, format: "layout" });
  await fs.writeFile(path.join(outputDir, `final-slide-${padded}.layout.json`), await layout.text(), "utf8");
}

console.log(`Exported ${presentation.slides.items.length} final layouts to ${outputDir}`);
