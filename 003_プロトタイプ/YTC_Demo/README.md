# YTC Standalone Combat Prototype

山田・K1を中央工業帯で操作し、移動、JET、射撃、敵撃破、勝利、被弾・リスポーンを確認する短編戦闘プロトタイプ。Windows配布版はUnity Editor／Unity HubがないPCでも直接起動できる。

## V2技術統合（Issue #10）

- 正本`DesignAssets_V2` はUnity glTFast 6.19.0でGLBを直接取り込む。FBX変換は行わない。
- K1はMecanim / Generic rig / in-place / root motion無効で、13 clipを同名のAnimator stateへ割り当てる。
- `WeaponSocket_R`にK11を接続し、射撃始点はK11内の`MuzzleSocket`とする。
- 中央産業帯V2の表示GLBとcollision OBJは同一原点／同一scaleで配置し、collision側Rendererは無効化する。
- V1は`YTC_Demo.unity`と`YTC_StandalonePrototype/`に保存し、V2は別シーン`YTC_Demo_V2.unity`と別出力`YTC_StandalonePrototype_V2/`を使う。

V2 Windows版の生成：

```powershell
Unity.exe -batchmode -quit `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -executeMethod YTCPrototype.Editor.PrototypeV2WindowsBuilder.BuildFromCommandLine `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\v2-windows-build.log"
```

V2配布時は`YTC_StandalonePrototype_V2`フォルダ一式を保ったまま`YTC_CombatDemo_V2.exe`を起動する。

V2配布物の検査：

```powershell
pwsh -NoProfile -File .\Tools\Validate-V2StandaloneBuild.ps1
```

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
- `左クリック`：マウス照準方向へ射撃
- `J`：K1が向いている方向へ射撃
- 敵：3体、HP50、簡易巡回、0.32秒の赤い予告線後に反撃
- 敵撃破：HP0で撃破火花、センサー消灯、0.32秒沈下後に非表示
- プレイヤー：HP100、被弾方向表示、HP0から0.8秒後にリスポーン
- `R`：敵、勝利表示、Player状態を含めて戦闘を再読み込み
- `Esc`：アプリ終了
- HUD：左下HP、右上残敵、照準、JET、被弾方向、中央`MISSION CLEAR`

セーブ、オンライン、装備変更、ドロップ、シナリオイベントは本タスクでは実装しない。

## Windows版を直接起動する

配布物は隣接フォルダ`003_プロトタイプ/YTC_StandalonePrototype/`にある。

1. `YTC_StandalonePrototype`フォルダ一式を同じ構成のままWindows PCへコピーする。
2. `YTC_CombatDemo.exe`をダブルクリックする。
3. Unity Editor、Unity Hub、追加ランタイムの導入は不要。

`YTC_CombatDemo.exe`だけを単独で移動してはならない。`YTC_CombatDemo_Data/`、`UnityPlayer.dll`、`MonoBleedingEdge/`などを含むフォルダ一式が配布単位となる。

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
3. メニュー`YTC Prototype > Setup or Refresh Combat Demo`を実行する。
4. `Assets/YTCPrototype/Scenes/YTC_Demo.unity`を開き、Playを押す。

メニューはURP Asset、デモシーン、カメラ、ライト、Player、敵、戦闘HUD、アセット同期、Build Settings登録をまとめて行う。`YTC Prototype > Build Windows Standalone`で1920×1080・Windowed・ResizableのWindows x64 Playerを生成する。既存ファイルの削除APIは使用しない。

## コマンドラインセットアップ

Unity Editorのパスを環境に合わせて指定する。

```powershell
Unity.exe -batchmode `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -executeMethod YTCPrototype.Editor.PrototypeSceneBuilder.BuildFromCommandLine `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\setup.log"
```

Windows版ビルド：

```powershell
Unity.exe -batchmode -quit `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -executeMethod YTCPrototype.Editor.PrototypeWindowsBuilder.BuildFromCommandLine `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\windows-build.log"
```

## テスト

Unityを使わない静的確認：

```powershell
pwsh -NoProfile -File .\Tools\Validate-Prototype.ps1
```

Unity EditModeテスト：

```powershell
Unity.exe -batchmode `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -runTests -testPlatform EditMode `
  -testResults "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\editmode-results.xml" `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\editmode.log"
```

Unity PlayMode戦闘ループテスト：

```powershell
Unity.exe -batchmode `
  -projectPath "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo" `
  -runTests -testPlatform PlayMode `
  -testResults "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\combat-playmode-results.xml" `
  -logFile "D:\05_codex\02_game\003_プロトタイプ\YTC_Demo\TestResults\combat-playmode.log"
```

Windows配布物確認：

```powershell
pwsh -NoProfile -File .\Tools\Validate-StandaloneBuild.ps1
```

両検証スクリプトはWindows PowerShell 5.1でも実行可能。配布物検証は文字コード依存を避けるため、ASCII名`README.txt`を必須確認し、日本語版`README_起動方法.txt`は利用者向けとして併置する。

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
- 左クリック／J射撃、敵HP、予告射撃、非流血撃破、勝利HUDの生成契約
- 実再生状態で射撃命中→HP低下→撃破→残敵0→勝利→リスタート全復元→被弾リスポーン

最終確認結果：

- 静的検証72/72 PASS
- Unity EditMode 19/19 PASS
- Unity PlayMode 1/1 PASS
- Windows x64ビルド終了コード0
- 配布物検証8/8 PASS
- 完成EXEの直接起動・2秒実行・正常終了コード0

検証環境はローカル導入済みのUnity 6000.5.8f1／URP 17.5.0。正式条件であるUnity LTS Editorでの再保存・再テストと、キーボード・マウスを使った目視プレイ調整は未実施。

## 実装上の判断

- 物理移動はRigidbodyではなくCharacterControllerへ統一した。入力と速度を直接検証しやすく、2.5Dレーン制約を明示できるため。
- 1フレームの移動量は`Time.deltaTime`で計算し、30fps／60fpsで速度が変わらない。
- カメラはプレイヤーのZを追従しない。W/Sは見た目と回避幅を作る限定レーンで、横方向の可読性を崩さない。
- 正式デザインアセットと操作ルートを分離した。モデル側ColliderやRigidbodyは無効化し、プレイヤー本体のCharacterControllerだけを物理の正とする。
- ジェットはP0用の暫定消費値だが、操作条件とHUD表示は制作正典のエネルギー共有へ接続できる構造にした。
- 射撃は短いヒットスキャンと二層LineRendererで実装した。味方は白芯＋橙尾、敵は赤芯＋暗色外縁として静止画でも区別する。
- 敵攻撃は射程10m以内に制限し、0.32秒の赤いセンサー点滅・予告線後にだけ発射する。カメラ表示幅内の攻撃となるため、P0では画面外マーカーを省略した。
- 敵撃破は流血を使わず、センサー消灯、低彩度化、撃破火花、短い沈下で表現する。
