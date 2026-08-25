$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$prototypeRoot = Split-Path -Parent $projectRoot
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$passes = [System.Collections.Generic.List[string]]::new()

function Assert-FileExists {
    param([string]$RelativePath)
    $fullPath = Join-Path $projectRoot $RelativePath
    if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
        $passes.Add("file: $RelativePath")
    }
    else {
        $errors.Add("missing file: $RelativePath")
    }
}

function Assert-Contains {
    param(
        [string]$RelativePath,
        [string]$Pattern,
        [string]$Label
    )
    $fullPath = Join-Path $projectRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $errors.Add("cannot inspect missing file: $RelativePath")
        return
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -match $Pattern) {
        $passes.Add($Label)
    }
    else {
        $errors.Add("missing contract: $Label")
    }
}

$requiredFiles = @(
    "Packages\manifest.json",
    "ProjectSettings\ProjectVersion.txt",
    "Assets\YTCPrototype\Runtime\PrototypeMovementMath.cs",
    "Assets\YTCPrototype\Runtime\PrototypePlayerController.cs",
    "Assets\YTCPrototype\Runtime\FixedDepthCamera.cs",
    "Assets\YTCPrototype\Runtime\PrototypeGuideOverlay.cs",
    "Assets\YTCPrototype\Runtime\PrototypeCombatMath.cs",
    "Assets\YTCPrototype\Runtime\PrototypeCombatDirector.cs",
    "Assets\YTCPrototype\Runtime\PrototypePlayerCombat.cs",
    "Assets\YTCPrototype\Runtime\PrototypePlayerHealth.cs",
    "Assets\YTCPrototype\Runtime\PrototypeEnemy.cs",
    "Assets\YTCPrototype\Runtime\PrototypeShotTracer.cs",
    "Assets\YTCPrototype\Editor\PrototypeSceneBuilder.cs",
    "Assets\YTCPrototype\Editor\PrototypeWindowsBuilder.cs",
    "Assets\YTCPrototype\Tests\EditMode\PrototypeMovementMathTests.cs",
    "Assets\YTCPrototype\Tests\EditMode\PrototypeSceneContractTests.cs",
    "Assets\YTCPrototype\Tests\EditMode\PrototypeCombatMathTests.cs",
    "Assets\YTCPrototype\Tests\PlayMode\PrototypeCombatPlayModeTests.cs"
)

foreach ($requiredFile in $requiredFiles) {
    Assert-FileExists $requiredFile
}

$manifestPath = Join-Path $projectRoot "Packages\manifest.json"
if (Test-Path -LiteralPath $manifestPath) {
    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($null -ne $manifest.dependencies."com.unity.render-pipelines.universal") {
            $passes.Add("URP package dependency")
        }
        else {
            $errors.Add("URP package dependency is missing")
        }

        if ($null -ne $manifest.dependencies."com.unity.test-framework") {
            $passes.Add("Unity Test Framework dependency")
        }
        else {
            $errors.Add("Unity Test Framework dependency is missing")
        }
    }
    catch {
        $errors.Add("Packages/manifest.json is invalid JSON: $($_.Exception.Message)")
    }
}

$controller = "Assets\YTCPrototype\Runtime\PrototypePlayerController.cs"
$camera = "Assets\YTCPrototype\Runtime\FixedDepthCamera.cs"
$guide = "Assets\YTCPrototype\Runtime\PrototypeGuideOverlay.cs"
$builder = "Assets\YTCPrototype\Editor\PrototypeSceneBuilder.cs"
$tests = "Assets\YTCPrototype\Tests\EditMode\PrototypeMovementMathTests.cs"
$sceneTests = "Assets\YTCPrototype\Tests\EditMode\PrototypeSceneContractTests.cs"
$combat = "Assets\YTCPrototype\Runtime\PrototypePlayerCombat.cs"
$health = "Assets\YTCPrototype\Runtime\PrototypePlayerHealth.cs"
$enemy = "Assets\YTCPrototype\Runtime\PrototypeEnemy.cs"
$director = "Assets\YTCPrototype\Runtime\PrototypeCombatDirector.cs"
$tracer = "Assets\YTCPrototype\Runtime\PrototypeShotTracer.cs"
$windowsBuilder = "Assets\YTCPrototype\Editor\PrototypeWindowsBuilder.cs"
$playModeTests = "Assets\YTCPrototype\Tests\PlayMode\PrototypeCombatPlayModeTests.cs"

Assert-Contains $controller 'ReadAxis\(KeyCode\.A, KeyCode\.D\)' "A/D primary horizontal movement"
Assert-Contains $controller 'ReadAxis\(KeyCode\.S, KeyCode\.W\)' "W/S limited depth-lane input"
Assert-Contains $controller 'minimumDepth\s*=\s*-2\.5f' "lane minimum Z=-2.5"
Assert-Contains $controller 'maximumDepth\s*=\s*2\.5f' "lane maximum Z=2.5"
Assert-Contains $controller 'KeyCode\.Space' "Space jump/hold input"
Assert-Contains $controller 'flightHoldDelay\s*=\s*0\.18f' "short-press/long-hold threshold"
Assert-Contains $controller 'maximumJetEnergy\s*=\s*100f' "jet energy budget"
Assert-Contains $controller 'KeyCode\.Backspace' "manual reset input"
Assert-Contains $controller 'Physics\.OverlapSphereNonAlloc' "ground probe"
Assert-Contains $controller '!hit\.transform\.IsChildOf\(transform\)' "ground probe excludes player collider"
Assert-Contains $camera 'CalculateFixedDepthCameraPosition' "fixed-depth camera calculation"
Assert-Contains $guide 'JET:' "HUD jet state"
Assert-Contains $guide 'ENERGY' "HUD energy state"
Assert-Contains $builder 'yamada_k1_demo\.obj' "official Yamada/K1 asset path"
Assert-Contains $builder 'central_industrial_belt_demo\.obj' "official field visual path"
Assert-Contains $builder 'central_industrial_belt_collision\.obj' "official collision-only asset path"
Assert-Contains $builder 'renderer\.enabled\s*=\s*false' "collision OBJ hidden from rendering"
Assert-Contains $builder 'AddComponent<MeshCollider>' "collision OBJ MeshCollider conversion"
Assert-Contains $tests 'DepthMovement_IsClampedToNarrowLane' "lane clamp unit test"
Assert-Contains $tests 'Flight_RequiresHoldDelayAirborneStateAndEnergy' "flight gating unit test"
Assert-Contains $tests 'FixedDepthCamera_IgnoresTargetDepth' "camera depth unit test"
Assert-Contains $sceneTests 'Player_UsesOfficialVisualAndNarrowDepthLane' "generated player scene contract test"
Assert-Contains $sceneTests 'Field_UsesOfficialVisualAndHiddenCollisionMesh' "generated field scene contract test"
Assert-Contains $sceneTests 'CameraAndHud_EnforceSideViewContract' "generated camera/HUD scene contract test"
Assert-Contains $sceneTests 'AirborneGroundProbe_ExcludesSelfAndAllowsHeldJet' "airborne self-collider exclusion/JET scene contract test"
Assert-Contains $controller 'FacingDirection' "player facing direction contract"
Assert-Contains $combat 'GetMouseButton\(0\)' "left-click shooting input"
Assert-Contains $combat 'KeyCode\.J' "J shooting input"
Assert-Contains $combat 'Physics\.RaycastAll' "aim-direction hit detection"
Assert-Contains $health 'TakeDamage' "player damage/respawn health contract"
Assert-Contains $enemy 'maximumHealth\s*=\s*50f' "enemy HP contract"
Assert-Contains $enemy 'attackTelegraphDuration\s*=\s*0\.32f' "enemy pre-fire telegraph"
Assert-Contains $enemy 'defeatDisplayDuration\s*=\s*0\.32f' "readable non-gore defeat delay"
Assert-Contains $director 'KeyCode\.R' "battle restart input"
Assert-Contains $director 'KeyCode\.Escape' "standalone quit input"
Assert-Contains $director 'ytc-smoke-test' "direct EXE smoke-test exit path"
Assert-Contains $guide 'MISSION CLEAR' "victory HUD contract"
Assert-Contains $guide '残敵' "top-right remaining enemy HUD contract"
Assert-Contains $tracer 'SpawnPlayerShot' "white-core/orange-tail player tracer"
Assert-Contains $tracer 'SpawnEnemyShot' "red-core enemy tracer"
Assert-Contains $tracer 'SpawnTelegraph' "enemy warning tracer"
Assert-Contains $builder 'EnemySensorTriangle' "angular red enemy sensor"
Assert-Contains $windowsBuilder 'BuildTarget\.StandaloneWindows64' "Windows x64 player build target"
Assert-Contains $windowsBuilder 'YTC_StandalonePrototype' "standalone distribution output"
Assert-Contains $windowsBuilder 'defaultScreenWidth\s*=\s*1920' "1920 default standalone width"
Assert-Contains $windowsBuilder 'defaultScreenHeight\s*=\s*1080' "1080 default standalone height"
Assert-Contains $playModeTests 'ShotDamageDefeatVictoryAndRespawn_CompleteCombatLoop' "combat loop PlayMode test"

$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $projectRoot "Assets\YTCPrototype") -Recurse -File -Include *.cs
$destructivePatterns = 'File\.Delete|Directory\.Delete|AssetDatabase\.DeleteAsset|DestroyImmediate'
$destructiveHits = $sourceFiles | Select-String -Pattern $destructivePatterns
if ($destructiveHits) {
    foreach ($hit in $destructiveHits) {
        $errors.Add("destructive API found: $($hit.Path):$($hit.LineNumber)")
    }
}
else {
    $passes.Add("no delete/destructive API in prototype C#")
}

$expectedDesignAssets = @(
    "DesignAssets\Models\yamada_k1_demo.obj",
    "DesignAssets\Models\central_industrial_belt_demo.obj",
    "DesignAssets\Models\central_industrial_belt_collision.obj",
    "DesignAssets\Materials\ytc_design_assets.mtl"
)

foreach ($asset in $expectedDesignAssets) {
    $assetPath = Join-Path $prototypeRoot $asset
    if (Test-Path -LiteralPath $assetPath -PathType Leaf) {
        $passes.Add("design asset: $asset")
    }
    else {
        $warnings.Add("design asset absent; Primitive fallback will be used: $asset")
    }
}

Write-Host "YTC prototype static validation"
Write-Host "PASS: $($passes.Count)"
foreach ($pass in $passes) {
    Write-Host "  [PASS] $pass"
}

Write-Host "WARN: $($warnings.Count)"
foreach ($warning in $warnings) {
    Write-Warning $warning
}

Write-Host "ERROR: $($errors.Count)"
foreach ($validationError in $errors) {
    Write-Error $validationError -ErrorAction Continue
}

if ($errors.Count -gt 0) {
    exit 1
}

exit 0
