# PCR-PROT-20260825-02 デザイン最終承認チェック

判定日: 未実施  
判定者: デザインマネージャー  
総合判定: **PENDING — 完成Windowsアプリ未確認のため承認しない**

## A. アセット単体ゲート

- [x] 正式K1図に対し、細身人型、白灰分割装甲、黒可動部、横長visorを反映
- [x] 左肩sensor、右前腕rail、左右jet差、左脚修理braceを形状で表現
- [x] ytc橙を全面塗装ではなく配線・latch・笑顔印へ限定
- [x] K11を別体化し、weapon rootとmuzzle socketを設定
- [x] Generic rig、標準bone、skin、必須13 clipsをGLBへ収録
- [x] Field表示とcollisionを分離
- [x] 歩行面、危険、操作物を色と形の両方で識別
- [x] 旧／新比較previewと通常距離previewを作成
- [x] 寸法、原点、材質、色、collision、取込契約を記録
- [ ] 技術責任者がGLB直取込またはFBX変換方式を正式確定

アセット単体所見: **組込み試験へ進めてよい。完成画面への承認ではない。**

## B. 完成アプリ必須ゲート

### K1外観

- [ ] 通常プレイ距離で1秒以内にK1と判別できる
- [ ] 頭部visor、胸部plate、黒関節、橙lineが潰れず、旧箱型modelに見えない
- [ ] 正面／側面／反転時にsilhouetteが不自然に細くならない
- [ ] K11が手から浮かず、胸・腕へ完全に埋まらない
- [ ] left hand contactとmuzzle位置が正しい

### Motion

- [ ] A/D入力で左右の足・腕・接地・重心移動が読める
- [ ] Idle、移動、反転、Jump、Jet、Shoot間のtransitionが途切れない
- [ ] 方向転換が減速 → 約0.30秒turn → 再加速となる
- [ ] 4.2 m/s時の0.80秒walk cycleで目立つfoot slidingがない
- [ ] 装甲plateがゴム状に変形せず、肘・膝・足首が機械らしく曲がる
- [ ] Jet時に胸の前傾と左右非対称thrusterが読める

### Field / 2.5D可読性

- [ ] 1秒以内に歩ける床、奥行きlane、背景、危険、敵、操作物を区別できる
- [ ] 歩行面が最も明るく連続し、背景の白面がK1／敵silhouetteを消さない
- [ ] dangerは赤だけでなく床欠損・三角歯・lipで判別できる
- [ ] interactiveは青だけでなく直立fork silhouetteで判別できる
- [ ] 前景railがK1、敵、projectile、reticleを隠さない
- [ ] 高架・煙突・夕景が敵sensor、攻撃予告、HUDより強くならない
- [ ] 通常カメラ距離でK1が小さすぎず、段差と敵の接地が見える

### 戦闘FX / HUD

- [ ] 弾、muzzle flash、hit、defeatが同じ単色線に見えず瞬時に区別できる
- [ ] 敵は暗灰の本体＋赤い三角sensorでK1と混同しない
- [ ] 残敵数は右上、勝利は `MISSION CLEAR`、被弾表示は操作guideと競合しない
- [ ] HUDと背景のcontrastが16:9通常画面で保たれる

### 機能維持

- [ ] 移動、jump、Jet、shoot、enemy defeat、`MISSION CLEAR`、restartが成立
- [ ] Windows standaloneで起動し、再現手順が共有されている

## C. 承認記録

| Gate | 状態 | 備考 |
|---|---|---|
| Asset design | READY FOR INTEGRATION | V2 previewとmanifest確認済み |
| Rig / import technical | PENDING | 技術責任者確認待ち |
| Completed app visual | PENDING | 実アプリ未確認 |
| Technical approval | PENDING | 技術責任者 |
| Proofreader QA | PENDING | 新V2 build対象 |
| Producer integration | PENDING | 上記完了後 |

最終デザイン承認欄: `________________`  
承認日: `________________`

