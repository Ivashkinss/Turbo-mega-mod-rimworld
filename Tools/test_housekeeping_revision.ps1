param([Parameter(Mandatory=$true)][string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$hash=(Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant()
$state=[ordered]@{productionAssemblySha256=$hash;passed=$false;current=$null;completed=@();error=$null}
$progress=Join-Path $root 'TestRuns/HousekeepingProgress.json'
function Save-State { $s=$state|ConvertTo-Json -Depth 6;for($i=0;$i -lt 20;$i++){try{$s|Set-Content -LiteralPath $progress -Encoding utf8;return}catch [IO.IOException]{if($i -eq 19){throw};Start-Sleep -Milliseconds 50}} }
function Run([string]$name,[string]$script,[hashtable]$params){
 if((Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant() -ne $hash){throw 'Production DLL changed'}
 $state.current=$name;Save-State;Write-Output ('Starting '+$name)
 $out=& (Join-Path $PSScriptRoot $script) @params|Out-String
 $out|Add-Content (Join-Path $root 'TestRuns/HousekeepingRevision.log') -Encoding utf8
 $match=[regex]::Match($out,'(?m)^TestRoot:\s*(.+)\s*$');if($match.Success){$script:profile=$match.Groups[1].Value.Trim()}
 $state.completed+=@{name=$name;profile=$script:profile};Save-State;Write-Output ('Passed '+$name)
}
$profile=(Resolve-Path -LiteralPath $Profile).Path
$fresh=Get-Content -LiteralPath (Join-Path $profile 'Result.json') -Raw|ConvertFrom-Json
if(!$fresh.passed -or $fresh.productionAssemblySha256 -ne $hash -or !$profile.StartsWith((Join-Path $root 'TestRuns'))){throw 'A passing same-binary isolated housekeeping fixture is required'}
$state.completed+=@{name='Housekeeping';profile=$profile}
try{
 Run 'Housekeeping reload' 'test_housekeeping_reload.ps1' @{Profile=$profile;TimeoutSeconds=$TimeoutSeconds}
 Run 'Pricing' 'test_component.ps1' @{Component='Pricing';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
 Run 'Pricing reload' 'test_pricing_reload.ps1' @{Profile=$profile;TimeoutSeconds=$TimeoutSeconds}
 $state.current=$null;$state.passed=$true;Save-State
}catch{$state.error=$_.ToString();Save-State;throw}
