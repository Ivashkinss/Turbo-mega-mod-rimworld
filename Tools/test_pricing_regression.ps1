param([Parameter(Mandatory=$true)][string]$PricingProfile,[int]$TimeoutSeconds=300,[switch]$Resume,[switch]$ContinuePassed,[string]$PriorHotelProfile)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$production=Join-Path $projectPath 'Assemblies/TurboMegaMod.dll'
$hash=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$progress=Join-Path $projectPath 'TestRuns/PricingRegressionProgress.json'
$state=[ordered]@{started=[DateTime]::UtcNow.ToString('o');productionAssemblySha256=$hash;current=$null;completed=@();passed=$false;error=$null}
$lastProfile=(Resolve-Path -LiteralPath $PricingProfile).Path;$resumeCount=0;$resumeIndex=0
$fresh=Get-Content -LiteralPath (Join-Path $lastProfile "Result.json") -Raw|ConvertFrom-Json
if(!$fresh.passed -or $fresh.productionAssemblySha256 -ne $hash -or !$lastProfile.StartsWith((Join-Path $projectPath "TestRuns"))){throw "A passing same-binary isolated pricing profile is required"}
$state.pricingProfile=$lastProfile
if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'A RimWorld process is already running'}
if($Resume -or $ContinuePassed){
 $previous=Get-Content -LiteralPath $progress -Raw|ConvertFrom-Json
 if($previous.productionAssemblySha256 -ne $hash){throw 'Continuation requires same DLL'}
 if($Resume -and ($previous.passed -or !$previous.error)){throw 'Resume requires a failed run'}
 if($ContinuePassed -and !$previous.passed){throw 'ContinuePassed requires successful prior subset'}
 $state.started=$previous.started;$state.completed=@($previous.completed);$resumeCount=$state.completed.Count
}
function Save-Progress {
 $json=$state|ConvertTo-Json -Depth 8
 for($attempt=0;$attempt -lt 20;$attempt++){
  try{$json|Set-Content -LiteralPath $progress -Encoding utf8;return}
  catch [IO.IOException]{if($attempt -eq 19){throw};Start-Sleep -Milliseconds 50}
 }
}
function Step([string]$name,[string]$script,[hashtable]$parameters){
 if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Pricing production binary changed'}
 if($script:resumeIndex -lt $resumeCount){
  $prior=$state.completed[$script:resumeIndex];if($prior.name -ne $name){throw 'Saved step order changed'}
  $file=if($name.EndsWith(' ingest reload')){'IngestReloadResult.json'}elseif($name.EndsWith(' migration')){'MigrationResult.json'}elseif($name.EndsWith(' reload')){'ReloadResult.json'}else{'Result.json'}
  $report=Get-Content -LiteralPath (Join-Path $prior.profile $file) -Raw|ConvertFrom-Json
  if(!$report.passed -or $report.productionAssemblySha256 -ne $hash){throw 'Resumed result is stale or failed'}
  $script:lastProfile=$prior.profile;$script:resumeIndex++;Write-Output ('Reusing '+$name);return
 }
 $state.current=$name;Save-Progress;Write-Output ('Starting '+$name);$watch=[Diagnostics.Stopwatch]::StartNew()
 $output=& (Join-Path $PSScriptRoot $script) @parameters|Out-String
 Add-Content -LiteralPath (Join-Path $projectPath 'TestRuns/PricingRegression.log') -Value $output -Encoding utf8
 $match=[regex]::Match($output,'(?m)^TestRoot:\s*(.+)\s*$');if($match.Success){$script:lastProfile=$match.Groups[1].Value.Trim()}
 if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Pricing binary changed during test'}
 $state.completed+=@{name=$name;profile=$script:lastProfile;seconds=[Math]::Round($watch.Elapsed.TotalSeconds,1)};Save-Progress;Write-Output ('Passed '+$name)
}
try{
 Step 'Pricing reload' 'test_pricing_reload.ps1' @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
 if(!$PriorHotelProfile){
  $old=Get-Content -LiteralPath (Join-Path $projectPath 'TestRuns/GeneticSupplyRegressionProgress.json') -Raw|ConvertFrom-Json
  $PriorHotelProfile=($old.completed|Where-Object name -eq 'Hospitality').profile
 }
 Step 'Hotel prior-save migration' 'test_pricing_hotel_migration.ps1' @{Profile=$PriorHotelProfile;TimeoutSeconds=$TimeoutSeconds}
 foreach($component in @('Hospitality','Sales','SalesDelivery','Wholesale','ComplexLayout','Deathrest')){
  Step $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
  $script=switch($component){'Hospitality'{'test_hotel_reload.ps1'} 'Sales'{'test_sales_reload.ps1'} 'SalesDelivery'{'test_sales_delivery_reload.ps1'} 'Wholesale'{'test_wholesale_reload.ps1'} 'ComplexLayout'{'test_complex_layout_reload.ps1'} 'Deathrest'{'test_deathrest_reload.ps1'}}
  Step ($component+' reload') $script @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
 }
 foreach($component in @('Vip','GeneticSupply','Hemogen')){Step $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}}
 Step 'ClinicGuest' 'test_component.ps1' @{Component='ClinicGuest';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
 Step 'ClinicGuest reload' 'test_clinic_guest_reload.ps1' @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
 $state.current=$null;$state.passed=$true;Save-Progress;'Targeted pricing regression passed'
}catch{$state.error=$_.ToString();Save-Progress;throw}
