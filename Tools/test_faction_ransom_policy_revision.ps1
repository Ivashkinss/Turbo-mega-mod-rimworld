param([Parameter(Mandatory=$true)][string]$LegacyProfile,[string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$digest=(Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
if(!$Profile){
    $output=& (Join-Path $PSScriptRoot 'test_component.ps1') -Component FactionRansomPolicy -FullSet -UseBuiltAssembly -TimeoutSeconds $TimeoutSeconds|Out-String
    if($output -notmatch 'TestRoot:\s*([^\r\n]+)'){throw 'Missing isolated policy profile.'}
    $Profile=$Matches[1].Trim()
}
$resolved=(Resolve-Path -LiteralPath $Profile).Path
if(!$resolved.StartsWith((Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Use isolated TestRuns profiles only.'}
$fresh=Get-Content -LiteralPath (Join-Path $resolved 'Result.json') -Raw|ConvertFrom-Json
if($fresh.component -ne 'FactionRansomPolicy' -or !$fresh.passed -or $fresh.productionAssemblySha256 -ne $digest){throw 'Need a clean fresh policy result on this exact DLL.'}
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component FactionRansomPolicy -CompileProbeOnly -UseBuiltAssembly|Out-Null
& (Join-Path $PSScriptRoot 'test_faction_ransom_policy_reload.ps1') -Profile $Profile -TimeoutSeconds $TimeoutSeconds
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component FactionRansom -CompileProbeOnly -UseBuiltAssembly|Out-Null
& (Join-Path $PSScriptRoot 'test_faction_ransom_reload.ps1') -Profile $LegacyProfile -TimeoutSeconds $TimeoutSeconds
if((Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant() -ne $digest){throw 'Production DLL changed during verification.'}
Write-Output 'Inspect the policy screenshot, then run verify_faction_ransom_policy.py with both profiles.'
