param([int]$TimeoutSeconds=300, [switch]$Resume)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$production=Join-Path $projectPath 'Assemblies\TurboMegaMod.dll'
$hash=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$progress=Join-Path $projectPath 'TestRuns\WholesaleRevisionProgress.json'
$state=[ordered]@{started=[DateTime]::UtcNow.ToString('o');productionAssemblySha256=$hash;current=$null;completed=@();passed=$false;error=$null}
$lastProfile=$null
$resumeCount=0;$resumeIndex=0
if($Resume) {
    $previous=Get-Content -LiteralPath $progress -Raw | ConvertFrom-Json
    if($previous.productionAssemblySha256 -ne $hash -or $previous.passed -or !$previous.error){throw 'Resume requires a failed run on the same production DLL.'}
    if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'A RimWorld process is still running.'}
    $state.started=$previous.started;$state.completed=@($previous.completed);$resumeCount=$state.completed.Count
}
function Save-Progress {$state|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $progress -Encoding utf8}
function Step([string]$name,[string]$script,[hashtable]$parameters) {
    if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Production DLL changed during wholesale regression.'}
    if($script:resumeIndex -lt $resumeCount){
        $prior=$state.completed[$script:resumeIndex];if($prior.name -ne $name){throw 'Saved step order changed.'}
        $script:lastProfile=$prior.profile;$script:resumeIndex++;Write-Output ('Reusing passed step '+$name);return
    }
    $state.current=$name;Save-Progress;Write-Output ('Starting '+$name)
    $watch=[Diagnostics.Stopwatch]::StartNew()
    $result=& (Join-Path $PSScriptRoot $script) @parameters | Out-String
    Add-Content -LiteralPath (Join-Path $projectPath 'TestRuns\WholesaleRevision.log') -Value $result -Encoding utf8
    $match=[regex]::Match($result,'(?m)^TestRoot:\s*(.+)\s*$')
    if($match.Success){$script:lastProfile=$match.Groups[1].Value.Trim()}
    if((Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash){throw 'Production DLL changed during a wholesale test.'}
    $state.completed+=@{name=$name;seconds=[Math]::Round($watch.Elapsed.TotalSeconds,1);profile=$script:lastProfile}
    Save-Progress;Write-Output ('Passed '+$name)
}
try {
    foreach($component in @('Wholesale','Sales','SalesDelivery','Hospitality','Complex')) {
        Step $component 'test_component.ps1' @{Component=$component;FullSet=$true;UseBuiltAssembly=$true;TimeoutSeconds=$TimeoutSeconds}
        $reload=switch($component){'Wholesale'{'test_wholesale_reload.ps1'} 'Sales'{'test_sales_reload.ps1'} 'SalesDelivery'{'test_sales_delivery_reload.ps1'} 'Hospitality'{'test_hotel_reload.ps1'} 'Complex'{'test_complex_reload.ps1'}}
        Step ($component+' reload') $reload @{Profile=$lastProfile;TimeoutSeconds=$TimeoutSeconds}
    }
    $state.current=$null;$state.passed=$true;Save-Progress
    Write-Output 'All targeted wholesale regressions passed.'
} catch {$state.error=$_.ToString();Save-Progress;throw}
