param([int]$TimeoutSeconds=240)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$hash=(Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant()
$state=[ordered]@{productionAssemblySha256=$hash;passed=$false;current=$null;completed=@();error=$null}
$progress=Join-Path $root 'TestRuns/GuestSupplyProgress.json'
function Save-State { $s=$state|ConvertTo-Json -Depth 6;for($i=0;$i -lt 20;$i++){try{$s|Set-Content -LiteralPath $progress -Encoding utf8;return}catch [IO.IOException]{if($i -eq 19){throw};Start-Sleep -Milliseconds 50}} }
function Run([string]$name,[string]$script,[hashtable]$params){
 if((Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant() -ne $hash){throw 'Production DLL changed'}
 $state.current=$name;Save-State;Write-Output ('Starting '+$name)
 $out=& (Join-Path $PSScriptRoot $script) @params|Out-String
 $out|Add-Content (Join-Path $root 'TestRuns/GuestSupplyRevision.log') -Encoding utf8
 $match=[regex]::Match($out,'(?m)^TestRoot:\s*(.+)\s*$');if($match.Success){$script:profile=$match.Groups[1].Value.Trim()}
 $state.completed+=@{name=$name;profile=$script:profile};Save-State;Write-Output ('Passed '+$name)
}
try{
 Run 'Addiction' 'test_component.ps1' @{Component='Addiction';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
 Run 'Addiction reload' 'test_addiction_reload.ps1' @{Profile=$profile;TimeoutSeconds=$TimeoutSeconds}
 foreach($component in @('AreaCache','GeneticSupply','Hemogen','Pricing')){Run $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}}
 Run 'Pricing reload' 'test_pricing_reload.ps1' @{Profile=$profile;TimeoutSeconds=$TimeoutSeconds}
 $state.current=$null;$state.passed=$true;Save-State
}catch{$state.error=$_.ToString();Save-State;throw}
