$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$prototypeRoot = Split-Path -Parent $projectRoot
$buildRoot = Join-Path $prototypeRoot "YTC_StandalonePrototype"
$errors = [System.Collections.Generic.List[string]]::new()
$passes = [System.Collections.Generic.List[string]]::new()

function Assert-BuildFile {
    param([string]$RelativePath, [long]$MinimumBytes = 1)
    $path = Join-Path $buildRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("missing distribution file: $RelativePath")
        return
    }

    $length = (Get-Item -LiteralPath $path).Length
    if ($length -lt $MinimumBytes) {
        $errors.Add("distribution file is too small: $RelativePath ($length bytes)")
        return
    }

    $passes.Add("file: $RelativePath ($length bytes)")
}

function Assert-BuildDirectory {
    param([string]$RelativePath)
    $path = Join-Path $buildRoot $RelativePath
    if (Test-Path -LiteralPath $path -PathType Container) {
        $passes.Add("directory: $RelativePath")
    }
    else {
        $errors.Add("missing distribution directory: $RelativePath")
    }
}

Assert-BuildFile "YTC_CombatDemo.exe" 100000
Assert-BuildFile "UnityPlayer.dll" 100000
Assert-BuildFile "UnityCrashHandler64.exe" 100000
Assert-BuildFile "BUILD_INFO.txt" 20
Assert-BuildFile "README.txt" 20
Assert-BuildDirectory "YTC_CombatDemo_Data"

$dataRoot = Join-Path $buildRoot "YTC_CombatDemo_Data"
if (Test-Path -LiteralPath $dataRoot -PathType Container) {
    $dataFiles = Get-ChildItem -LiteralPath $dataRoot -Recurse -File
    if ($dataFiles.Count -gt 5) {
        $passes.Add("player data payload: $($dataFiles.Count) files")
    }
    else {
        $errors.Add("player data payload appears incomplete: $($dataFiles.Count) files")
    }
}

$editorLeak = Get-ChildItem -LiteralPath $buildRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match 'UnityEditor|UnityHub' }
if ($editorLeak) {
    foreach ($leak in $editorLeak) {
        $errors.Add("editor-only artifact in distribution: $($leak.FullName)")
    }
}
else {
    $passes.Add("no Unity Editor/Hub dependency in distribution")
}

Write-Host "YTC standalone build validation"
Write-Host "PASS: $($passes.Count)"
foreach ($pass in $passes) {
    Write-Host "  [PASS] $pass"
}

Write-Host "ERROR: $($errors.Count)"
foreach ($validationError in $errors) {
    Write-Error $validationError -ErrorAction Continue
}

if ($errors.Count -gt 0) {
    exit 1
}

exit 0
