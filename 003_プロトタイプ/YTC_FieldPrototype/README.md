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

- `W` / `A` / `S` / `D`: フィールド移動
- `Space`: ジャンプ
- `R`: スポーン地点へ戻る

## デザイン素材

| 素材 | 正式ソース | Resourcesパス |
|---|---|---|
| 山田3Dモデル | `002_スライド/assets/3d_movement_prototype_v1/yamada_k1_prototype_v1.obj` | `Characters/Yamada/Yamada` |
| デモフィールド | `002_スライド/assets/3d_movement_prototype_v1/central_belt_stage01_demo_field_v1.obj` | `Environment/DemoField/DemoField` |

- 1 Unity unit = 1mを基準にする。
- フィールド床面は原点付近の `Y=0` を基準にする。
- 山田モデルは読み込み時に身長約1.8mへ自動正規化される。
- モデル側Colliderは無効化し、移動判定には親のCharacterControllerを使用する。
- フィールド側にColliderがない場合、MeshFilterごとにMeshColliderを自動付与する。

Editorセットアップが正式OBJ／MTLをプロジェクトのResourcesへコピーします。同期先は生成物としてGit管理対象外です。正式ソースが見つからない場合だけ、カプセルと簡易フィールドへフォールバックします。

カメラは2.5D確認用のOrthographicです。A/Dを主移動軸、W/Sを奥行き移動として扱い、カメラのZ位置は固定します。開始位置はデザイン指定の `(-13.7, 0, 0)` です。

## テスト

EditModeテストで、斜め入力の正規化、ジャンプ初速、重力計算を検証します。PlayModeテストでは、正式な山田／フィールド資産のロード、プロトタイプの自動生成、接地、移動、0.6m障害物と2mギャップのジャンプ通過を実際のCharacterControllerで検証します。

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
