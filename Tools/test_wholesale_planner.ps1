param([string]$Cases)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
if(!$Cases){$Cases=Join-Path $projectPath 'Tests\Data\WholesalePackingCases.json'}
$buildPath=Join-Path $PSScriptRoot 'Build'
[void](New-Item -ItemType Directory -Path $buildPath -Force)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$program=Join-Path $buildPath 'WholesalePackingTests.exe'
$planner=Join-Path $projectPath 'Source\Wholesale\PackingPlanner.cs'
$test=Join-Path $projectPath 'Tests\WholesalePackingTests.cs'
$caseProbe=Join-Path $projectPath 'Tests\WholesalePackingCaseProbe.cs'
& $compiler /noconfig /nologo /target:exe /optimize+ /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll ('/out:'+$program) $planner $test $caseProbe
if($LASTEXITCODE -ne 0){throw 'Wholesale packing tests failed to compile.'}
$output=if($Cases){& $program (Resolve-Path -LiteralPath $Cases).Path | Out-String}else{& $program | Out-String}
if($LASTEXITCODE -ne 0){throw 'Wholesale packing tests failed.'}
$result=$output | ConvertFrom-Json
if(!$result.passed){throw 'Wholesale packing test report failed.'}
$production=Join-Path $projectPath 'Assemblies\TurboMegaMod.dll'
$h=[Reflection.Assembly]::LoadFrom((Join-Path $projectPath 'Components\Harmony\1.6\Assemblies\0Harmony.dll'))
$reader=$h.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly',[type[]]@([string]))
$assembly=$reader.Invoke($null,@([string]$production))
try{$linked=$null -ne $assembly.MainModule.GetType('TurboMegaMod.Wholesale.PackingPlanner')}finally{$assembly.Dispose()}
$report=[ordered]@{passed=$true;checks=$result.checks;scope=$result.scope;linkedIntoGameAssembly=$linked;runtimeWholesaleTestedByThisRunner=$false;
    plannerSha256=(Get-FileHash -LiteralPath $planner -Algorithm SHA256).Hash.ToLowerInvariant();
    testSha256=(Get-FileHash -LiteralPath $test -Algorithm SHA256).Hash.ToLowerInvariant();
    fixtureCases=$result.fixtureCases;fixtureAssertions=$result.fixtureAssertions;excludedCases=$result.excludedCases;
    fixtureAdapterSha256=(Get-FileHash -LiteralPath $caseProbe -Algorithm SHA256).Hash.ToLowerInvariant();
    fixtureSha256=$(if($Cases){(Get-FileHash -LiteralPath $Cases -Algorithm SHA256).Hash.ToLowerInvariant()}else{$null})}
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\WholesalePackingTests.json') -Encoding utf8
$report | ConvertTo-Json
