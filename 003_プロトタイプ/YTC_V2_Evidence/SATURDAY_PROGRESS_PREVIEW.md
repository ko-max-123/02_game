# Issue #10 土曜日の進捗プレビュー（WIP）

この資料は、2026-08-29（土）の進捗提示用です。完成承認・独立QA・Issue Closeを意味しません。

## 提示物

- 対象実装コミット: `ca3b2713464c6096dd943ad2ecb22dc6d9d4d72a`
- 公開ブランチ: `issue-10-k1-v2`
- Windows実行ビルド: [`../YTC_StandalonePrototype_V2/`](../YTC_StandalonePrototype_V2/)
- 実行ファイル: `YTC_StandalonePrototype_V2/YTC_CombatDemo_V2.exe`
- 画像・取込証跡: このフォルダ内のPNG 10点と [`V2_IMPORT_REPORT.md`](V2_IMPORT_REPORT.md)

GitHubからブランチ全体をZIPでダウンロードして展開し、`YTC_StandalonePrototype_V2` フォルダを丸ごと保持してください。EXEだけを単独で移動すると起動できません。

## 起動方法

1. Windows x64 PCで、ダウンロードしたZIPを展開する。
2. `003_プロトタイプ/YTC_StandalonePrototype_V2/YTC_CombatDemo_V2.exe` をダブルクリックする。
3. Windowsの保護画面が表示された場合は、発行元未署名のWIPビルドであることを確認したうえで実行する。

Unity Editor、Unity Hub、追加ランタイムのインストールは不要です。

## 操作

| 入力 | 動作 |
|---|---|
| `A` / `D` | 左右移動 |
| `W` / `S` | 奥行きレーン移動 |
| `Space` | ジャンプ／長押しでジェット |
| 左クリック / `J` | 射撃 |
| `R` | リスタート |
| `Esc` | 終了 |

## 今回確認できる内容

- K1 V2正本GLBをglTFast 6.19.0で直接取込（FBX再変換なし）
- Genericリグ、in-place、root motion無効
- 13アニメーションクリップ
  - Idle、前後移動、奥行き移動、180度旋回
  - Jump Start／Loop／Land
  - Jet Start／Loop／End
  - Shoot Recoil
- Animator 2レイヤー構成（射撃を別レイヤーで再生）
- K11、WeaponSocket_R、MuzzleSocketの統合
- 中央産業帯V2表示モデルと専用衝突モデル
- 既存V1シーンと別のV2シーン／V2 Windowsビルド

## 画像の見方

- [`01_normal_gameplay_distance.png`](01_normal_gameplay_distance.png): 通常プレイ距離
- [`02_walk_foot_contact.png`](02_walk_foot_contact.png): 歩行時の足元
- [`03_k11_grip_and_muzzle.png`](03_k11_grip_and_muzzle.png): K11保持・銃口ソケット
- [`04_field_readability.png`](04_field_readability.png): 中央産業帯V2
- `sequence_01`〜`sequence_06`: Idle、歩行、旋回、ジャンプ、ジェット、射撃の状態サンプル

連続動画ではなく、Windows実行ビルドを最優先の提示物とし、PNGは確認箇所を補う証跡です。

## 検証結果（2026-08-27）

- Unity EditMode: 27 / 27 PASS
- Unity PlayMode: 2 / 2 PASS
- Windows V2ビルド検査: 8 / 8 PASS
- `YTC_CombatDemo_V2.exe` を直接起動し、5秒後もプロセス稼働を確認
- `*_BurstDebugInformation_DoNotShip` が公開対象にないことを確認

## 技術判断と既知の不足

- 取込方式は **glTFastによる正本GLB直取込** とし、FBX再変換は採用していません。
- 現段階ではランタイムIKを追加していません。足接地とK11保持は、正本GLBのベイク済みアニメーション、Genericリグ、ソケット、プレイヤーカプセルで構成しています。最終版で補助IKが必要かは、実機上のデザイン確認を受けて再判断します。
- 通常プレイ距離でのK1輪郭、背景とのコントラスト、フィールド可読性はデザイン承認前で、調整対象になり得ます。
- コード署名・インストーラーは未対応です。Windows x64向けの直接実行WIPビルドです。
- 30〜90秒の連続動画は未作成です。今回は実行ビルドと10点の画像を提示します。
- デザインマネージャーの提示可否判断、校正者の独立確認、プロデューサーの統合記録は未実施です。

## 未達ゲート

1. デザインマネージャーによる「提示可／条件付き提示可／提示不可」の目視判断
2. 校正者による起動・歩行・ジャンプ・連続動作・画面視認性の独立確認
3. プロデューサーによる「土曜日の進捗プレビュー」の確定
4. Issue #10本来の専門承認、独立QA、最終合格
