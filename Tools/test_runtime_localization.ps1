$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$harmonyPath = Join-Path $gamePath 'Mods\2009463077\1.6\Assemblies\0Harmony.dll'
if (!(Test-Path -LiteralPath $harmonyPath)) {
    $harmonyPath = Join-Path $gamePath 'Mods\2009463077\Current\Assemblies\0Harmony.dll'
}
$testBuildPath = Join-Path $PSScriptRoot 'Build'
New-Item -ItemType Directory -Path $testBuildPath -Force | Out-Null
# The assembly name matches a target so production assembly filtering is exercised.
$runnerPath = Join-Path $testBuildPath 'DynamicTradeInterface.exe'
$testHarmonyPath = Join-Path $testBuildPath '0Harmony.dll'
# A fresh test copy avoids the downloaded file's zone marker without modifying it.
[IO.File]::WriteAllBytes($testHarmonyPath, [IO.File]::ReadAllBytes($harmonyPath))
& $compilerPath /nologo /target:exe /optimize+ (('/reference:') + $harmonyPath) (('/out:') + $runnerPath) (Join-Path $projectPath 'Source\RuntimeLocalization.cs') (Join-Path $projectPath 'Source\RuntimeTranslationKeys.Generated.cs') (Join-Path $projectPath 'Source\MeleeCompatibility.cs') (Join-Path $projectPath 'Source\HugsLibCompatibility.cs') (Join-Path $projectPath 'Source\MeleeResourceCompatibility.cs') (Join-Path $projectPath 'Source\CharacterEditorCompatibility.cs') (Join-Path $projectPath 'Tests\RuntimeLocalizationTests.cs')
if ($LASTEXITCODE -ne 0) { throw "Runtime test compile failed: $LASTEXITCODE" }
$result = & $runnerPath $testHarmonyPath (Join-Path $projectPath 'Languages\Russian\Keyed\RuntimeUI.xml')
if ($LASTEXITCODE -ne 0) { throw "Runtime localization tests failed: $LASTEXITCODE" }
$result | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\RuntimeLocalizationTests.json') -Encoding utf8
Write-Output $result
