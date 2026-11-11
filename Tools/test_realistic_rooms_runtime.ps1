$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$modsPath = Split-Path -Parent $projectPath
$harmonyPath = & (Join-Path $PSScriptRoot 'get_harmony_reference.ps1')
$buildPath = Join-Path $PSScriptRoot 'Build'
New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
$copyPath = Join-Path $buildPath '0Harmony.dll'
[IO.File]::WriteAllBytes($copyPath, [IO.File]::ReadAllBytes($harmonyPath))
$runnerPath = Join-Path $buildPath 'RealisticRoomsCompatibilityTests.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:exe /optimize+ (('/reference:') + $harmonyPath) (('/out:') + $runnerPath) (Join-Path $projectPath 'Source\RealisticRoomsCompatibility.cs') (Join-Path $projectPath 'Tests\RealisticRoomsCompatibilityTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Rooms regression test compile failed.' }
$result = & $runnerPath $copyPath
if ($LASTEXITCODE -ne 0) { throw 'Rooms regression test failed.' }
$result | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\RealisticRoomsRuntime.json') -Encoding utf8
Write-Output $result
