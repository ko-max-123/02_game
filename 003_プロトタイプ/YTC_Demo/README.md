# YTC Movement Prototype

`02_タスク.txt`の移動確認用Unityプロトタイプ。正式技術方針のUnity／URP／C#で、山田・K1を中央工業帯デモフィールド内で操作する。

## 実装範囲

- `A` / `D`：横方向の主移動
- `W` / `S`：Z=-2.5～2.5mの限定奥行きレーン移動
- `Space`短押し：ジャンプ
- `Space`長押し：0.18秒後にジェットを開始し、上昇・滞空を補助
- ジェットエネルギー：最大100、使用中28/秒消費、停止0.65秒後から22/秒回復
- 2.5Dカメラ：Orthographic、X/Yだけ追従、カメラZ=-16m固定
- 地面判定：`CharacterController.isGrounded`と足元Sphere判定を併用し、Player本体・子階層Colliderは除外
- 落下復帰：Y=-8m未満で開始位置へ自動復帰
- 手動復帰：`Backspace`
- 操作ガイド：状態、レーンZ、JET、ENERGY、採用アセットを常時表示

本タスクでは戦闘、セーブ、オンライン、装備、ストーリーイベントを実装しない。

## 正式デザインアセット

EditorセットアップはUnityプロジェクト外の次の正式配置を参照する。

```text
003_プロトタイプ/DesignAssets/
├─ Models/yamada_k1_demo.obj
├─ Models/central_industrial_belt_demo.obj
├─ Models/central_industrial_belt_collision.obj
└─ Materials/ytc_design_assets.mtl
```

- 1 unit=1m、Y-up、Z-forwardとして読み込む。
- `central_industrial_belt_demo.obj`は表示専用。
- `central_industrial_belt_collision.obj`はRendererを無効化し、MeshCollider専用にする。
- セットアップ時に`Assets/YTCPrototype/ImportedDesignAssets/`へ同じ相対構造で同期する。元の`DesignAssets`は変更・削除しない。
- アセットが未配置の場合は、移動・ジャンプ・飛行を検証できるPrimitiveへ自動フォールバックする。

## Unity Editorでの一操作セットアップ

1. Unity Hubからこの`YTC_Demo`フォルダを開く。
2. Package Managerの処理とスクリプトコンパイル完了を待つ。
3. メニュー`YTC Prototype > Setup or Refresh Movement Demo`を実行する。
4. `Assets/YTCPrototype/Scenes/YTC_Demo.unity`を開き、Playを押す。

メニューはURP Asset、デモシーン、カメラ、ライト、CharacterController、HUD、アセット同期、Build Settings登録をまとめて行う。既存ファイルの削除APIは使用しない。

## コマンドラインセットアップ

Unity Editorのパスを環境に合わせて指定する。

```powershell
Unity.exe -batchmode `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -executeMethod YTCPrototype.Editor.PrototypeSceneBuilder.BuildFromCommandLine `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\setup.log"
```

## テスト

Unityを使わない静的確認：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Validate-Prototype.ps1
```

Unity EditModeテスト：

```powershell
Unity.exe -batchmode -quit `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -runTests -testPlatform EditMode `
  -testResults "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\editmode-results.xml" `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\editmode.log"
```

EditModeテストは次を検証する。

- 斜め入力で速度が増えない
- W/Sが狭いZレーンを越えない
- ジャンプ速度
- Space長押し・空中・エネルギーの飛行条件
- エネルギーの消費、回復、0下限
- プレイヤーZが変化してもカメラZが変化しない
- 落下復帰の閾値
- 生成シーンが正式モデル、衝突専用OBJ、固定奥行きカメラ、HUDを採用している
- 離床後の地面判定が自身のCharacterControllerを拾わず、Space長押しJET条件へ移行できる

最終確認結果は静的検証41/41 PASS、Unity EditMode 13/13 PASS。

検証環境はローカル導入済みのUnity 6000.5.8f1／URP 17.5.0。正式条件であるUnity LTS Editorでの再保存・再テスト、キーボードによる目視Play、Playerビルドは次工程で行う。

## 実装上の判断

- 物理移動はRigidbodyではなくCharacterControllerへ統一した。入力と速度を直接検証しやすく、2.5Dレーン制約を明示できるため。
- 1フレームの移動量は`Time.deltaTime`で計算し、30fps／60fpsで速度が変わらない。
- カメラはプレイヤーのZを追従しない。W/Sは見た目と回避幅を作る限定レーンで、横方向の可読性を崩さない。
- 正式デザインアセットと操作ルートを分離した。モデル側ColliderやRigidbodyは無効化し、プレイヤー本体のCharacterControllerだけを物理の正とする。
- ジェットはP0用の暫定消費値だが、操作条件とHUD表示は制作正典のエネルギー共有へ接続できる構造にした。
