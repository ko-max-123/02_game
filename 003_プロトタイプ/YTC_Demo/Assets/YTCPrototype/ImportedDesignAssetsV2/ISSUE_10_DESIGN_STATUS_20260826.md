# Issue #10 デザイン進捗

- Issue: `#10 【最優先/P0 V2】K1モデル・リグアニメーション・中央産業帯の改善`
- 担当: 役割2 デザインマネージャー
- 更新日: 2026-08-26
- 状態: `ACTIVE / TECHNICAL INTEGRATION WAIT`

## 結論

DesignAssets_V2の技術引渡しと取込条件回答は完了した。完成Windowsアプリの目視とデザイン最終承認は未完であり、Issueを完了扱いにしない。

## 実施

1. `yamada_k1_rigged_v2.glb` をK1外観・skin・skeleton・13 clipsの唯一の正本と確定。
2. segmented OBJは静的監査／変換fallback、K11 GLBとField表示GLBは各正本と回答。
3. bone／clip／socket／material名、尺、loop、event時刻、scale／axis、rigid weightを不変契約として技術側へ回答。
4. `MuzzleSocket` はK11 `WeaponRoot` 配下を正と回答。
5. Field visual GLBとcollision OBJを同一原点／scale／axisの別objectで統合する条件を回答。
6. 技術側GLB監査PASSを受領。
7. 技術一次決定「Unity glTFastでGLB直取込、FBX変換なし」へ合意。

## 依存

- 技術責任者: Unity実取込検証、Animator／IK／状態遷移、Field統合、Windows V2 build。
- デザインマネージャー: 技術返却物と完成アプリを通常プレイ距離で確認し、`DESIGN_FINAL_APPROVAL_CHECKLIST.md` を判定。
- 校正者: 専門承認後の独立QA。

## 技術側から必要な返却物

1. Import後のroot／scale／rig／hierarchy／clip／material確認画像。
2. 通常プレイ距離の静止画。
3. Idle → Walk → Turn180 → Jump → Jet → Shootの連続capture。
4. 足元とfoot slidingが確認できるcapture。
5. K11 grip、left-hand contact、muzzle位置が確認できるcapture。
6. Field、K1、敵、危険、操作物、HUDが同時に見えるcapture。
7. Windows standalone V2のpathと起動手順。

## 未解決／阻害事項

- glTFast取込後の実階層、animation、PBR材質再現は未確認。
- K1の手足、重心、方向転換、foot slidingは完成アプリ未確認。
- Fieldの通常カメラ可読性とHUD競合は完成アプリ未確認。
- Windows standalone V2未受領。

総合デザイン承認: **PENDING**

