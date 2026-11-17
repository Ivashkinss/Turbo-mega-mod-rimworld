param([Parameter(Mandatory=$true)][string]$Profile,[Parameter(Mandatory=$true)][string]$LegacyContractProfile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$profile=(Resolve-Path -LiteralPath $Profile).Path
$legacy=(Resolve-Path -LiteralPath $LegacyContractProfile).Path
$hash=(Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash
$fresh=Get-Content (Join-Path $profile 'Result.json') -Raw|ConvertFrom-Json
if(!$fresh.passed -or $fresh.productionAssemblySha256 -ne $hash){throw 'Passing same-binary fresh ransom fixture required'}
function Run([string]$script,[hashtable]$params){
 if((Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash -ne $hash){throw 'Production DLL changed'}
 $out=& (Join-Path $PSScriptRoot $script) @params|Out-String
 $out|Add-Content (Join-Path $root 'TestRuns/FactionRansomRevision.log') -Encoding utf8
 $match=[regex]::Match($out,'(?m)^TestRoot:\s*(.+)\s*$');if($match.Success){$script:housekeepingProfile=$match.Groups[1].Value.Trim()}
}
Run 'test_faction_ransom_reload.ps1' @{Profile=$profile;TimeoutSeconds=$TimeoutSeconds}
Copy-Item -LiteralPath (Join-Path $profile 'ReloadResult.json') -Destination (Join-Path $profile 'PaymentReloadResult.json')
Run 'test_faction_ransom_reload.ps1' @{Profile=$profile;TimeoutSeconds=$TimeoutSeconds}
Copy-Item -LiteralPath (Join-Path $profile 'ReloadResult.json') -Destination (Join-Path $profile 'ReleaseReloadResult.json')
Run 'test_work_contract_reload.ps1' @{Profile=$legacy;TimeoutSeconds=$TimeoutSeconds}
Run 'test_component.ps1' @{Component='Housekeeping';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
Run 'test_housekeeping_reload.ps1' @{Profile=$housekeepingProfile;TimeoutSeconds=$TimeoutSeconds}
Write-Output ('Ransom profile: '+$profile)
Write-Output ('Old contract profile: '+$legacy)
Write-Output ('Housekeeping profile: '+$housekeepingProfile)
