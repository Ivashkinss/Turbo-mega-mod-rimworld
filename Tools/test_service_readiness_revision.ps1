param([Parameter(Mandatory=$true)][string]$LegacyMenuProfile,[string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$digest=(Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
if(!$Profile){
    $output=& (Join-Path $PSScriptRoot 'test_component.ps1') -Component ServiceReadiness -FullSet -UseBuiltAssembly -TimeoutSeconds $TimeoutSeconds|Out-String
    if($output -notmatch 'TestRoot:\s*([^\r\n]+)'){throw 'Missing isolated profile in test output.'}
    $Profile=$Matches[1].Trim()
}
$resolved=(Resolve-Path -LiteralPath $Profile).Path
if(!$resolved.StartsWith((Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Use isolated TestRuns profiles only.'}
$fresh=Get-Content -LiteralPath (Join-Path $resolved 'Result.json') -Raw|ConvertFrom-Json
if($fresh.component -ne 'ServiceReadiness' -or !$fresh.passed -or $fresh.productionAssemblySha256 -ne $digest -or $fresh.runtimeErrors -ne 0){throw 'A clean fresh result on this exact DLL is required.'}
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component RestaurantMenu -UseBuiltAssembly -CompileProbeOnly|Out-Null
& (Join-Path $PSScriptRoot 'test_restaurant_menu_reload.ps1') -Profile $LegacyMenuProfile -TimeoutSeconds $TimeoutSeconds
if((Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant() -ne $digest){throw 'Production DLL changed during the revision.'}
Write-Output ('Readiness profile: '+$resolved)
Write-Output ('Legacy menu profile: '+(Resolve-Path -LiteralPath $LegacyMenuProfile).Path)
Write-Output 'Inspect the native screenshots, then run verify_service_readiness.py with both profiles.'
