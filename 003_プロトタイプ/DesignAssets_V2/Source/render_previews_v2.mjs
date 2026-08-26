import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
const require = createRequire(import.meta.url);
const sharp = require("C:/Users/minim/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp");

const sourceDir = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(sourceDir, "..");
const previewDir = path.join(rootDir, "Previews");
const oldPreviewDir = path.resolve(rootDir, "..", "DesignAssets", "Previews");

for (const name of ["yamada_k1_v2_preview", "central_industrial_belt_v2_preview", "normal_gameplay_distance_v2"]) {
  await sharp(path.join(previewDir, `${name}.svg`), { density: 144 }).png().toFile(path.join(previewDir, `${name}.png`));
}

function titleCard(title, subtitle, width, accent = "#F28C28") {
  return Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="110">
    <rect width="100%" height="100%" fill="#111920"/>
    <rect x="28" y="22" width="7" height="66" fill="${accent}"/>
    <text x="52" y="54" fill="#F3F0E8" font-family="Arial,sans-serif" font-size="27" font-weight="700">${title}</text>
    <text x="52" y="82" fill="#B8C2C7" font-family="Arial,sans-serif" font-size="16">${subtitle}</text>
  </svg>`);
}

async function comparison(oldFile, newFile, outFile, title, oldSub, newSub) {
  const panelW = 760, panelH = 760, gap = 20, titleH = 110, totalW = panelW * 2 + gap, totalH = titleH + panelH;
  const oldImg = await sharp(oldFile).resize(panelW, panelH, { fit: "contain", background: "#172129" }).png().toBuffer();
  const newImg = await sharp(newFile).resize(panelW, panelH, { fit: "contain", background: "#172129" }).png().toBuffer();
  const labels = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${totalW}" height="${panelH}">
    <rect x="0" y="0" width="${panelW}" height="52" fill="#111920" fill-opacity="0.92"/>
    <rect x="${panelW + gap}" y="0" width="${panelW}" height="52" fill="#111920" fill-opacity="0.92"/>
    <text x="24" y="34" fill="#D4DADF" font-family="Arial,sans-serif" font-size="19" font-weight="700">OLD — ${oldSub}</text>
    <text x="${panelW + gap + 24}" y="34" fill="#F4F1E8" font-family="Arial,sans-serif" font-size="19" font-weight="700">V2 — ${newSub}</text>
    <line x1="${panelW + gap / 2}" y1="0" x2="${panelW + gap / 2}" y2="${panelH}" stroke="#F28C28" stroke-width="3"/>
  </svg>`);
  await sharp({ create: { width: totalW, height: totalH, channels: 4, background: "#111920" } })
    .composite([
      { input: titleCard(title, "same project asset family / V2 preserves the formal concept silhouette", totalW), top: 0, left: 0 },
      { input: oldImg, top: titleH, left: 0 },
      { input: newImg, top: titleH, left: panelW + gap },
      { input: labels, top: titleH, left: 0 },
    ]).png().toFile(outFile);
}

await comparison(
  path.join(oldPreviewDir, "yamada_k1_preview.png"),
  path.join(previewDir, "yamada_k1_v2_preview.png"),
  path.join(previewDir, "comparison_k1_old_vs_v2.png"),
  "K1 VISUAL REVISION — OLD / V2",
  "box-heavy prototype",
  "human proportion + split armour",
);

await comparison(
  path.join(oldPreviewDir, "central_industrial_belt_preview.png"),
  path.join(previewDir, "central_industrial_belt_v2_preview.png"),
  path.join(previewDir, "comparison_field_old_vs_v2.png"),
  "CENTRAL INDUSTRIAL BELT — OLD / V2",
  "dense single-value scene",
  "foreground / play plane / background separation",
);

console.log("Rendered PNG previews and old/new comparisons.");
