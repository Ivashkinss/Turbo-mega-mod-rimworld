param([int]$TimeoutSeconds=300,[switch]$Resume)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$production=Join-Path $projectPath 'Assemblies/TurboMegaMod.dll'
$hash=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$progress=Join-Path $projectPath 'TestRuns/DossierRevisionProgress.json'
$state=[ordered]@{started=[DateTime]::UtcNow.ToString('o');productionAssemblySha256=$hash;current=$null;completed=@();passed=$false;error=$null}
$lastProfile=$null;$resumeCount=0;$resumeIndex=0
if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'A RimWorld process is already running'}
if($Resume){
 $previous=Get-Content -LiteralPath $progress -Raw|ConvertFrom-Json
 if($previous.productionAssemblySha256 -ne $hash -or $previous.passed -or !$previous.error){throw 'Resume requires a failed run on the same DLL'}
 $state.started=$previous.started;$state.completed=@($previous.completed);$resumeCount=$state.completed.Count
}
function Save-Progress {$state|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $progress -Encoding utf8}
function Step([string]$name,[string]$script,[hashtable]$parameters){
 if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Dossier production binary changed'}
 if($script:resumeIndex -lt $resumeCount){
  $prior=$state.completed[$script:resumeIndex];if($prior.name -ne $name){throw 'Saved step order changed'}
  $file=if($name.EndsWith(' reload')){'ReloadResult.json'}else{'Result.json'}
  $report=Get-Content -LiteralPath (Join-Path $prior.profile $file) -Raw|ConvertFrom-Json
  if(!$report.passed -or $report.productionAssemblySha256 -ne $hash){throw 'Resumed result is stale or failed'}
  $script:lastProfile=$prior.profile;$script:resumeIndex++;Write-Output ('Reusing '+$name);return
 }
 $state.current=$name;Save-Progress;Write-Output ('Starting '+$name);$watch=[Diagnostics.Stopwatch]::StartNew()
 $output=& (Join-Path $PSScriptRoot $script) @parameters|Out-String
 Add-Content -LiteralPath (Join-Path $projectPath 'TestRuns/DossierRevision.log') -Value $output -Encoding utf8
 $match=[regex]::Match($output,'(?m)^TestRoot:\s*(.+)\s*$');if($match.Success){$script:lastProfile=$match.Groups[1].Value.Trim()}
 if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Dossier binary changed during test'}
 $state.completed+=@{name=$name;profile=$script:lastProfile;seconds=[Math]::Round($watch.Elapsed.TotalSeconds,1)};Save-Progress;Write-Output ('Passed '+$name)
}
try{
 foreach($component in @('ComplexLayout','Farming','Dossier','Hospitality','Restaurant','Company','Clinic','ClinicGuest','Sales','SalesDelivery','Wholesale','Complex')){
  Step $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
  $reload=switch($component){'Dossier'{'test_dossier_reload.ps1'} 'Hospitality'{'test_hotel_reload.ps1'} 'Restaurant'{'test_restaurant_reload.ps1'} 'Company'{'test_company_reload.ps1'} 'Clinic'{'test_clinic_reload.ps1'} 'ClinicGuest'{'test_clinic_guest_reload.ps1'} 'Sales'{'test_sales_reload.ps1'} 'SalesDelivery'{'test_sales_delivery_reload.ps1'} 'Wholesale'{'test_wholesale_reload.ps1'} 'Complex'{'test_complex_reload.ps1'} 'Farming'{'test_farming_reload.ps1'} 'ComplexLayout'{'test_complex_layout_reload.ps1'}}
  Step ($component+' reload') $reload @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
 }
 $state.current=$null;$state.passed=$true;Save-Progress;'Targeted dossier revision passed'
}catch{$state.error=$_.ToString();Save-Progress;throw}
