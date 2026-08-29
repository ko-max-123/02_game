$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$prototypeRoot = Split-Path -Parent $projectRoot
$buildRoot = Join-Path $prototypeRoot "YTC_StandalonePrototype_V2"

$required = @(
    "YTC_CombatDemo_V2.exe",
    "YTC_CombatDemo_V2_Data",
    "UnityPlayer.dll",
    "MonoBleedingEdge",
    "BUILD_INFO.txt",
    "README.txt"
)

$passed = 0
foreach ($relative in $required) {
    $path = Join-Path $buildRoot $relative
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing V2 standalone artifact: $path"
    }
    $passed++
}

$buildInfo = Get-Content -LiteralPath (Join-Path $buildRoot "BUILD_INFO.txt") -Raw
if ($buildInfo -notmatch "glTFast direct GLB") {
    throw "BUILD_INFO.txt does not record the V2 direct GLB import mode."
}
$passed++

$readme = Get-Content -LiteralPath (Join-Path $buildRoot "README.txt") -Raw
if ($readme -notmatch "YTC_CombatDemo_V2.exe") {
    throw "README.txt does not contain the V2 executable launch instruction."
}
$passed++

Write-Output "V2 standalone validation: $passed/$passed PASS"
