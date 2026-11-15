param()
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$source=Join-Path $projectPath 'Source/MedicalAllocation.cs'
$test=Join-Path $projectPath 'Tests/MedicalAllocationTests.cs'
$cases=Join-Path $projectPath 'Tests/Data/ClinicReceiptAllocationCases.json'
$program=Join-Path $PSScriptRoot 'Build/MedicalAllocationTests.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe /optimize+ /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll ('/out:'+$program) $source $test
if($LASTEXITCODE -ne 0){throw 'Medical allocation test compilation failed'}
$output=& $program $cases | Out-String
if($LASTEXITCODE -ne 0){throw 'Medical allocation tests failed'}
$result=$output|ConvertFrom-Json
$result|Add-Member sourceSha256 (Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant()
$result|Add-Member fixtureSha256 (Get-FileHash $cases -Algorithm SHA256).Hash.ToLowerInvariant()
$result|ConvertTo-Json|Set-Content (Join-Path $projectPath 'Documentation/MedicalAllocationTests.json') -Encoding utf8
$result|ConvertTo-Json
