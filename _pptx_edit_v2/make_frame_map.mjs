import fs from "node:fs/promises";

const edits = new Map([
  [1, ["sh/547294r6"]],
  [2, ["sh/a10jqpsj", "sh/x4r21kru", "sh/mtwrmxg7", "sh/ove9o7yd"]],
  [6, ["sh/98rqt4r6", "sh/ml07i9sv", "sh/h4bupgn6", "sh/v2tcn650"]],
  [8, ["sh/o7ydoret", "sh/6h0fypgb", "sh/tkby9kzm"]],
  [10, ["sh/l036l83y"]],
  [11, ["sh/98rytw72"]],
  [16, ["sh/9kby1g7m", "tb/ahc3ypgj"]],
  [20, ["sh/qpwjmh8n"]],
  [22, ["sh/7a18rydc", "tb/0nu5gv2p"]],
  [23, ["tb/r2x8vi5c"]],
  [25, ["sh/obyt0bel", "sh/elwbu1wj"]],
  [31, ["tb/v6tc7q1c"]],
  [36, ["tb/7upwfido"]],
  [40, ["sh/x8ny10z2", "sh/alwfql0r", "sh/1wnmx4vq", "sh/fu54vudk"]],
  [41, ["sh/ruh8by9w", "sh/f298ju10", "sh/g3i9czil", "sh/25krep0r"]],
  [42, ["sh/nmxwfetc", "tb/ovixwva5"]],
  [47, ["sh/503e9ora", "sh/4zadgjap", "tb/r69grihc"]],
  [52, ["tb/7a5sfit8"]],
  [55, ["sh/gv6t0bqp", "sh/0rqtcfqt"]],
]);

const inventoryText = await fs.readFile(
  "D:/05_codex/02_game/_pptx_edit_v2/template-inspect/template-inspect.ndjson",
  "utf8",
);
const inheritedTextIds = new Map();
for (const line of inventoryText.split(/\r?\n/)) {
  if (!line.trim()) continue;
  const record = JSON.parse(line);
  const editable = ["textbox", "table"].includes(record.kind)
    || (record.kind === "shape" && String(record.name ?? "").includes("Placeholder"));
  if (!Number.isInteger(record.slide) || !editable) continue;
  if (!inheritedTextIds.has(record.slide)) inheritedTextIds.set(record.slide, []);
  inheritedTextIds.get(record.slide).push(record.id);
}

const outputSlides = [];
for (let i = 1; i <= 55; i += 1) {
  outputSlides.push({
    outputSlide: i,
    sourceSlide: i,
    narrativeRole: "既存統合資料を継承",
    reuseMode: "duplicate-slide",
    editTargets: [...new Set([...(inheritedTextIds.get(i) ?? []), ...(edits.get(i) ?? [])])].map((sourceElementId) => ({
      sourceElementId,
      action: "rewrite",
    })),
  });
}
for (let chapter = 1; chapter <= 20; chapter += 1) {
  outputSlides.push({
    outputSlide: 55 + chapter,
    sourceSlide: 49,
    narrativeRole: `第${chapter}章の敵・5ステージ・ギミック詳細`,
    reuseMode: "duplicate-slide",
    editTargets: [...new Set(inheritedTextIds.get(49) ?? [])].map((sourceElementId) => ({
      sourceElementId,
      action: "rewrite",
    })),
  });
}

await fs.writeFile(
  "D:/05_codex/02_game/_pptx_edit_v2/template-frame-map.json",
  `${JSON.stringify({ outputSlides, omittedSourceSlides: [] }, null, 2)}\n`,
  "utf8",
);
