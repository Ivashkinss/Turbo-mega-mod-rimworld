$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testBuildPath = Join-Path $PSScriptRoot 'Build'
New-Item -ItemType Directory -Path $testBuildPath -Force | Out-Null
$runnerPath = Join-Path $testBuildPath 'FridgeLayoutTests.exe'
& $compilerPath /nologo /target:exe /optimize+ (('/out:') + $runnerPath) (Join-Path $projectPath 'Source\FridgeItemGraphicWorker.cs') (Join-Path $projectPath 'Tests\FridgeLayoutTests.cs')
if ($LASTEXITCODE -ne 0) { throw "Test compile failed: $LASTEXITCODE" }
$result = & $runnerPath
if ($LASTEXITCODE -ne 0) { throw "Fridge layout tests failed: $LASTEXITCODE" }
$report = $result | ConvertFrom-Json
$report | Add-Member -NotePropertyName sourceSha256 -NotePropertyValue ((Get-FileHash -LiteralPath (Join-Path $projectPath 'Source\FridgeItemGraphicWorker.cs') -Algorithm SHA256).Hash.ToLowerInvariant())
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\FridgeLayoutTests.json') -Encoding utf8
Write-Output $result
