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
$sourceText = [IO.File]::ReadAllText((Join-Path $projectPath 'Source\FridgeItemGraphicWorker.cs')).Replace("`r`n", "`n")
$sourceBytes = [Text.Encoding]::UTF8.GetBytes($sourceText)
$sha256 = [Security.Cryptography.SHA256]::Create()
try { $sourceHash = [BitConverter]::ToString($sha256.ComputeHash($sourceBytes)).Replace('-', '').ToLowerInvariant() }
finally { $sha256.Dispose() }
$report | Add-Member -NotePropertyName sourceSha256 -NotePropertyValue $sourceHash
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\FridgeLayoutTests.json') -Encoding utf8
Write-Output $result
