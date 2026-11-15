param([int]$TimeoutSeconds=240, [switch]$Resume)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$runs=Join-Path $projectPath 'TestRuns'
$progress=Join-Path $runs 'ServiceRevisionProgress.json'
$log=Join-Path $runs 'ServiceRevision.log'
$stop=Join-Path $runs 'ServiceRevision.stop'
$production=Join-Path $projectPath 'Assemblies\TurboMegaMod.dll'
$hash=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$state=[ordered]@{started=[DateTime]::UtcNow.ToString('o');productionAssemblySha256=$hash;current=$null;completed=@();passed=$false;error=$null}
$lastProfile=$null
$resumeCount=0
$resumeIndex=0
if($Resume) {
    $previous=Get-Content -LiteralPath $progress -Raw | ConvertFrom-Json
    if($previous.productionAssemblySha256 -ne $hash){throw 'Cannot resume tests from another production DLL.'}
    if($previous.passed -or !$previous.error){throw 'Resume requires an interrupted or failed regression.'}
    if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'A RimWorld process is still running.'}
    $state.started=$previous.started;$state.completed=@($previous.completed)
    $resumeCount=$state.completed.Count
}
function Save-Progress { $state|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $progress -Encoding utf8 }
function Step([string]$name,[string]$script,[hashtable]$parameters) {
    if(Test-Path -LiteralPath $stop){throw 'Cooperative stop requested before next test.'}
    if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Production DLL changed during regression.'}
    if($script:resumeIndex -lt $resumeCount) {
        $prior=$state.completed[$script:resumeIndex]
        if($prior.name -ne $name){throw 'Saved regression step order does not match this runner.'}
        $script:lastProfile=$prior.profile;$script:resumeIndex++
        Write-Output ('Reusing passed step '+$name);return
    }
    $state.current=$name;Save-Progress;Write-Output ('Starting '+$name)
    $watch=[Diagnostics.Stopwatch]::StartNew()
    $result= & (Join-Path $PSScriptRoot $script) @parameters | Out-String
    Add-Content -LiteralPath $log -Value $result -Encoding utf8
    $match=[regex]::Match($result,'(?m)^TestRoot:\s*(.+)\s*$')
    if($match.Success){$script:lastProfile=$match.Groups[1].Value.Trim()}
    if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Production DLL changed during test.'}
    $state.completed+=@{name=$name;seconds=[Math]::Round($watch.Elapsed.TotalSeconds,1);profile=$script:lastProfile}
    Save-Progress;Write-Output ('Passed '+$name)
}
try {
    Step 'Recruit' 'test_component.ps1' @{Component='Recruit';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
    Step 'Recruit reload' 'test_recruit_reload.ps1' @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
    Step 'Sales' 'test_component.ps1' @{Component='Sales';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
    Step 'Sales reload' 'test_sales_reload.ps1' @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
    Step 'Farming' 'test_component.ps1' @{Component='Farming';FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
    Step 'Farming reload' 'test_farming_reload.ps1' @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
    Step 'Core libraries' 'test_component.ps1' @{Component='Harmony';FullSet=$true;UseBuiltAssembly=$true;WithCoreRegression=$true;TimeoutSeconds=$TimeoutSeconds}
    Step 'Melee and hands' 'test_component.ps1' @{Component='MeleeAnimation';FullSet=$true;UseBuiltAssembly=$true;WithHands=$true;TimeoutSeconds=$TimeoutSeconds}
    Step 'HugsLib and RunAndGun' 'test_component.ps1' @{Component='HugsLib';FullSet=$true;UseBuiltAssembly=$true;WithRunAndGun=$true;TimeoutSeconds=$TimeoutSeconds}
    foreach($component in @('Hospitality','Restaurant','Complex','Clinic','ClinicGuest','ComplexLayout','Company')) {
        Step $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
        $reload=switch($component){'Hospitality'{'test_hotel_reload.ps1'} 'Restaurant'{'test_restaurant_reload.ps1'} 'Complex'{'test_complex_reload.ps1'} 'Clinic'{'test_clinic_reload.ps1'} 'ClinicGuest'{'test_clinic_guest_reload.ps1'} 'ComplexLayout'{'test_complex_layout_reload.ps1'} 'Company'{'test_company_reload.ps1'}}
        Step ($component+' reload') $reload @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
    }
    $state.current=$null;$state.passed=$true;Save-Progress;Write-Output 'All service revision regressions passed.'
} catch {
    $state.error=$_.ToString();Save-Progress;throw
}
