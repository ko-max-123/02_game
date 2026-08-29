# YTC K1 / 中央産業帯 Design Assets V2

更新日: 2026-08-25  
対象変更要求: `PCR-PROT-20260825-02`  
デザイン状態: **組込み用アセット提出可／完成アプリの最終デザイン承認は未実施**

## 1. 成果物

### K1 / K11

- `Models/yamada_k1_rigged_v2.glb`
  - Generic rig、skin、13 animation clipsを持つ正規V2形状
- `Models/yamada_k1_segmented_v2.obj`
  - 部品名と担当boneをコメントで保持した静的確認・変換用fallback
- `Models/k11_rifle_v2.glb`
  - `WeaponRoot > MuzzleSocket` を持つ別体武器
- `Models/k11_rifle_v2.obj`
  - 静的確認・変換用fallback

### 中央産業帯

- `Models/central_industrial_belt_v2.glb`
  - 表示用。白い試験施設、物流倉庫、高架、夕方の工業地帯
- `Models/central_industrial_belt_v2.obj`
  - 表示用fallback
- `Models/central_industrial_belt_collision_v2.obj`
  - 衝突専用。背景・高架・前景・危険表示を除外

### 契約・確認資料

- `asset_manifest_v2.json`: 寸法、bone、clip、socket、lane、marker、材質、SHA-256
- `TECHNICAL_IMPORT_CONTRACT.md`: 技術担当向け受渡し契約
- `DESIGN_FINAL_APPROVAL_CHECKLIST.md`: 完成アプリで行う最終確認
- `Previews/comparison_k1_old_vs_v2.png`: K1旧版／V2比較
- `Previews/comparison_field_old_vs_v2.png`: フィールド旧版／V2比較
- `Previews/normal_gameplay_distance_v2.png`: 通常2.5Dカメラ距離想定
- `Previews/yamada_k1_v2_preview.png`: K1単体プレビュー
- `Previews/central_industrial_belt_v2_preview.png`: フィールド単体プレビュー

## 2. 正式参照

- K1外観: `002_スライド/assets/character_equipment_prototypes_v1/02_k1_combat_shell_prototype_v1.png`
- 山田: `001_設定資料/02_Characters.md` の山田
- K11/バックパック: `002_スライド/assets/character_equipment_prototypes_v1/03_k1_starting_loadout_prototype_v1.png`
- フィールド: `001_設定資料/10_Fields.md` の中央産業帯
- 制作方針: `001_設定資料/11_Producer_Direction.md`

## 3. K1 V2 デザイン

旧版の大きな箱型胸郭、色だけで表した左右差、棒状の四肢を廃止した。V2は正式図に合わせ、黒い可動内装の上へ白灰の分割装甲を載せた細身の人型中量シルエットである。

- 低い額、横長ミントvisor、顎の黒い可動部で頭部を一目でK1と判別
- 胸部は左右分割plateと中央rail。橙の配線、笑顔印、修理痕を近距離情報として配置
- 肩・肘・膝・足首の黒い間隙を残し、通常距離でも関節方向が読める
- 左肩sensor、右前腕trial rail、左右で異なるjet pod、左脚braceで非対称性を形状化
- K11は白灰／黒／ytc橙のcompact rifle。銃口位置はweapon側socketを正とする
- 装甲は剛体寄りweight。関節だけが曲がり、plateがゴム状に伸びないことを前提とする

### 寸法・原点

| 項目 | 値 |
|---|---:|
| 単位 | 1 unit = 1 m |
| 軸 | Y-up / +Z-forward |
| 原点 | 両足中央の接地面 `(0, 0, 0)` |
| antenna込み最高点 | Y = 2.057 m |
| 表示mesh最低点 | Y = 0.030 m |
| bind pose幅 | 1.324 m |
| 最大奥行き | 0.737 m |
| Unity想定Transform | Rotation `(0,0,0)` / Scale `(1,1,1)` |

### 当たり判定方針

- プレイヤー本体: 原点基準の単純capsuleを使用する。初期目安は center Y `0.99 m`、height `1.88 m`、radius `0.31 m`。
- 足元: ground probeは原点から下方向。装甲meshを直接衝突判定へ使わない。
- 被弾部位を分ける場合も `Head / Torso / Limb` の少数primitiveを使い、表示plate単位に増やさない。
- K11: 通常射撃は `MuzzleSocket` 起点。表示meshを弾道colliderにしない。

## 4. 色・材質

| 役割 | Material | 基準色 | 用途 |
|---|---|---|---|
| K1主装甲 | `K1_CeramicWhite` | `#D9D9D2` | 主要silhouette |
| K1明装甲 | `K1_WarmWhite` | `#ECE9DF` | plate分割 |
| 機構 | `K1_Gunmetal` | `#343B40` | frame、武器、試験部品 |
| 可動部 | `K1_Undersuit` | `#161C20` | 関節、内装 |
| ytc識別 | `YTC_Orange` | `#F28C28` | 配線、latch、笑顔印 |
| sensor | `K1_VisorMint` | `#8DE4D0` | visor、jet、LARK系発光 |
| 修理 | `K1_Repair` | `#696B69` | 溶接痕、brace |
| 歩行面 | `Field_WalkableTop` | `#F0EFE8` | 明るい接地面 |
| 足場縁 | `Field_Edge` | `#293137` | 輪郭と段差 |
| 環境危険 | `Field_Hazard` | `#E7B83E` | 黄＋歯形／縁形状 |
| 即時危険 | `Field_Danger` | `#C93F3F` | 赤＋床欠損／三角歯 |
| 操作物 | `Field_Interactive` | `#39A9DB` | 青＋fork形状console |

色だけには依存しない。歩行面は明度と連続した平面、危険は床欠損・三角歯・太いlip、操作物は直立fork silhouetteで識別する。

## 5. 中央産業帯 V2 デザイン

- 前景: 暗い細railと少数post。画面下端のframeとして扱い、K1・敵を隠さない。
- 中景: 最も明るい歩行面、濃いedge、段差riser。操作・戦闘判断の主層。
- 背景: 中明度の試験施設／倉庫、大きな面、低密度。敵sensorと同じ赤を置かない。
- 遠景: 暗い高架／煙突／tankと抑えた夕景。reticleやHUDより先に目へ入らない。
- 奥行きlane: center Z `0.0 m`、可視境界 `-2.56 ～ +2.56 m`。
- 赤いtrenchは表示されるがcollision floorを置かず、実際の床欠損と一致させる。

Fieldの正確なbounds、spawn、marker、walkable top Yは `asset_manifest_v2.json` を参照する。

## 6. ライセンス

本フォルダのV2形状、材質、preview、生成sourceは本プロジェクト向けのオリジナル制作物である。詳細は `LICENSE.md`。

## 7. 再生成

`Source/generate_design_assets_v2.mjs` がmodel、manifest、SVG previewを生成し、`Source/render_previews_v2.mjs` がPNGと旧／新比較を生成する。生成sourceは制作履歴と再現性のため同梱する。

