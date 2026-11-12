$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$output=Join-Path $PSScriptRoot 'Build\FarmingPowerBudgetTests.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:exe /optimize+ ('/out:'+$output) (Join-Path $projectPath 'Source\FarmingPowerBudget.cs') (Join-Path $projectPath 'Tests\FarmingPowerBudgetTests.cs')
if($LASTEXITCODE -ne 0){throw 'Power budget test compile failed'}
$result=& $output
if($LASTEXITCODE -ne 0){throw 'Power budget tests failed'}
$result | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\FarmingPowerTests.json') -Encoding utf8
$result
