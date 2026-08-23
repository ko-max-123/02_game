import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const starter = "D:/05_codex/02_game/_pptx_edit_v2/template-starter.pptx";
const storyPath = "D:/05_codex/02_game/001_設定資料/01_Story_20_Chapters.md";
const fieldsPath = "D:/05_codex/02_game/001_設定資料/10_Fields.md";
const output = "D:/05_codex/02_game/002_統合スライド/YTC_Game_Project_Definition_v2_校正再提出版.pptx";

const presentation = await PresentationFile.importPptx(await FileBlob.load(starter));
const inspected = await presentation.inspect({
  kind: "slide,textbox,table,notes",
  maxChars: 350000,
});
const records = inspected.ndjson.split(/\r?\n/).filter(Boolean).map((line) => JSON.parse(line));

function recordOn(slide, kind, needle) {
  const hit = records.find((r) => r.slide === slide && r.kind === kind && String(r.text ?? r.textPreview ?? "").includes(needle));
  if (!hit) throw new Error(`Missing ${kind} on slide ${slide}: ${needle}`);
  return hit;
}

function replaceText(slide, needle, replacement) {
  const hit = recordOn(slide, "textbox", needle);
  const shape = presentation.resolve(hit.id);
  shape.text.replace(needle, replacement);
}

function setText(slide, needle, replacement) {
  const hit = recordOn(slide, "textbox", needle);
  const shape = presentation.resolve(hit.id);
  shape.text = replacement;
}

function tableOn(slide) {
  const hit = records.find((r) => r.slide === slide && r.kind === "table");
  if (!hit) throw new Error(`Missing table on slide ${slide}`);
  return presentation.resolve(hit.id);
}

function setCell(slide, row, column, value) {
  tableOn(slide).cells.set(row, column, value);
}

function setNotes(slideNumber, lines) {
  const slide = presentation.slides.getItem(slideNumber - 1);
  const normalized = lines.at(-1) === "[/Sources]" ? lines : [...lines, "[/Sources]"];
  slide.speakerNotes.textFrame.setText(normalized.join("\n"));
  slide.speakerNotes.setVisible(true);
}

replaceText(1, "Producer Final Draft v1", "Producer Revision v2 / 校正再提出版");

replaceText(2, "技術推奨\n技術責任者がUE5系・データ駆動・オフライン優先を採択。", "技術確定\n技術責任者の正式決定：Unity LTS／URP／C#。Git／Git LFS／GitHub Actions、オフライン完結を基準。");
replaceText(2, "仕様ゲート\n数式・ID・装備枠・章解放の矛盾はP0前に閉じる。", "仕様確定\n数式・ID・装備枠・K1値・章解放を制作正典へ同期済み。");
replaceText(2, "技術推奨", "技術確定");
replaceText(2, "技術責任者がUE5系・データ駆動・オフライン優先を採択。", "正式決定：Unity LTS／URP／C#。Git／Git LFS／GitHub Actions、オフライン完結を基準。");
replaceText(2, "仕様ゲート", "仕様確定");
replaceText(2, "数式・ID・装備枠・章解放の矛盾はP0前に閉じる。", "数式・ID・装備枠・K1値・章解放を制作正典へ同期済み。");
replaceText(2, "緊急指示の必須範囲を一冊に統合。確定／暫定／将来／責任者決定待ちを混同しない。", "緊急指示の必須範囲を一冊に統合。物語・システム・想定技術・機能／非機能要件を確定状態で管理。");

replaceText(10, "空の救出地点", "誰もいない救出地点");
replaceText(11, "空の拠点", "放棄された拠点");

setCell(16, 1, 1, "神楽企業自治区の中央ノード");
setCell(16, 1, 2, "Stage 05で第五都市系統＋全系統を停止");
replaceText(16, "章進行表と詳細設定へ反映する基準。", "章・人物・敵・報酬・フィールドへ同期済みの制作正典。");

replaceText(20, "HP＝心電波", "HP＝バイタル波形");

replaceText(22, "K1の部位構造。戦術装備・ユーティリティ・消耗品の最終参照関係は仕様ゲートで固定。", "戦術装備A/BはCON_*、ユーティリティはEQP_UTIL_*。環境枠は第5章1枠、第18章2枠。");
setCell(22, 7, 0, "戦術A/B＋環境");
setCell(22, 7, 1, "CON_*装填／環境対策");
setCell(22, 7, 2, "回復・攻撃／寒冷・高熱");

setCell(23, 4, 1, "ビームカービン");
setCell(23, 6, 1, "大槌");
replaceText(23, "代表武器。未確定DPS値を大きく見せず、行動差を正典とする。", "代表武器。計算済みDPSは08資料で一元管理し、ここでは行動差を示す。");

replaceText(25, "• K1 Stage I〜IV、FINAL\n• 武器・装備レベル1〜10、品質5段階\n• 3系統の武器改造、装備プリセット3\n• 装備コスト上限で万能構成を防止", "• K1 Stage I〜IV：コスト100→110→125→140\n• 武器・装備レベル1〜10、品質5段階\n• 3系統の武器改造、装備プリセット3\n• 環境枠は第5章1枠、第18章2枠");
replaceText(25, "K1 Stage III／IVの装備コスト増加値は、技術仕様ゲートで確定する。", "Stage I 100／II 110／III 125／IV 140。物語改修のみで上限を増やす。");

setCell(31, 4, 3, "列車分岐／白峰・黄砂・潮境・旧首都・神楽の五都市命令");

setCell(36, 6, 1, "ゲーム速度はオフライン時のみ80%／100%。ポーズは完全停止");

replaceText(40, "Core\nC++：戦闘、セーブ、装備、進行、共通基盤。", "Core\nC#：移動、戦闘、AI、任務、進行、セーブ。");
replaceText(40, "Content\nBlueprint：ギミック、演出、会話、敵・ボス調整。", "Content\nScriptableObject／Addressables：武器・敵・任務・会話をデータ化。");
replaceText(40, "Presentation\nAnimation BP／Montage、Niagara、Chaos、MetaSounds。", "Presentation\nURP、Animator、Timeline、Audio Mixer、UI Toolkit／uGUI。");
replaceText(40, "TECH OWNER RECOMMENDATION\nUnreal Engine 5系の安定版を開始時に固定。対象はWindows PC、3Dモデルを用いる2.5Dリアルタイムアクション。", "TECH OWNER CONFIRMED\nUnity LTS／URP／C#を採用。Git／Git LFS／GitHub Actions。Windows PC向け、オフライン完結の2.5Dリアルタイムアクション。");
replaceText(40, "想定技術は、100ステージをデータと再利用部品で支える", "Unityとデータ駆動で100ステージを支える");
replaceText(40, "C++：戦闘、セーブ、装備、進行、共通基盤。", "C#：移動、戦闘、AI、任務、進行、セーブ。");
replaceText(40, "Blueprint：ギミック、演出、会話、敵・ボス調整。", "ScriptableObject／Addressablesで武器・敵・任務・会話をデータ化。");
replaceText(40, "Animation BP／Montage、Niagara、Chaos、MetaSounds。", "URP、Animator、Timeline、Audio Mixer、UI Toolkit／uGUI。");
replaceText(40, "TECH OWNER RECOMMENDATION", "TECH OWNER CONFIRMED");
replaceText(40, "Unreal Engine 5系の安定版を開始時に固定。対象はWindows PC、3Dモデルを用いる2.5Dリアルタイムアクション。", "Unity LTS／URP／C#を採用。Git／Git LFS／GitHub Actions。Windows PC向け、オフライン完結。");

replaceText(41, "戦闘は専用部品、状態は限定GAS、内容はデータ駆動", "技術基盤は、役割ごとに分離する");
setText(41, "• 移動・射撃・近接・飛行は専用コンポーネント", "• 移動・射撃・近接・飛行をC#コンポーネント化\n• AIは可視化可能な有限状態・規則で構成\n• GAMEPLAY：戦闘、任務、戦利品、ハブ、進行\n• PERSISTENCE：版付きセーブ、設定、実績");
setText(41, "• Primary Data Asset", "• ScriptableObject／Addressables\n• 安定ID、表示名、ローカライズキー、個体GUIDを分離\n• 武器・敵・装備・任務・会話・ドロップをデータ化\n• ID・参照・解放章・報酬を自動検査");

replaceText(42, "暫定目標。フレーム依存を排除し、オフライン本編を壊さない。", "技術責任者の正式要件。フレーム依存を排除し、オフライン本編を壊さない。");
setCell(42, 4, 1, "最低環境SSDでステージ読込15秒以内。ボス再戦30秒以内");
setCell(42, 7, 1, "セーブ破損復旧、進行不能復帰、オフライン完結を受入試験");

replaceText(47, "P0着手前に閉じる、優先度・高の仕様ゲート", "確定仕様は、P0受入試験へ接続する");
replaceText(47, "決定待ちを確定仕様として実装しない。責任者ごとに閉じる。", "技術責任者決定とプロデューサー決定を制作正典へ同期済み。");
setCell(47, 0, 0, "領域");
setCell(47, 0, 1, "確定仕様");
setCell(47, 0, 2, "P0検証");
setCell(47, 1, 0, "数値");
setCell(47, 1, 1, "STAT150≈1.11／DPSはリロード込み／Penetration 0–100");
setCell(47, 1, 2, "計算表・ゲーム・自動テストが一致");
setCell(47, 2, 0, "ID・装備");
setCell(47, 2, 1, "CUR/MAT/CON/DAT/EVD/COL。戦術2枠はCON_*、UTILは恒久装置");
setCell(47, 2, 2, "重複ID・参照・移行を自動検査");
setCell(47, 3, 0, "K1・進行");
setCell(47, 3, 1, "コスト100→110→125→140。環境枠1→2。ソロはCP復帰");
setCell(47, 3, 2, "章報酬・セーブ・装備画面で一致");
setCell(47, 4, 0, "解放");
setCell(47, 4, 1, "爆発MOD ch9／P6 ch13／基本・強化フレア／LARK限定→恒常");
setCell(47, 4, 2, "章別アンロックテスト");
setCell(47, 5, 0, "物語");
setCell(47, 5, 1, "第五都市＝神楽／K7 Stage04／K8核→座標→停止コード");
setCell(47, 5, 2, "章・敵・報酬・フィールドを相互監査");
setCell(47, 6, 0, "校正");
setCell(47, 6, 1, "全ページ再描画・出典ノート更新・正典同期");
setCell(47, 6, 2, "校正者合格後のみ最終版");

setCell(48, 3, 2, "確定済み計算・ID・セーブ・性能をP0で回帰検証");

setCell(52, 3, 2, "五都市（神楽中央ノード）／複合環境／時間評価／LARK救済");

replaceText(53, "詳細数値は04〜08資料。数式・上限はGate 0後に再計算。", "詳細数値は04〜08資料。数式・上限は確定済み、P0で回帰検証。");
setCell(54, 3, 2, "上限・属性・部位倍率は確定値をP0で検証");

replaceText(55, "PRODUCER FINAL DIRECTION", "PRODUCER REVISION FOR PROOFREADING");
replaceText(55, "次の正式ゲートは、物語決定の正典反映と、技術責任者による数式・ID・装備枠・セーブ・性能予算の承認。その後P0へ進む。", "技術責任者の確定仕様と物語決定を正典へ同期。全20章の敵・5ステージ・ギミックを付録で追跡可能にした。校正合格後のみ最終版とする。");

setNotes(2, [
  "[Sources]",
  "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.9–11, p.17",
  "- 000_エージェント共有読み物/01_タスク.txt",
  "- 002_校正者成果物/09_校正者_YTC統合企画要件定義v1_校正報告_20260819.md",
]);
setNotes(40, ["[Sources]", "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.9, p.17"]);
setNotes(41, ["[Sources]", "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.9"]);
setNotes(42, ["[Sources]", "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.13"]);
setNotes(43, ["[Sources]", "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.12–14"]);
setNotes(47, [
  "[Sources]",
  "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.10–11",
  "- 001_設定資料/DOCUMENT_REVIEW_SUMMARY.md, 2026-08-19確定・解決記録",
]);
setNotes(23, [
  "[Sources]",
  "- 001_設定資料/04_Weapons.md",
  "- 001_設定資料/08_Weapon_Stats.md",
  "- 002_技術責任者成果物/04_技術責任者_武器DPS確定_20260823.md",
]);
setNotes(48, [
  "[Sources]",
  "- 000_エージェント共有読み物/01_タスク.txt",
  "- 000_エージェント共有読み物/エージェント役割設定.txt",
  "- 002_スライド/03_技術責任者_技術要件定義_v1.pptx, p.10–14",
]);
setNotes(53, [
  "[Sources]",
  "- 001_設定資料/04_Weapons.md",
  "- 001_設定資料/05_Equipment.md",
  "- 001_設定資料/06_Loot_and_Items.md",
  "- 001_設定資料/08_Weapon_Stats.md",
  "- 002_技術責任者成果物/04_技術責任者_武器DPS確定_20260823.md",
]);
setNotes(54, [
  "[Sources]",
  "- 001_設定資料/07_Player_Stats.md",
  "- 001_設定資料/08_Weapon_Stats.md",
  "- 001_設定資料/12_ID_and_Terminology.md",
  "- 002_技術責任者成果物/04_技術責任者_武器DPS確定_20260823.md",
]);
setNotes(55, [
  "[Sources]",
  "- 002_校正者成果物/09_校正者_YTC統合企画要件定義v1_校正報告_20260819.md",
  "- 001_設定資料/DOCUMENT_REVIEW_SUMMARY.md, 2026-08-19確定・解決記録",
]);

function cleanMarkdown(text) {
  return text.replace(/`/g, "").replace(/\*\*/g, "").trim();
}

const story = await fs.readFile(storyPath, "utf8");
const fields = await fs.readFile(fieldsPath, "utf8");
const fieldStageRows = [...fields.matchAll(/^\|\s*(\d{2})\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|$/gm)].map((match) => ({
  stage: Number(match[1]),
  name: cleanMarkdown(match[2]),
  gimmick: cleanMarkdown(match[3]),
}));
if (fieldStageRows.length !== 100) throw new Error(`Expected 100 field stage rows, got ${fieldStageRows.length}`);
const chapterRegex = /^## 第(\d+)章「([^」]+)」\s*$/gm;
const matches = [...story.matchAll(chapterRegex)];
if (matches.length !== 20) throw new Error(`Expected 20 chapters, got ${matches.length}`);

for (let index = 0; index < matches.length; index += 1) {
  const match = matches[index];
  const number = Number(match[1]);
  const title = match[2];
  const start = match.index + match[0].length;
  const end = index + 1 < matches.length ? matches[index + 1].index : story.length;
  const block = story.slice(start, end);
  const stagesText = block.split("### ステージ進行")[1]?.split("### 出現する敵")[0] ?? "";
  const enemiesText = block.split("### 出現する敵")[1]?.split("### ステージギミック")[0] ?? "";
  const gimmicksText = block.split("### ステージギミック")[1]?.split("### 結末・解放")[0] ?? "";
  const stages = [...stagesText.matchAll(/^\d+\.\s+(.+)$/gm)].map((m) => cleanMarkdown(m[1]));
  const enemyLines = [...enemiesText.matchAll(/^-\s+(.+)$/gm)].map((m) => cleanMarkdown(m[1]));
  const enemyLabels = enemyLines.map((line) => line.match(/^([A-Z][A-Z0-9_]+)(?:\s|$)/)?.[1] ?? line);
  const gimmicks = [...gimmicksText.matchAll(/^-\s+(.+)$/gm)].map((m) => cleanMarkdown(m[1]));
  if (stages.length !== 5 || enemyLabels.length === 0 || gimmicks.length === 0) {
    throw new Error(`Chapter ${number} parse failed: stages=${stages.length}, enemies=${enemyLabels.length}, gimmicks=${gimmicks.length}`);
  }

  const slideNumber = 55 + number;
  replaceText(slideNumber, "付録：第1〜5章の敵とステージギミック", `付録詳細 ${String(number).padStart(2, "0")}｜${title}`);
  replaceText(slideNumber, "章ごとの敵・ボス・ギミックを、学習内容と物語上の役割に結び付ける。", `全敵：${enemyLabels.join("／")}`);
  replaceText(slideNumber, "49", String(slideNumber));
  setCell(slideNumber, 0, 0, "STAGE");
  setCell(slideNumber, 0, 1, "進行");
  setCell(slideNumber, 0, 2, "主なギミック");
  for (let stage = 0; stage < 5; stage += 1) {
    const fieldStage = fieldStageRows[(number - 1) * 5 + stage];
    const expectedStage = ((number - 1) % 2) * 5 + stage + 1;
    if (fieldStage.stage !== expectedStage) {
      throw new Error(`Chapter ${number} stage ${stage + 1}: expected field stage ${expectedStage}, got ${fieldStage.stage}`);
    }
    setCell(slideNumber, stage + 1, 0, String(stage + 1).padStart(2, "0"));
    setCell(slideNumber, stage + 1, 1, stages[stage]);
    setCell(slideNumber, stage + 1, 2, fieldStage.gimmick);
  }
  setNotes(slideNumber, [
    "[Sources]",
    `- 001_設定資料/01_Story_20_Chapters.md, 第${number}章「${title}」`,
    "- 001_設定資料/03_Enemies_and_Encounters.md",
    "- 001_設定資料/10_Fields.md",
  ]);
}

for (let slideNumber = 2; slideNumber <= 75; slideNumber += 1) {
  const pageRecord = records.find((r) =>
    r.slide === slideNumber
    && r.kind === "textbox"
    && /^\d+$/.test(String(r.text ?? ""))
    && Array.isArray(r.bbox)
    && r.bbox[0] > 1100
    && r.bbox[1] > 630
  );
  if (!pageRecord) throw new Error(`Missing page number on slide ${slideNumber}`);
  const pageShape = presentation.resolve(pageRecord.id);
  pageShape.text = String(slideNumber);
  pageShape.text.style = {
    fontSize: 15,
    typeface: "Yu Gothic",
    color: "#5D6770",
    alignment: "right",
    verticalAlignment: "bottom",
    wrap: "none",
    autoFit: "none",
    insets: { top: 0, right: 0, bottom: 0, left: 0 },
  };
}

await fs.mkdir("D:/05_codex/02_game/002_統合スライド", { recursive: true });
const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(output);

const finalInspect = await presentation.inspect({ kind: "slide,textbox,table,notes", maxChars: 350000 });
await fs.writeFile("D:/05_codex/02_game/_pptx_edit_v2/final-inspect.ndjson", finalInspect.ndjson || "", "utf8");
