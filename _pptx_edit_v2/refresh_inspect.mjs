import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = "D:/05_codex/02_game/002_統合スライド/YTC_Game_Project_Definition_v1.pptx";
const out = "D:/05_codex/02_game/_pptx_edit_v2/template-inspect/template-inspect.ndjson";
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
const inspect = await presentation.inspect({
  kind: "slide,textbox,shape,image,table,chart",
  maxChars: 250000,
});
await fs.writeFile(out, inspect.ndjson || "", "utf8");
