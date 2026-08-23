import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = "D:/05_codex/02_game/002_統合スライド/YTC_Game_Project_Definition_v2_校正再提出版.pptx";
const output = "D:/05_codex/02_game/002_統合スライド/YTC_Game_Project_Definition_v2_校正済最終版.pptx";
const inspectOutput = `${output}.inspect.ndjson`;

const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
const inspected = await presentation.inspect({ kind: "textbox", maxChars: 350000 });
const records = inspected.ndjson.split(/\r?\n/).filter(Boolean).map((line) => JSON.parse(line));

function replaceFragment(slideNumber, before, after) {
  const hit = records.find((record) => record.slide === slideNumber && record.kind === "textbox" && String(record.text ?? "").includes(before));
  if (!hit) throw new Error(`Missing label on slide ${slideNumber}: ${before}`);
  const shape = presentation.resolve(hit.id);
  shape.text.replace(before, after);
}

replaceFragment(1, "Producer Revision v2 / 校正再提出版", "Producer Final v2 / 校正済最終版");
replaceFragment(55, "PRODUCER REVISION FOR PROOFREADING", "PRODUCER FINAL / 校正済最終版");

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(output);

const finalInspect = await presentation.inspect({ kind: "slide,textbox,table,notes", maxChars: 350000 });
await fs.writeFile(inspectOutput, finalInspect.ndjson || "", "utf8");

console.log(`Saved proofread final deck to ${output}`);
