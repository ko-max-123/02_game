# YTC 山田フィールド移動プロトタイプ

`000_エージェント共有読み物/02_タスク.txt` に基づく、山田の3Dモデルでデモフィールドを移動するためのUnityプロトタイプです。

## 実行環境

- Unity `6000.5.8f1`
- Universal Render Pipeline `17.5.0`
- C#

## 起動方法

1. Unity Hubでこの `YTC_FieldPrototype` フォルダを開く。
2. 初回インポート完了後、`Assets/Scenes/Prototype.unity` を開く。
3. Playを押す。

初回起動時にシーンとURP設定を生成し、リポジトリ内の正式デザイン資産をResourcesへ自動同期します。

## 操作

- `A` / `D`: フィールド全域の主移動軸（X）
- `W` / `S`: 対応する待機区画だけの短い奥行き移動（Z）
- `Space`: ジャンプ
- `Backspace`: スポーン地点へ戻る（プロトタイプのデバッグ専用）

`R` は正典のリロード入力用に予約し、この移動プロトタイプでは使用しません。

## デザイン素材

| 素材 | 正式ソース | Resourcesパス |
|---|---|---|
| 山田3Dモデル | `002_スライド/assets/3d_movement_prototype_v1/yamada_k1_prototype_v1.obj` | `Characters/Yamada/Yamada` |
| デモフィールド | `002_スライド/assets/3d_movement_prototype_v1/central_belt_stage01_demo_field_v1.obj` | `Environment/DemoField/DemoField` |

- 1 Unity unit = 1mを基準にする。
- フィールド床面は原点付近の `Y=0` を基準にする。
- 表示用の山田K1モデルは正式仕様の全高2.30mを維持する。
- モデル側Colliderは無効化し、移動判定には表示寸法と分離した高さ1.86m、半径0.38m、中心Y=0.93mのCharacterControllerを使用する。
- 表示メッシュと当たり判定を分離し、正式 `COLLISION_*` 寸法の軽量コライダーを使用する。

Editorセットアップが正式OBJ／MTLをプロジェクトのResourcesへコピーします。Unity標準OBJではノード境界が結合されるため、表示メッシュには当たり判定を付けず、正式 `COLLISION_*` ノードの寸法に対応するBoxCollider群と坂コライダーを実行時に構築します。同期先は生成物としてGit管理対象外です。正式ソースが見つからない場合だけ、カプセルと簡易フィールドへフォールバックします。

## 2.5D座標契約

- モデル正面は正式資産と同じ `+Z`。カメラをプレイヤーの `+Z` 側に置き、`-Z` 方向を見ることで正面を表示する。
- カメラオフセットは正式推奨値 `(0, 3.8, 10.5)`。OrthographicでプレイヤーのX/Yを追従し、カメラZは初期レーン中央に固定する。
- W/Sを許可する待機区画はX=`-14.0〜-9.2`、`1.7〜5.3`、`10.1〜14.0`m。それ以外の障害物・ギャップ・段差・坂区間では奥行き入力を無効化する。
- プレイヤーの奥行きは全区間で `Z=-0.65〜0.65`mにClampし、障害物の横素通りを防ぐ。
- 開始位置は正式推奨座標 `(-13.7, 0, 0)` とする。CharacterControllerは接地安定化用の0.05m上方から生成し、開始直後に床面へ接地する。

## テスト

EditModeテストで、斜め入力の正規化、ジャンプ初速、重力計算、奥行き移動区画を検証します。Unity Editor内のPlayMode自動テストでは、正式な山田／フィールド資産のロード、表示全高2.30mと当たり判定高1.86mの分離、接地、移動、奥行き区画／Clamp、0.6m障害物、2mギャップ、0.2／0.4／0.6m段差、坂の通過をCharacterControllerで検証します。さらにカメラのOrthographic、推奨オフセット、固定Z、X/Y追従、`-Z` 視線をassertします。

この証跡はEditor PlayMode自動検証であり、手動キーボード操作、Playerビルド、実機での検証結果ではありません。

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe' `
  -batchmode -projectPath . -runTests -testPlatform EditMode `
  -testResults EditModeResults.xml

& 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe' `
  -batchmode -projectPath . -runTests -testPlatform PlayMode `
  -testResults PlayModeResults.xml
```

## 現在の制約

- この成果物の範囲は移動、ジャンプ、追従カメラ、素材差し替え確認まで。
- 戦闘、アニメーション制御、飛行、ゲームパッド入力は後続実装。
- 手動プレイとPlayerビルドの受け入れ検証は後続実施。
