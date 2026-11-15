param([int]$TimeoutSeconds=300,[string]$ClinicProfile,[switch]$Resume)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$production=Join-Path $projectPath 'Assemblies/TurboMegaMod.dll'
$hash=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$progress=Join-Path $projectPath 'TestRuns/ClinicAttributionProgress.json'
$state=[ordered]@{started=[DateTime]::UtcNow.ToString('o');productionAssemblySha256=$hash;current=$null;completed=@();passed=$false;error=$null}
$lastProfile=$null
$reuseCount=0;$reuseIndex=0
if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'A RimWorld process is still running'}
if($Resume){
 if($ClinicProfile){throw 'Choose either Resume or a seed clinic profile'}
 $previous=Get-Content -LiteralPath $progress -Raw|ConvertFrom-Json
 if($previous.passed -or !$previous.error -or $previous.productionAssemblySha256 -ne $hash){throw 'Resume requires a failed run on the same production DLL'}
 $state.started=$previous.started;$state.completed=@($previous.completed);$reuseCount=$state.completed.Count
}
function Save-Progress {$state|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $progress -Encoding utf8}
function Step([string]$name,[string]$script,[hashtable]$parameters){
 if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Production changed during medical regression'}
 if($script:reuseIndex -lt $reuseCount){
  $prior=$state.completed[$script:reuseIndex];if($prior.name -ne $name){throw 'Resume step order changed'}
  $file=if($name.EndsWith(' reload')){'ReloadResult.json'}else{'Result.json'}
  $report=Get-Content -LiteralPath (Join-Path $prior.profile $file) -Raw|ConvertFrom-Json
  if(!$report.passed -or $report.productionAssemblySha256 -ne $hash){throw 'A resumed result is failed or stale'}
  $script:lastProfile=$prior.profile;$script:reuseIndex++;Write-Output ('Reusing passed '+$name);return
 }
 $state.current=$name;Save-Progress;Write-Output ('Starting '+$name)
 $watch=[Diagnostics.Stopwatch]::StartNew()
 $output=& (Join-Path $PSScriptRoot $script) @parameters|Out-String
 Add-Content -LiteralPath (Join-Path $projectPath 'TestRuns/ClinicAttribution.log') -Value $output -Encoding utf8
 $match=[regex]::Match($output,'(?m)^TestRoot:\s*(.+)\s*$')
 if($match.Success){$script:lastProfile=$match.Groups[1].Value.Trim()}
 if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Production changed in medical test'}
 $state.completed+=@{name=$name;profile=$script:lastProfile;seconds=[Math]::Round($watch.Elapsed.TotalSeconds,1)}
 Save-Progress;Write-Output ('Passed '+$name)
}
try{
 if($ClinicProfile){
  $resolved=(Resolve-Path -LiteralPath $ClinicProfile).Path
  $allowed=(Join-Path $projectPath 'TestRuns')+[IO.Path]::DirectorySeparatorChar
  if(!$resolved.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Only an isolated local clinic fixture can seed this run'}
  $result=Get-Content -LiteralPath (Join-Path $resolved 'Result.json') -Raw|ConvertFrom-Json
  if(!$result.passed -or !$result.clinic.passed -or $result.productionAssemblySha256 -ne $hash){throw 'Seed clinic result is failed or from another DLL'}
  $lastProfile=$resolved;$state.completed+=@{name='Clinic';profile=$resolved;seconds=0;reusedVerifiedResult=$true};Save-Progress
 }else{Step 'Clinic' 'test_component.ps1' @{Component='Clinic';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}}
 Step 'Clinic reload' 'test_clinic_reload.ps1' @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
 foreach($component in @('ClinicGuest','Company')){
  Step $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
  $reload=if($component -eq 'ClinicGuest'){'test_clinic_guest_reload.ps1'}else{'test_company_reload.ps1'}
  Step ($component+' reload') $reload @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
 }
 $state.current=$null;$state.passed=$true;Save-Progress;'Targeted medical attribution tests passed'
}catch{$state.error=$_.ToString();Save-Progress;throw}
