param([Parameter(Mandatory=$true)][string]$LegacyVipProfile,[string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$digest=(Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
if(!$Profile){
    $output=& (Join-Path $PSScriptRoot 'test_component.ps1') -Component OpeningVisit -FullSet -UseBuiltAssembly -TimeoutSeconds $TimeoutSeconds|Out-String
    if($output -notmatch 'TestRoot:\s*([^\r\n]+)'){throw 'Missing isolated opening profile.'};$Profile=$Matches[1].Trim()
}
$resolved=(Resolve-Path -LiteralPath $Profile).Path
if(!$resolved.StartsWith((Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Use isolated TestRuns profiles.'}
$fresh=Get-Content -LiteralPath (Join-Path $resolved 'Result.json') -Raw|ConvertFrom-Json
if($fresh.component -ne 'OpeningVisit' -or !$fresh.passed -or $fresh.productionAssemblySha256 -ne $digest){throw 'Need a clean opening result on this DLL.'}
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component OpeningVisit -CompileProbeOnly -UseBuiltAssembly|Out-Null
& (Join-Path $PSScriptRoot 'test_opening_visit_reload.ps1') -Profile $Profile -TimeoutSeconds $TimeoutSeconds
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component Vip -CompileProbeOnly -UseBuiltAssembly|Out-Null
& (Join-Path $PSScriptRoot 'test_vip_reload.ps1') -Profile $LegacyVipProfile -TimeoutSeconds $TimeoutSeconds
if((Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant() -ne $digest){throw 'Production DLL changed during verification.'}
Write-Output 'Run verify_opening_visit.py with the opening and legacy VIP profiles.'
