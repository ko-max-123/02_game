import fs from "node:fs/promises";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const OUTPUT = "D:\\05_codex\\02_game\\002_校正者成果物\\08_校正者_ストーリー成果物校正報告_20260819.pptx";
const PREVIEW_DIR = "D:\\05_codex\\02_game\\.tmp_story_proof_20260819\\proof-report-preview";

const COLORS = {
  canvas: "#FFFFFF",
  ink: "#111111",
  muted: "#5F6368",
  panel: "#EDEDED",
  panelSoft: "#F7F7F7",
  rule: "#B8BCC4",
  accent: "#3D8DFF",
  pass: "#1E8E5A",
  caution: "#B3261E",
  cautionFill: "#FCE8E6",
};

const FONT = "Yu Gothic";

function addText(slide, text, position, options = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    name: options.name,
    position,
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = {
    fontSize: options.fontSize ?? 24,
    typeface: FONT,
    color: options.color ?? COLORS.ink,
    bold: options.bold ?? false,
    alignment: options.alignment ?? "left",
    verticalAlignment: options.verticalAlignment ?? "top",
    autoFit: options.autoFit ?? "shrinkText",
  };
  return shape;
}

function addRect(slide, position, fill, line = COLORS.rule, width = 1, name) {
  return slide.shapes.add({
    geometry: "rect",
    name,
    position,
    fill,
    line: { style: "solid", fill: line, width },
  });
}

function addRule(slide, left, top, width, color = COLORS.rule, height = 2) {
  return addRect(slide, { left, top, width, height }, color, color, 0);
}

function addPageNumber(slide, number) {
  addText(slide, String(number).padStart(2, "0"), { left: 1180, top: 665, width: 48, height: 24 }, {
    fontSize: 14,
    color: COLORS.muted,
    alignment: "right",
    name: `page-${number}`,
  });
}

function addHeader(slide, kicker, title, page) {
  addText(slide, kicker, { left: 64, top: 28, width: 500, height: 28 }, {
    fontSize: 16,
    bold: true,
    color: COLORS.muted,
    name: `kicker-${page}`,
  });
  addText(slide, title, { left: 64, top: 72, width: 1152, height: 74 }, {
    fontSize: 46,
    bold: true,
    name: `title-${page}`,
  });
  addRule(slide, 64, 158, 1152, COLORS.ink, 2);
  addPageNumber(slide, page);
}

function addSources(slide, lines) {
  slide.speakerNotes.textFrame.setText(["[Sources]", ...lines]);
  slide.speakerNotes.setVisible(true);
}

async function writeBlob(path, blob) {
  await fs.writeFile(path, new Uint8Array(await blob.arrayBuffer()));
}

const presentation = Presentation.create({ slideSize: { width: 1280, height: 720 } });

// Slide 1: decision cover, based on Codex Grid slide-01 hierarchy.
{
  const slide = presentation.slides.add();
  slide.background.fill = COLORS.canvas;
  addText(slide, "ROLE 8 / PROOFREADING REPORT", { left: 64, top: 40, width: 620, height: 30 }, {
    fontSize: 18,
    bold: true,
    color: COLORS.muted,
    name: "cover-kicker",
  });
  addText(slide, "校正合格", { left: 64, top: 190, width: 760, height: 130 }, {
    fontSize: 88,
    bold: true,
    color: COLORS.pass,
    name: "cover-decision",
  });
  addText(slide, "ストーリー設定確定案は、\n正典・ゲームプレイ・表示品質の基準を満たす", { left: 64, top: 330, width: 820, height: 150 }, {
    fontSize: 36,
    bold: true,
    name: "cover-message",
  });
  addRect(slide, { left: 930, top: 174, width: 286, height: 346 }, COLORS.panelSoft, COLORS.rule, 1, "cover-summary-panel");
  addText(slide, "判定", { left: 970, top: 216, width: 200, height: 34 }, {
    fontSize: 20,
    bold: true,
    color: COLORS.muted,
  });
  addText(slide, "合格", { left: 970, top: 270, width: 200, height: 64 }, {
    fontSize: 48,
    bold: true,
    color: COLORS.pass,
  });
  addText(slide, "対象\n全16スライド\n\n修正\nなし", { left: 970, top: 362, width: 190, height: 130 }, {
    fontSize: 22,
    bold: true,
  });
  addRule(slide, 64, 590, 1152, COLORS.pass, 10);
  addText(slide, "2026.08.19  |  校正者による正式精査", { left: 64, top: 620, width: 700, height: 32 }, {
    fontSize: 20,
    color: COLORS.muted,
  });
  addPageNumber(slide, 1);
  addSources(slide, [
    "D:/05_codex/02_game/002_緊急タスク成果物/07_ストーリー作成者_ストーリー設定確定案.pptx",
    "D:/05_codex/02_game/000_エージェント共有読み物/01_タスク.txt",
  ]);
}

// Slide 2: evidence table, based on Codex Grid slide-14 information hierarchy.
{
  const slide = presentation.slides.add();
  slide.background.fill = COLORS.canvas;
  addHeader(slide, "REVIEW SCOPE", "正典とスライドを、四つの観点で突合した", 2);
  addText(slide, "対象はストーリー資料だけでなく、ゲームルール・フィールド・敵・用語まで含む。", { left: 64, top: 182, width: 1152, height: 36 }, {
    fontSize: 22,
    color: COLORS.muted,
  });

  const headers = ["観点", "照合対象", "確認内容", "結果"];
  const colX = [64, 270, 590, 1050];
  const colW = [190, 304, 444, 166];
  const rowTop = 246;
  const rowH = 80;
  headers.forEach((header, index) => {
    addRect(slide, { left: colX[index], top: rowTop, width: colW[index], height: 48 }, COLORS.ink, COLORS.ink, 0);
    addText(slide, header, { left: colX[index] + 12, top: rowTop + 10, width: colW[index] - 24, height: 28 }, {
      fontSize: 18,
      bold: true,
      color: COLORS.canvas,
    });
  });

  const rows = [
    ["物語正典", "全20章・用語・結末", "章番号、ORPHEUS／BLACK RAIL／LARK、最終決着", "一致"],
    ["人物", "ytc全15名・協力者・敵", "氏名、役割、生存正史、山田・美緒・真壁の対立軸", "一致"],
    ["ゲーム接続", "ルール・敵・装備・フィールド", "解放時期、ボス、ステージ進行、証拠と報酬", "一致"],
    ["表示品質", "全16スライド・注記", "文字切れ、重なり、誤字、Sources、読み順", "適合"],
  ];
  rows.forEach((row, rowIndex) => {
    const y = rowTop + 48 + rowIndex * rowH;
    row.forEach((value, colIndex) => {
      addRect(slide, { left: colX[colIndex], top: y, width: colW[colIndex], height: rowH }, rowIndex % 2 === 0 ? COLORS.panelSoft : COLORS.canvas, COLORS.rule, 1);
      addText(slide, value, { left: colX[colIndex] + 12, top: y + 14, width: colW[colIndex] - 24, height: rowH - 24 }, {
        fontSize: colIndex === 2 ? 17 : 19,
        bold: colIndex === 0 || colIndex === 3,
        color: colIndex === 3 ? COLORS.pass : COLORS.ink,
        verticalAlignment: "middle",
      });
    });
  });
  addText(slide, "参照範囲：00_INDEX～12_ID_and_Terminology、YTC_Game_Setting_Story_v1、対象PPTX", { left: 64, top: 634, width: 1080, height: 28 }, {
    fontSize: 17,
    color: COLORS.muted,
  });
  addSources(slide, [
    "D:/05_codex/02_game/001_設定資料/00_INDEX.md",
    "D:/05_codex/02_game/001_設定資料/01_Story_20_Chapters.md",
    "D:/05_codex/02_game/001_設定資料/02_Characters.md",
    "D:/05_codex/02_game/001_設定資料/03_Enemies_and_Encounters.md",
    "D:/05_codex/02_game/001_設定資料/09_Game_Rules.md",
    "D:/05_codex/02_game/001_設定資料/10_Fields.md",
    "D:/05_codex/02_game/001_設定資料/12_ID_and_Terminology.md",
  ]);
}

// Slide 3: metric-led evidence, preserving flat Codex Grid rhythm.
{
  const slide = presentation.slides.add();
  slide.background.fill = COLORS.canvas;
  addHeader(slide, "PASS EVIDENCE", "重大な矛盾・誤記・表示不良はゼロ", 3);

  const metrics = [
    ["20", "章", "章番号と因果関係"],
    ["15", "名", "ytc全員の役割と生存"],
    ["16", "枚", "全ページを原寸確認"],
    ["0", "件", "要修正事項"],
  ];
  metrics.forEach((metric, index) => {
    const x = 64 + index * 288;
    if (index > 0) addRule(slide, x - 24, 228, 2, COLORS.rule, 320);
    addText(slide, metric[0], { left: x, top: 220, width: 220, height: 110 }, {
      fontSize: 84,
      bold: true,
      color: index === 3 ? COLORS.pass : COLORS.ink,
    });
    addText(slide, metric[1], { left: x + 132, top: 278, width: 70, height: 44 }, {
      fontSize: 28,
      bold: true,
      color: COLORS.muted,
    });
    addText(slide, metric[2], { left: x, top: 354, width: 225, height: 72 }, {
      fontSize: 21,
      bold: true,
    });
  });

  addRect(slide, { left: 64, top: 492, width: 1152, height: 118 }, COLORS.panelSoft, COLORS.rule, 1);
  addText(slide, "検査結果", { left: 94, top: 522, width: 160, height: 32 }, {
    fontSize: 20,
    bold: true,
    color: COLORS.muted,
  });
  addText(slide, "全16枚レンダリング済み  /  オーバーフローなし  /  Sources 16/16  /  元PPTXの修正なし", { left: 280, top: 516, width: 890, height: 58 }, {
    fontSize: 23,
    bold: true,
    verticalAlignment: "middle",
  });
  addSources(slide, [
    "D:/05_codex/02_game/002_緊急タスク成果物/07_ストーリー作成者_ストーリー設定確定案.pptx",
    "QA: all 16 slides rendered and inspected; slides_test.py reported no overflow; 16 [Sources] blocks present.",
  ]);
}

// Slide 4: producer handoff with explicit conditions.
{
  const slide = presentation.slides.add();
  slide.background.fill = COLORS.canvas;
  addHeader(slide, "PRODUCER HANDOFF", "校正は合格。統合前に四つの補完案を承認する", 4);
  addText(slide, "以下は校正不備ではなく、ストーリー成果物が明示した正典補完の意思決定事項。", { left: 64, top: 182, width: 1152, height: 34 }, {
    fontSize: 21,
    color: COLORS.muted,
  });

  const decisions = [
    ["01", "第18章", "五つ目の都市を「神楽企業自治区」と定義"],
    ["02", "第19章", "第4ステージに、停止コード復元中のK7戦を明記"],
    ["03", "第13章", "「二人の隊長」を「山田とレオナ」へ明確化"],
    ["04", "第16章", "LARKスキャンを任務限定解放し、終了後に恒常化"],
  ];
  decisions.forEach((decision, index) => {
    const y = 248 + index * 82;
    addRect(slide, { left: 64, top: y, width: 74, height: 62 }, COLORS.accent, COLORS.accent, 0);
    addText(slide, decision[0], { left: 64, top: y + 11, width: 74, height: 34 }, {
      fontSize: 22,
      bold: true,
      color: COLORS.canvas,
      alignment: "center",
    });
    addText(slide, decision[1], { left: 170, top: y + 7, width: 140, height: 42 }, {
      fontSize: 24,
      bold: true,
      color: COLORS.accent,
    });
    addText(slide, decision[2], { left: 330, top: y + 7, width: 840, height: 52 }, {
      fontSize: 22,
      bold: true,
    });
    addRule(slide, 170, y + 62, 1000, COLORS.rule, 1);
  });

  addRect(slide, { left: 64, top: 596, width: 1152, height: 64 }, COLORS.ink, COLORS.ink, 0);
  addText(slide, "承認後、01_Story_20_Chapters.md と関連資料を同時更新して統合する。", { left: 90, top: 610, width: 1100, height: 34 }, {
    fontSize: 22,
    bold: true,
    color: COLORS.canvas,
    alignment: "center",
  });
  addSources(slide, [
    "D:/05_codex/02_game/002_緊急タスク成果物/07_ストーリー作成者_ストーリー設定確定案.pptx (slide 15)",
    "D:/05_codex/02_game/001_設定資料/01_Story_20_Chapters.md",
    "D:/05_codex/02_game/001_設定資料/09_Game_Rules.md",
    "D:/05_codex/02_game/001_設定資料/10_Fields.md",
  ]);
}

await fs.mkdir(PREVIEW_DIR, { recursive: true });
for (const [index, slide] of presentation.slides.items.entries()) {
  const stem = `slide-${String(index + 1).padStart(2, "0")}`;
  await writeBlob(`${PREVIEW_DIR}\\${stem}.png`, await presentation.export({ slide, format: "png", scale: 1 }));
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(`${PREVIEW_DIR}\\${stem}.layout.json`, await layout.text());
}
await writeBlob(
  `${PREVIEW_DIR}\\montage.webp`,
  await presentation.export({ format: "webp", montage: true, scale: 1 }),
);
const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(OUTPUT);

console.log(OUTPUT);
