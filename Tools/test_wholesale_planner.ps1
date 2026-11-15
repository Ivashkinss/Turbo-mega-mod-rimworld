$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$buildPath=Join-Path $PSScriptRoot 'Build'
[void](New-Item -ItemType Directory -Path $buildPath -Force)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$program=Join-Path $buildPath 'WholesalePackingTests.exe'
$planner=Join-Path $projectPath 'Source\Wholesale\PackingPlanner.cs'
$test=Join-Path $projectPath 'Tests\WholesalePackingTests.cs'
& $compiler /noconfig /nologo /target:exe /optimize+ /reference:System.dll /reference:System.Core.dll ('/out:'+$program) $planner $test
if($LASTEXITCODE -ne 0){throw 'Wholesale packing tests failed to compile.'}
$output=& $program | Out-String
if($LASTEXITCODE -ne 0){throw 'Wholesale packing tests failed.'}
$result=$output | ConvertFrom-Json
if(!$result.passed){throw 'Wholesale packing test report failed.'}
$report=[ordered]@{passed=$true;checks=$result.checks;scope=$result.scope;linkedIntoGameAssembly=$false;runtimeWholesaleImplemented=$false;
    plannerSha256=(Get-FileHash -LiteralPath $planner -Algorithm SHA256).Hash.ToLowerInvariant();
    testSha256=(Get-FileHash -LiteralPath $test -Algorithm SHA256).Hash.ToLowerInvariant()}
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\WholesalePackingTests.json') -Encoding utf8
$report | ConvertTo-Json
