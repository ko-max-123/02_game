# K1 / 中央産業帯 V2 技術取込契約

契約ID: `YTC-DESIGN-TECH-ASSET-V2-20260825`  
対象: `PCR-PROT-20260825-02`

## 1. 権限境界

- デザインマネージャー決定: K1造形、比率、材質、色、動きの見た目、フィールド階層、画面可読性。
- 技術責任者決定: Unity importer、FBX変換、Rig/Animator構成、状態遷移、IK、collision実装、性能、build。
- 技術変換で外観が変わる場合はデザイン側へ差分画像を返す。独断でplate比率、色、socket位置を変更しない。

## 2. 正規ファイルとfallback

| Asset | 正規V2 | fallback |
|---|---|---|
| K1 | `Models/yamada_k1_rigged_v2.glb` | `Models/yamada_k1_segmented_v2.obj` |
| K11 | `Models/k11_rifle_v2.glb` | `Models/k11_rifle_v2.obj` |
| Field表示 | `Models/central_industrial_belt_v2.glb` | `Models/central_industrial_belt_v2.obj` |
| Field衝突 | `Models/central_industrial_belt_collision_v2.obj` | 同左 |

制作環境にBlender／FBX SDKがないため、偽装したFBXは納品しない。UnityへglTF importerを採用するか、技術側の管理するDCC／converterでFBXへ変換する。変換後も以下を維持する。

- 1 unit = 1 m
- Y-up / +Z-forward
- Root Rotation `(0,0,0)` / Scale `(1,1,1)`
- negative／non-uniform scale、shearなし
- material slot名、bone名、socket名、clip名を不変にする

## 3. Rig

Rigは `Generic`、in-place、root motion不使用。

```text
K1_Root
└─ Pelvis
   ├─ Spine_01
   │  └─ Chest
   │     ├─ Neck > Head
   │     ├─ Clavicle_L > UpperArm_L > LowerArm_L > Hand_L
   │     ├─ Clavicle_R > UpperArm_R > LowerArm_R > Hand_R
   │     ├─ JetSocket_L
   │     └─ JetSocket_R
   ├─ UpperLeg_L > LowerLeg_L > Foot_L > Toe_L
   └─ UpperLeg_R > LowerLeg_R > Foot_R > Toe_R

Hand_R
└─ WeaponSocket_R

K11 WeaponRoot
└─ MuzzleSocket
```

- bind pose確定済み。
- weightは正規化し、1頂点最大4bone。現V2の機械装甲は原則1boneへの剛体weight。
- `MuzzleSocket` の正はK1 skeletonではなくK11側。武器交換時にもFX位置を保証するため。
- 左手は必要に応じUnity側IKでK11 foregripへ合わせる。IK追加は技術責任者判断だが、手首の破綻はデザイン確認対象。

## 4. Animation clips

| Clip | Length | Loop | 見た目の要点 |
|---|---:|:---:|---|
| `Idle_Loop` | 1.20 s | yes | 胸と頭の小さな生体感。plateは伸ばさない |
| `WalkForward_Loop` | 0.80 s | yes | 4.2 m/s基準、左右各1接地、腕振り、胸のcounter |
| `WalkDepth_Positive_Loop` | 0.86 s | yes | 奥行き側へ肩を7°まで先行 |
| `WalkDepth_Negative_Loop` | 0.86 s | yes | 反対側へ同等の読みやすさ |
| `Turn180_L/R` | 0.30 s | no | 上半身先行、踏み替え2接地、0.28–0.34 s許容 |
| `Jump_Start` | 0.24 s | no | 沈み込みと離地を分ける |
| `Jump_Loop` | 0.45 s | yes | 空中で脚を軽く引き、棒立ちを避ける |
| `Land` | 0.28 s | no | 両膝とpelvisで衝撃を受ける |
| `Jet_Start` | 0.18 s | no | 胸を約8°伏せて推進方向を示す |
| `Jet_Loop` | 0.50 s | yes | 左右jetの非対称性を残す |
| `Jet_End` | 0.20 s | no | 姿勢をidle／walkへ戻す |
| `Shoot_Recoil` | 0.15 s | no | 右腕だけでなく胸へ小さく反動を返す |

イベント時刻は `asset_manifest_v2.json` を正とする。`WalkForward_Loop` の `Footstep_L=0.00`、`Footstep_R=0.40`、`Shoot_Recoil` の `Fire=0.00` を含む。

### 移動同期

- 前進: 基準速度 `4.2 m/s`、1cycle `0.80 s`。
- 奥行き: 1cycle `0.86 s`。画面上の変位が小さいため前進より少し落ち着かせる。
- 逆方向入力: 後退歩行ではなく、減速 → `Turn180` → 再加速。
- 足滑りが見える場合、外観を変えずAnimator speed／BlendTreeを移動速度へ同期する。

## 5. K1 collider / weapon

- 本体は単純capsule推奨: center Y `0.99`、height `1.88`、radius `0.31`。
- 攻撃・被弾判定は表示meshから分離する。
- 射撃始点・muzzle flash・弾tracerはK11 `MuzzleSocket`。
- `WeaponSocket_R` へK11 `WeaponRoot` を接続し、左手IK目標は技術側で作成する。
- 通常2.5Dカメラで銃身が腕・胸へ完全に埋まらないこと。

## 6. Field表示／衝突

- 表示と衝突は同一原点、同一scale、同一軸。
- collision OBJでは `COL_*` だけを使用する。
- walkable topの表示／衝突Y差は `±0.02 m`以内。
- 背景、遠景、高架、前景rail、marker、危険の見た目をcollisionへ入れない。
- Lane center Z `0`、境界 Z `-2.56 ～ +2.56`。
- trench center X `1.35` は床欠損。赤い底を歩行面にしない。
- spawn／enemy／console座標は `asset_manifest_v2.json`。

## 7. Unity組込み後に技術側から返すもの

1. Import Inspectorのscale／axis／rig／clip一覧画像。
2. 通常カメラ距離の静止画。
3. Idle → Walk → Turn180 → Jump → Jet → Shootを連続して示すcapture。
4. 足元が見える歩行capture。
5. K11のgrip、left-hand contact、muzzle flash位置が見えるcapture。
6. FieldでK1、敵、危険、操作物、HUDが同時に見えるcapture。
7. Windows standalone buildのパスと起動手順。

デザイン側は完成アプリを目視するまで最終承認を出さない。

## 8. Issue #10 技術方式協議記録

記録日: 2026-08-26

- 技術側GLB実体監査: nodes 26、skin 1、joints 22、mesh 1、primitives 8、animations 13。全primitiveの `POSITION / NORMAL / JOINTS_0 / WEIGHTS_0`、剛体weight 1.0、socket、clip尺、SHA-256一致を確認。
- 技術一次決定: 正本GLBをUnity glTFastで直接取り込み、FBX変換を行わない。
- デザイン回答: 再変換による外観、bone／clip／socket名、PBR材質、weightの損失を避ける条件に適合するため合意。
- 正式確定条件: Unity実取込後にroot R0/S1、2.057 m最高点、階層、SkinnedMeshRenderer、13 clips、material slot／manifest色、socket、通常距離での外観を技術側とデザイン側で確認する。
- 承認状態: `PENDING`。完成Windowsアプリ未確認。

