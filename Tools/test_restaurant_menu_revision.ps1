param([string]$MenuProfile,[string]$DeliveryProfile,[string]$RestaurantProfile,[string]$SalesProfile,[string]$WholesaleProfile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$production=Join-Path $root 'Assemblies/TurboMegaMod.dll'
$digest=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$progress=Join-Path $root 'TestRuns/MenuRevisionProgress.json'
$state=[ordered]@{productionAssemblySha256=$digest;completed=@();passed=$false;error=$null}
function Save-Progress {$state|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $progress -Encoding utf8}
function Passed([string]$profile,[string]$file) {
    if(!$profile){return $false}
    $resolved=(Resolve-Path -LiteralPath $profile).Path
    if(!$resolved.StartsWith((Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Use isolated TestRuns only.'}
    $path=Join-Path $resolved $file
    if(!(Test-Path -LiteralPath $path)){return $false}
    $r=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
    return $r.passed -and $r.productionAssemblySha256 -eq $digest -and $r.runtimeErrors -eq 0 -and !$r.gpuLost
}
function Fresh([string]$component,[string]$profile) {
    if(!(Passed $profile 'Result.json')){
        $output=& (Join-Path $PSScriptRoot 'test_component.ps1') -Component $component -FullSet -UseBuiltAssembly -TimeoutSeconds $TimeoutSeconds|Out-String
        if($output -notmatch 'TestRoot:\s*([^\r\n]+)'){throw 'Missing isolated profile in test output.'}
        $profile=$Matches[1].Trim()
    }
    Record $component $profile 'Result.json'
    return $profile
}
function Record([string]$name,[string]$profile,[string]$file) {
    if(!(Passed $profile $file)){throw ($name+' lacks a clean result on the frozen DLL.')}
    if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $digest){throw 'Production DLL changed during revision.'}
    $state.completed+=@{name=$name;profile=(Resolve-Path -LiteralPath $profile).Path;file=$file};Save-Progress
    Write-Host ('Passed '+$name)
}
function Reload([string]$name,[string]$profile,[string]$script) {
    if(!(Passed $profile 'ReloadResult.json')){& (Join-Path $PSScriptRoot $script) -Profile $profile -TimeoutSeconds $TimeoutSeconds|Out-Null}
    Record $name $profile 'ReloadResult.json'
}
try {
    $MenuProfile=Fresh 'RestaurantMenu' $MenuProfile;Reload 'menu reload' $MenuProfile 'test_restaurant_menu_reload.ps1'
    $DeliveryProfile=Fresh 'SalesDelivery' $DeliveryProfile;Reload 'delivery reload' $DeliveryProfile 'test_sales_delivery_reload.ps1'
    $RestaurantProfile=Fresh 'Restaurant' $RestaurantProfile;Reload 'restaurant reload' $RestaurantProfile 'test_restaurant_reload.ps1'
    $SalesProfile=Fresh 'Sales' $SalesProfile
    $WholesaleProfile=Fresh 'Wholesale' $WholesaleProfile
    $state.passed=$true;Save-Progress
}catch{$state.error=$_.Exception.Message;Save-Progress;throw}
