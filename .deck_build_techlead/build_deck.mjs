import fs from "node:fs/promises";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const OUT = "D:/05_codex/02_game/002_スライド/03_技術責任者_技術要件定義_v1.pptx";
const RENDER_DIR = "D:/05_codex/02_game/.deck_build_techlead/artifact_render";

const W = 1280;
const H = 720;
const C = {
  canvas: "#FFFFFF",
  ink: "#111111",
  muted: "#5F6368",
  panel: "#EFEFEF",
  panel2: "#F7F7F7",
  rule: "#B8BCC4",
  orange: "#F28C28",
  orangeSoft: "#FFF1E3",
  blue: "#3D8DFF",
  blueSoft: "#EAF3FF",
  red: "#B42318",
  redSoft: "#FDECEA",
  green: "#137A4A",
  greenSoft: "#E7F5ED",
};
const FONT = "Yu Gothic";

function addText(slide, text, x, y, w, h, options = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    name: options.name,
    position: { left: x, top: y, width: w, height: h },
    fill: options.fill ?? "none",
    line: options.line ?? { style: "solid", fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = {
    fontSize: options.fontSize ?? 20,
    typeface: options.typeface ?? FONT,
    color: options.color ?? C.ink,
    bold: options.bold ?? false,
    alignment: options.alignment ?? "left",
    verticalAlignment: options.verticalAlignment ?? "top",
    autoFit: options.autoFit ?? "shrinkText",
  };
  return shape;
}

function addRect(slide, x, y, w, h, fill, options = {}) {
  return slide.shapes.add({
    geometry: options.geometry ?? "rect",
    name: options.name,
    position: { left: x, top: y, width: w, height: h },
    fill,
    line: options.line ?? { style: "solid", fill: options.lineColor ?? fill, width: options.lineWidth ?? 0 },
    borderRadius: options.borderRadius,
  });
}

function addLine(slide, x, y, w, h = 0, color = C.rule, width = 1) {
  return slide.shapes.add({
    geometry: "line",
    position: { left: x, top: y, width: w, height: h },
    fill: "none",
    line: { style: "solid", fill: color, width },
  });
}

function addHeader(slide, title, section, page) {
  addText(slide, title, 48, 34, 1060, 66, { fontSize: 38, bold: true, autoFit: "shrinkText" });
  addText(slide, section, 1088, 42, 142, 28, { fontSize: 15, color: C.muted, alignment: "right" });
  addLine(slide, 48, 116, 1184, 0, C.rule, 1);
  addText(slide, String(page).padStart(2, "0"), 1180, 674, 50, 22, { fontSize: 14, color: C.muted, alignment: "right" });
}

function addBullets(slide, items, x, y, w, options = {}) {
  const size = options.fontSize ?? 20;
  const gap = options.gap ?? 10;
  const rowH = options.rowH ?? 48;
  items.forEach((item, i) => {
    addText(slide, "•", x, y + i * (rowH + gap), 24, rowH, { fontSize: size + 2, bold: true, color: options.bulletColor ?? C.orange });
    addText(slide, item, x + 32, y + i * (rowH + gap), w - 32, rowH, { fontSize: size, color: options.color ?? C.ink, autoFit: "shrinkText" });
  });
}

function addNotes(slide, sources, note = "") {
  const block = [
    "[Sources]",
    ...sources.map((s) => `- ${s}`),
    note ? `\n${note}` : "",
  ].filter(Boolean).join("\n");
  slide.speakerNotes.textFrame.setText(block);
}

function addTable(slide, values, x, y, w, h, columnWidths, options = {}) {
  const table = slide.tables.add({
    rows: values.length,
    columns: values[0].length,
    left: x,
    top: y,
    width: w,
    height: h,
    columnWidths,
    values,
  });
  table.borders.assign({ style: "solid", fill: C.rule, width: 1 });
  for (let r = 0; r < values.length; r++) {
    for (let c = 0; c < values[0].length; c++) {
      const cell = table.getCell(r, c);
      cell.fill = r === 0 ? C.ink : (r % 2 === 0 ? C.panel2 : C.canvas);
      cell.text.style = {
        fontSize: r === 0 ? (options.headerSize ?? 16) : (options.bodySize ?? 16),
        typeface: FONT,
        color: r === 0 ? C.canvas : C.ink,
        bold: r === 0 || (options.boldFirstColumn && c === 0),
      };
    }
  }
  return table;
}

function sectionLabel(slide, text, x, y, color = C.orange) {
  addRect(slide, x, y + 4, 8, 28, color);
  addText(slide, text, x + 20, y, 300, 38, { fontSize: 23, bold: true });
}

async function writeBlob(path, blob) {
  await fs.writeFile(path, new Uint8Array(await blob.arrayBuffer()));
}

async function main() {
  await fs.mkdir(RENDER_DIR, { recursive: true });
  await fs.mkdir("D:/05_codex/02_game/002_スライド", { recursive: true });

  const deck = Presentation.create({ slideSize: { width: W, height: H } });

  // 1. Cover
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addRect(s, 0, 0, 18, H, C.orange);
    addText(s, "TECHNICAL REQUIREMENTS / v1.0", 52, 48, 560, 32, { fontSize: 17, bold: true, color: C.muted });
    addText(s, "YTC: Deliver Tomorrow", 52, 176, 1040, 84, { fontSize: 62, bold: true, autoFit: "none" });
    addText(s, "技術・機能・非機能要件定義", 52, 278, 930, 80, { fontSize: 43, bold: true, color: C.orange });
    addText(s, "ソロで完結する2.5D装備収集アクションを、データ駆動で完成させる", 52, 448, 970, 66, { fontSize: 26, color: C.ink });
    addText(s, "2026.08.20  |  技術責任者", 52, 620, 480, 30, { fontSize: 17, color: C.muted });
    addNotes(s, ["001_設定資料/00_INDEX.md", "001_設定資料/11_Producer_Direction.md"]);
  }

  // 2. Product contract
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "完成条件は『ソロで最後まで遊べること』から始める", "PRODUCT CONTRACT", 2);
    addText(s, "3Dメカの手触りを、横スクロールの読みやすさで成立させる。", 48, 148, 1030, 62, { fontSize: 30, bold: true });
    const metrics = [
      ["20", "章", "物語は本編で完結"],
      ["10", "フィールド", "環境差で装備判断"],
      ["25–35h", "本編", "買い切り・オフライン"],
      ["1→4", "人", "P0–P4は1人／協力はP5"],
    ];
    metrics.forEach((m, i) => {
      const x = 48 + i * 296;
      addLine(s, x, 264, 252, 0, i === 3 ? C.blue : C.orange, 5);
      addText(s, m[0], x, 286, 250, 70, { fontSize: 46, bold: true, color: C.ink });
      addText(s, m[1], x, 354, 250, 30, { fontSize: 20, bold: true, color: C.muted });
      addText(s, m[2], x, 408, 250, 74, { fontSize: 18, color: C.ink });
    });
    addText(s, "対象外", 48, 548, 110, 30, { fontSize: 18, bold: true, color: C.red });
    addText(s, "競技PvP  /  性能課金  /  有料ランダムガチャ  /  毎日ログイン前提  /  サービス終了で本編が遊べない構成", 164, 544, 1030, 58, { fontSize: 18, color: C.ink });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/11_Producer_Direction.md"]);
  }

  // 3. Story arc
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "物語の5幕が、学習するゲームシステムを段階的に増やす", "STORY × SYSTEM", 3);
    addLine(s, 72, 280, 1130, 0, C.ink, 2);
    const acts = [
      ["1–4", "拾った火種", "基本操作\nドロップ\n武器相性"],
      ["5–8", "黒い流通網", "環境装備\n護衛\nジャミング"],
      ["9–12", "鎧の値段", "高熱\n装備制限\n拠点防衛"],
      ["13–16", "誰に届けるか", "共闘\n空中戦\n民間人保護"],
      ["17–20", "明日を届ける", "複合環境\n連続作戦\n最終決戦"],
    ];
    acts.forEach((a, i) => {
      const x = 62 + i * 232;
      addRect(s, x, 270, 18, 18, i < 4 ? C.orange : C.blue, { geometry: "ellipse" });
      addText(s, `CH ${a[0]}`, x, 216, 160, 28, { fontSize: 17, bold: true, color: C.muted });
      addText(s, a[1], x, 316, 208, 42, { fontSize: 22, bold: true });
      addText(s, a[2], x, 382, 190, 130, { fontSize: 18, color: C.ink });
    });
    addText(s, "K1の独立認証核LARKを軸に、兵器流通の証拠回収が『装備成長』と『真相解明』を同時に進める。", 72, 570, 1080, 54, { fontSize: 23, bold: true, color: C.orange });
    addNotes(s, ["001_設定資料/01_Story_20_Chapters.md", "001_設定資料/00_INDEX.md"]);
  }

  // 4. Design pillars
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "戦闘・装備・物語を、同じ意思決定へ収束させる", "DESIGN PILLARS", 4);
    addText(s, "プレイヤーは毎回、\n『何を持ち込み、\n誰を守り、\n何を持ち帰るか』\nを選ぶ。", 60, 184, 450, 300, { fontSize: 36, bold: true, color: C.ink });
    addLine(s, 544, 164, 0, 424, C.rule, 2);
    const pillars = [
      ["01", "二つの距離", "安全な射撃／危険だが装甲を崩す近接"],
      ["02", "環境を攻略", "寒冷・熱・電磁・浸水へ装備とルートで回答"],
      ["03", "任務の裏側", "戦利品は強化素材であり、事件の証拠でもある"],
      ["04", "15人の会社", "全員が戦闘・整備・情報・生活で機能する"],
      ["05", "笑顔を届ける", "救助・非致死・証拠公開が長期成果につながる"],
    ];
    pillars.forEach((p, i) => {
      const y = 154 + i * 94;
      addText(s, p[0], 586, y, 50, 30, { fontSize: 17, bold: true, color: C.orange });
      addText(s, p[1], 650, y - 2, 210, 34, { fontSize: 22, bold: true });
      addText(s, p[2], 860, y - 2, 340, 56, { fontSize: 17, color: C.ink });
      if (i < pillars.length - 1) addLine(s, 586, y + 66, 614, 0, C.panel, 1);
    });
    addNotes(s, ["001_設定資料/00_INDEX.md", "001_設定資料/11_Producer_Direction.md"]);
  }

  // 5. Core loop
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "6つの状態遷移が、プレイと物語を一周させる", "CORE LOOP", 5);
    const labels = ["本社", "準備", "出撃", "攻略", "回収", "帰還・成長"];
    for (let i = 0; i < labels.length - 1; i++) {
      addRect(s, 218 + i * 194, 296, 42, 34, C.orange, { geometry: "rightArrow" });
    }
    labels.forEach((label, i) => {
      const x = 48 + i * 194;
      addRect(s, x, 250, 160, 126, i === 5 ? C.blueSoft : C.panel2, { lineColor: i === 5 ? C.blue : C.rule, lineWidth: 1 });
      addText(s, String(i + 1).padStart(2, "0"), x + 16, 264, 42, 28, { fontSize: 16, bold: true, color: i === 5 ? C.blue : C.orange });
      addText(s, label, x + 16, 306, 128, 42, { fontSize: 21, bold: true, alignment: "center", verticalAlignment: "middle" });
    });
    addText(s, "会話・任務選択", 50, 398, 155, 28, { fontSize: 15, color: C.muted, alignment: "center" });
    addText(s, "プリセット・推奨タグ", 244, 398, 160, 36, { fontSize: 15, color: C.muted, alignment: "center" });
    addText(s, "8–15分", 438, 398, 160, 28, { fontSize: 15, color: C.muted, alignment: "center" });
    addText(s, "戦闘・環境・目標", 632, 398, 160, 28, { fontSize: 15, color: C.muted, alignment: "center" });
    addText(s, "装備・素材・証拠", 826, 398, 160, 28, { fontSize: 15, color: C.muted, alignment: "center" });
    addText(s, "強化・分解・解析", 1020, 398, 160, 28, { fontSize: 15, color: C.muted, alignment: "center" });
    addText(s, "失敗しても証拠品とチェックポイントまでの素材50%を保持し、再挑戦時は装備変更へ直行できる。", 92, 522, 1060, 58, { fontSize: 22, bold: true, alignment: "center", color: C.ink });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/06_Loot_and_Items.md", "001_設定資料/11_Producer_Direction.md"]);
  }

  // 6. Functional requirements
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "P1で検証すべき機能要件を6領域に固定する", "FUNCTIONAL REQUIREMENTS", 6);
    const rows = [
      ["ID", "領域", "必須要件", "P1合格条件"],
      ["FR-01", "操作", "移動・ジャンプ・飛行・回避・射撃・近接・切替・全入力変更", "入力遅延とキャンセルが一貫"],
      ["FR-02", "戦闘", "Shield→Armor→HP、Break、部位、状態異常、予告、Down/Retry", "30/60fpsで判定同一"],
      ["FR-03", "任務", "目標、護衛、防衛、救助、失敗条件、1–3チェックポイント", "初見で主目標を理解"],
      ["FR-04", "装備", "射撃1・近接1・戦術2・弾薬MOD1・K1各部位・プリセット", "推奨なしでも代替攻略"],
      ["FR-05", "成長", "ドロップ、鑑定、強化、分解、売却、証拠、ランク、スキル", "進行不能を起こさない"],
      ["FR-06", "基盤", "本社ハブ、セーブ、再戦導線、図鑑、設定、ローカライズ", "再起動後に状態復元"],
    ];
    addTable(s, rows, 48, 150, 1184, 482, [92, 132, 650, 310], { bodySize: 16, headerSize: 16, boldFirstColumn: true });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/04_Weapons.md", "001_設定資料/05_Equipment.md", "001_設定資料/06_Loot_and_Items.md"]);
  }

  // 7. Combat model
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "戦闘は『生存資源』と『行動資源』を混ぜない", "COMBAT MODEL", 7);
    sectionLabel(s, "生存資源", 56, 152, C.orange);
    const stages = [
      ["SHIELD", "4秒非被弾で回復", C.blueSoft, C.blue],
      ["ARMOR", "自然回復なし／補修と補給", C.orangeSoft, C.orange],
      ["HP", "医療と補給／0でDown", C.redSoft, C.red],
    ];
    stages.forEach((st, i) => {
      const y = 214 + i * 112;
      addRect(s, 56, y, 470, 78, st[2], { lineColor: st[3], lineWidth: 2 });
      addText(s, st[0], 76, y + 17, 150, 34, { fontSize: 23, bold: true, color: st[3] });
      addText(s, st[1], 228, y + 18, 276, 34, { fontSize: 18, color: C.ink });
      if (i < 2) addRect(s, 266, y + 82, 48, 24, C.ink, { geometry: "downArrow" });
    });
    addLine(s, 574, 150, 0, 430, C.rule, 2);
    sectionLabel(s, "行動資源", 618, 152, C.blue);
    addText(s, "ENERGY", 618, 220, 180, 36, { fontSize: 25, bold: true, color: C.blue });
    addText(s, "ジェット・シールド・ビーム・支援機能で共有。0でも地上移動・実弾・近接は使用可能。", 618, 266, 560, 78, { fontSize: 19 });
    addText(s, "HEAT", 618, 374, 180, 36, { fontSize: 25, bold: true, color: C.orange });
    addText(s, "高出力の継続を制限。100で5秒間、ジェット・ビーム・シールド回復が停止。", 618, 420, 560, 78, { fontSize: 19 });
    addText(s, "射撃＝安全と継続　／　近接＝接近リスクと装甲破壊", 618, 548, 560, 42, { fontSize: 21, bold: true, color: C.ink });
    addNotes(s, ["001_設定資料/07_Player_Stats.md", "001_設定資料/08_Weapon_Stats.md", "001_設定資料/09_Game_Rules.md"]);
  }

  // 8. Content scale
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "100ステージは目標値。品質を守るため、部品化して生産する", "CONTENT MODEL", 8);
    addText(s, "100", 64, 154, 280, 120, { fontSize: 82, bold: true, color: C.orange });
    addText(s, "メインステージ", 66, 272, 260, 36, { fontSize: 22, bold: true });
    addText(s, "20章 × 原則5ステージ\n10フィールド × 10ステージ\n1ステージ 8–15分", 66, 336, 300, 140, { fontSize: 22, color: C.ink });
    addLine(s, 410, 154, 0, 416, C.rule, 2);
    const layers = [
      ["目的部品", "撃破・到達・護衛・防衛・救助・回収・追跡・装置停止"],
      ["戦闘部品", "一般AI・支援AI・Elite・Bossフェーズ・部位・予告"],
      ["環境部品", "寒冷・熱・砂塵・毒・電磁・浸水・強風・低重力"],
      ["物語部品", "会話・証拠・報酬・解放・章フラグ・ハブ更新"],
    ];
    layers.forEach((l, i) => {
      const y = 154 + i * 105;
      addText(s, l[0], 454, y, 180, 34, { fontSize: 21, bold: true, color: i === 3 ? C.blue : C.orange });
      addText(s, l[1], 650, y, 530, 64, { fontSize: 18 });
      addLine(s, 454, y + 78, 726, 0, C.panel, 1);
    });
    addText(s, "品質ゲートを満たせない場合は、ステージ数を先に削減する。", 454, 580, 726, 40, { fontSize: 22, bold: true, color: C.red });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/10_Fields.md", "001_設定資料/11_Producer_Direction.md"]);
  }

  // 9. Architecture
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "技術基盤は『コアがオンラインへ依存しない』構造にする", "TARGET ARCHITECTURE", 9);
    const bands = [
      ["UX / PRESENTATION", "入力抽象化・カメラ・HUD・字幕・アクセシビリティ", C.blueSoft, C.blue],
      ["GAMEPLAY RUNTIME", "移動・戦闘・AI・任務・ドロップ・ハブ・進行", C.orangeSoft, C.orange],
      ["CONTENT DATA", "安定ID・マスターデータ・会話・ローカライズ・Addressable配信", C.panel2, C.muted],
      ["PERSISTENCE / PLATFORM", "版管理セーブ・設定・実績・任意テレメトリ・P5オンライン境界", C.greenSoft, C.green],
      ["ENGINE FOUNDATION", "Unity LTS / URP / C#　＋　Git・Git LFS・GitHub Actions", C.ink, C.canvas],
    ];
    bands.forEach((b, i) => {
      const y = 144 + i * 92;
      addRect(s, 64, y, 1152, 70, b[2], { lineColor: i === 4 ? C.ink : b[3], lineWidth: 1 });
      addRect(s, 64, y, 14, 70, i === 4 ? C.orange : b[3]);
      addText(s, b[0], 98, y + 14, 300, 32, { fontSize: 19, bold: true, color: i === 4 ? C.canvas : b[3] });
      addText(s, b[1], 416, y + 14, 770, 40, { fontSize: 18, color: i === 4 ? C.canvas : C.ink });
    });
    addText(s, "P0–P4ではサーバー停止・未接続でも本編、セーブ、装備、再戦がすべて成立する。", 112, 622, 1050, 40, { fontSize: 21, bold: true, alignment: "center", color: C.blue });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/11_Producer_Direction.md", "001_設定資料/12_ID_and_Terminology.md"], "Unity LTS / URP / C# is the technical lead's proposed baseline, not an externally sourced claim.");
  }

  // 10. Data decisions
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "レビューで見つかった実装矛盾は、データ契約で一本化する", "DATA CONTRACTS", 10);
    const rows = [
      ["論点", "技術決定", "実装への効果"],
      ["出力倍率", "記載式を正とし、STAT150は約1.11倍", "逓減を維持し説明を修正"],
      ["DPS", "継続DPS＝射撃間隔＋リロード込み", "表を自動計算・範囲外をCIで警告"],
      ["アイテムID", "CUR/MAT/CON/DAT/EVD/COLを具体IDに使用", "ITM_は抽象カテゴリのみ"],
      ["装備枠", "戦術2枠はCON_*を装填。EQP_UTIL_*は再使用・常時装置", "回復品の二重マスターを廃止"],
      ["Penetration", "全補正後に0–100へクランプ", "徹甲弾で上限超過しない"],
      ["E14属性", "電撃／衝撃。刺突性はPenetrationで表現", "未定義DMG_PIERCEを増やさない"],
      ["組織と勢力", "organizationIdとfactionIdを別フィールド化", "治安局ORG/FACの競合を解消"],
    ];
    addTable(s, rows, 48, 144, 1184, 500, [230, 510, 444], { bodySize: 16, headerSize: 16 });
    addNotes(s, ["001_設定資料/DOCUMENT_REVIEW_SUMMARY.md", "001_設定資料/06_Loot_and_Items.md", "001_設定資料/08_Weapon_Stats.md", "001_設定資料/12_ID_and_Terminology.md"], "Rows labeled as technical decisions are decisions made by the technical lead for integration.");
  }

  // 11. Progression decisions
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "解放時期と例外ルールも、実装可能な形へ固定する", "PROGRESSION CONTRACTS", 11);
    const rows = [
      ["対象", "確定仕様", "更新先"],
      ["K1コスト", "Stage I 100 → II 110 → III 125 → IV 140", "05 / 07"],
      ["環境枠", "第5章で1枠、第18章で2枠目", "01 / 05"],
      ["ソロ蘇生", "HP0はチェックポイント。緊急蘇生器はP5協力専用", "05 / 07 / 09"],
      ["ロケット", "第9章は爆発MOD、第13章でP6本体", "01 / 04"],
      ["電撃", "E14は第8章、第13章で水面伝播／共闘応用", "01 / 04"],
      ["フレア", "基本版を第7章前、8章報酬は強化版", "01 / 10"],
      ["LARKスキャン", "第16章開始時に任務限定、クリア後に恒常化", "01 / 05"],
    ];
    addTable(s, rows, 84, 150, 1112, 490, [210, 700, 202], { bodySize: 16, headerSize: 16, boldFirstColumn: true });
    addNotes(s, ["001_設定資料/DOCUMENT_REVIEW_SUMMARY.md", "001_設定資料/01_Story_20_Chapters.md", "001_設定資料/05_Equipment.md", "001_設定資料/09_Game_Rules.md"]);
  }

  // 12. Save, localization, accessibility
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "セーブ・文字・入力は、後から直す機能ではない", "FOUNDATION FEATURES", 12);
    addLine(s, 438, 160, 0, 430, C.rule, 1);
    addLine(s, 842, 160, 0, 430, C.rule, 1);
    sectionLabel(s, "SAVE", 60, 158, C.orange);
    addBullets(s, [
      "オート1／手動3／チェックポイント一時保存",
      "原子的書き込み＋直前バックアップ",
      "saveVersionと移行テストを必須化",
      "IDは表示名変更後も再利用しない",
    ], 60, 220, 340, { fontSize: 18, rowH: 58, gap: 14 });
    sectionLabel(s, "LOCALIZATION", 466, 158, C.blue);
    addBullets(s, [
      "全表示文字列をコードから分離",
      "日本語の1.5倍幅までレイアウト保持",
      "語順変更できる文単位で変数管理",
      "固有語は全言語共通表記を固定",
    ], 466, 220, 340, { fontSize: 18, rowH: 58, gap: 14, bulletColor: C.blue });
    sectionLabel(s, "ACCESSIBILITY", 870, 158, C.green);
    addBullets(s, [
      "全入力変更・コントローラー対応",
      "字幕サイズ・背景・話者名",
      "色だけで情報を伝えない",
      "揺れ・点滅・ブラー・連打を調整",
    ], 870, 220, 340, { fontSize: 18, rowH: 58, gap: 14, bulletColor: C.green });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/11_Producer_Direction.md", "001_設定資料/12_ID_and_Terminology.md"]);
  }

  // 13. NFR
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "非機能要件は、プレイ体験を守る測定条件として定義する", "NON-FUNCTIONAL REQUIREMENTS", 13);
    const rows = [
      ["ID", "品質属性", "受入基準"],
      ["NFR-01", "性能", "推奨1080p/60fps（16.7ms）、最低30fps（33.3ms）。入力・判定は時間基準"],
      ["NFR-02", "再試行性", "ボス再戦まで30秒以内。最低環境SSDでステージ読込15秒以内を目標"],
      ["NFR-03", "信頼性", "セーブは原子的書込・世代バックアップ・破損検出。異常終了後も復旧可能"],
      ["NFR-04", "オフライン", "P0–P4の本編・装備・セーブ・再戦はネットワーク不要"],
      ["NFR-05", "可用性", "落下・挟まり・護衛停止から復帰。進行不能を品質ゲートで禁止"],
      ["NFR-06", "プライバシー", "テレメトリは説明・選択可能。会話、個人ファイル、不要な入力履歴を収集しない"],
      ["NFR-07", "保守性", "マスターデータ検証、ID参照検査、計算式テスト、セーブ移行をCIで自動化"],
    ];
    addTable(s, rows, 48, 148, 1184, 500, [120, 180, 884], { bodySize: 16, headerSize: 16, boldFirstColumn: true });
    addNotes(s, ["001_設定資料/09_Game_Rules.md", "001_設定資料/11_Producer_Direction.md", "001_設定資料/DOCUMENT_REVIEW_SUMMARY.md"], "The 15-second stage load target and save implementation details are technical acceptance proposals.");
  }

  // 14. QA gates
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "品質は、実装完了ではなく『壊れず学べること』で判定する", "QUALITY GATES", 14);
    const gates = [
      ["DATA", "参照整合", "ID重複・欠損参照・DPS帯・装備コスト・解放順を自動検査"],
      ["SYSTEM", "決定性", "30/60fps、入力差、セーブ移行、チェックポイント復元を自動検証"],
      ["STAGE", "遊べる", "主目標が分かる／画面外予告／代替攻略／再戦30秒／進行不能なし"],
      ["CHAPTER", "学べる", "新要素を1つ教え、ボスで応用し、報酬が次章で役立つ"],
    ];
    gates.forEach((g, i) => {
      const y = 154 + i * 116;
      addText(s, g[0], 64, y, 120, 30, { fontSize: 17, bold: true, color: i < 2 ? C.blue : C.orange });
      addText(s, g[1], 194, y - 4, 180, 40, { fontSize: 24, bold: true });
      addText(s, g[2], 402, y - 2, 790, 62, { fontSize: 18 });
      addLine(s, 64, y + 76, 1128, 0, C.panel, 2);
    });
    addText(s, "P1は入力感と第1章、P3はコンテンツ生産速度、P4は全章の回帰を重点測定する。", 96, 622, 1080, 34, { fontSize: 21, bold: true, alignment: "center", color: C.ink });
    addNotes(s, ["001_設定資料/11_Producer_Direction.md", "001_設定資料/03_Enemies_and_Encounters.md", "001_設定資料/09_Game_Rules.md"]);
  }

  // 15. Milestones
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "開発はP0からP4までを一本道にし、協力プレイを分離する", "DELIVERY ROADMAP", 15);
    addLine(s, 64, 284, 1148, 0, C.ink, 2);
    const ms = [
      ["P0", "戦闘\nプロトタイプ", "移動・射撃・近接\n飛行・Armor/HP\nAI・再挑戦"],
      ["P1", "バーティカル\nスライス", "第1章5ステージ\nK11・VS-01\nゴライアスT"],
      ["P2", "コアループ", "第2章まで\n回収・強化・売却・セーブ"],
      ["P3", "生産可能状態", "3フィールド\nデータ駆動・QA基準"],
      ["P4", "本編完成", "20章・10フィールド\n人物任務・全文字列管理"],
      ["P5", "将来機能", "最大4人協力\n追加短編・家庭用"],
    ];
    ms.forEach((m, i) => {
      const x = 52 + i * 197;
      addRect(s, x, 273, 20, 20, i === 5 ? C.blue : C.orange, { geometry: "ellipse" });
      addText(s, m[0], x, 208, 60, 32, { fontSize: 18, bold: true, color: i === 5 ? C.blue : C.orange });
      addText(s, m[1], x, 326, 174, 54, { fontSize: 20, bold: true });
      addText(s, m[2], x, 404, 174, 104, { fontSize: 16, color: C.ink });
    });
    addRect(s, 1018, 530, 176, 56, C.blueSoft, { lineColor: C.blue, lineWidth: 1 });
    addText(s, "オンラインは別判定", 1032, 544, 150, 30, { fontSize: 17, bold: true, color: C.blue, alignment: "center" });
    addNotes(s, ["001_設定資料/11_Producer_Direction.md"]);
  }

  // 16. Risks
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "最大リスクは機能不足ではなく、相互作用の暴走にある", "TOP RISKS", 16);
    const rows = [
      ["リスク", "兆候", "技術対策"],
      ["飛行で地上戦を無視", "敵と地形を越える", "対空・Energy・空中報酬。見えない壁を減らす"],
      ["射撃だけが最適", "近接を使わない", "重装甲・弾薬・Break・切替攻撃で回答"],
      ["100面が薄くなる", "反復とQA遅延", "部品化・代表ギミック・品質未達なら面数削減"],
      ["装備整理が主時間", "倉庫疲れ", "特性上限・フィルター・確定品・保護・天井"],
      ["固有名詞が追えない", "物語離脱", "各章2つまで・証拠ボード・章末自動要約"],
      ["オンラインが本編を遅延", "同期都合で設計変更", "P5分離・境界インターフェース・本編依存禁止"],
    ];
    addTable(s, rows, 48, 150, 1184, 490, [300, 280, 604], { bodySize: 16, headerSize: 16 });
    addNotes(s, ["001_設定資料/11_Producer_Direction.md", "001_設定資料/DOCUMENT_REVIEW_SUMMARY.md"]);
  }

  // 17. Producer decisions
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addHeader(s, "技術基盤は着手可能。物語整合の4点だけはプロデューサー決定が必要", "DECISION REQUEST", 17);
    addRect(s, 48, 154, 552, 430, C.redSoft, { lineColor: C.red, lineWidth: 1 });
    addText(s, "プロデューサー決定", 76, 180, 480, 38, { fontSize: 25, bold: true, color: C.red });
    addBullets(s, [
      "第18章『五つの声』の5系統目を定義する",
      "第19章Stage 04へK7戦を明記する",
      "第19章報酬をK8統合核と停止情報に分ける",
      "第3章は『初回撃破』ではなく『初回決着』の例外にする",
    ], 76, 246, 480, { fontSize: 18, rowH: 56, gap: 16, bulletColor: C.red });
    addRect(s, 632, 154, 600, 430, C.greenSoft, { lineColor: C.green, lineWidth: 1 });
    addText(s, "技術責任者の確定範囲", 660, 180, 520, 38, { fontSize: 25, bold: true, color: C.green });
    addBullets(s, [
      "データID・装備枠・計算式・解放順",
      "Unity LTS／URP／C#の基盤とオフライン境界",
      "性能・セーブ・アクセシビリティ・プライバシー基準",
      "P0–P4の品質ゲートとP5オンライン分離",
    ], 660, 246, 520, { fontSize: 18, rowH: 56, gap: 16, bulletColor: C.green });
    addNotes(s, ["001_設定資料/DOCUMENT_REVIEW_SUMMARY.md", "001_設定資料/01_Story_20_Chapters.md", "001_設定資料/10_Fields.md"]);
  }

  // 18. Handoff
  {
    const s = deck.slides.add();
    s.background.fill = C.canvas;
    addText(s, "TECHNICAL HANDOFF", 52, 50, 420, 30, { fontSize: 17, bold: true, color: C.muted });
    addText(s, "P0→P1を先に固定する。", 52, 150, 980, 82, { fontSize: 54, bold: true });
    addText(s, "入力感、武器相性、第1章、セーブを通せば、残りのコンテンツを安全に増やせる。", 52, 250, 1000, 72, { fontSize: 27, color: C.orange, bold: true });
    addLine(s, 52, 364, 1130, 0, C.rule, 2);
    const next = [
      ["01", "プロデューサー", "物語整合4点を決定し、統合版へ反映"],
      ["02", "技術責任者", "要件ID・マスター仕様・P0受入試験を文書化"],
      ["03", "開発チーム", "P0戦闘プロトタイプを計測可能な状態で実装"],
    ];
    next.forEach((n, i) => {
      const x = 52 + i * 390;
      addText(s, n[0], x, 412, 48, 30, { fontSize: 17, bold: true, color: C.blue });
      addText(s, n[1], x, 456, 320, 34, { fontSize: 22, bold: true });
      addText(s, n[2], x, 510, 320, 80, { fontSize: 18 });
    });
    addText(s, "提出: 3. 技術責任者  /  2026.08.20", 52, 650, 430, 26, { fontSize: 16, color: C.muted });
    addNotes(s, ["000_エージェント共有読み物/01_タスク.txt", "001_設定資料/11_Producer_Direction.md", "001_設定資料/DOCUMENT_REVIEW_SUMMARY.md"]);
  }

  for (let i = 0; i < deck.slides.items.length; i++) {
    const slide = deck.slides.items[i];
    const png = await deck.export({ slide, format: "png", scale: 1 });
    await writeBlob(`${RENDER_DIR}/slide-${String(i + 1).padStart(2, "0")}.png`, png);
    const layout = await slide.export({ format: "layout" });
    await fs.writeFile(`${RENDER_DIR}/slide-${String(i + 1).padStart(2, "0")}.layout.json`, await layout.text());
  }

  const montage = await deck.export({ format: "webp", montage: true, scale: 1 });
  await writeBlob(`${RENDER_DIR}/montage.webp`, montage);

  const pptx = await PresentationFile.exportPptx(deck);
  await pptx.save(OUT);
  console.log(JSON.stringify({ output: OUT, slides: deck.slides.items.length, renderDir: RENDER_DIR }));
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
