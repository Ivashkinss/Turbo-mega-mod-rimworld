param([Parameter(Mandatory=$true)][string]$Profile, [int]$TimeoutSeconds=360, [string]$ProjectRoot,[switch]$CompatibilityReload)
$ErrorActionPreference='Stop'
if (!$ProjectRoot) {
    $parent=Split-Path -Parent $PSScriptRoot
    if (Test-Path -LiteralPath (Join-Path $parent 'AGENTS.md')) { $ProjectRoot=$parent }
    else { $ProjectRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path }
}
$projectPath=(Resolve-Path -LiteralPath $ProjectRoot).Path
$gamePath=Split-Path -Parent (Split-Path -Parent $projectPath)
$testRoot=(Resolve-Path -LiteralPath $Profile).Path
$allowed=(Join-Path $projectPath 'TestRuns')+[IO.Path]::DirectorySeparatorChar
if (!$testRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Use an isolated TestRuns profile only.' }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before running the isolated reload.' }
$production=Join-Path $projectPath 'Assemblies\TurboMegaMod.dll'
$assemblyTime=(Get-Item -LiteralPath $production).LastWriteTimeUtc
if (@(Get-ChildItem -LiteralPath (Join-Path $projectPath 'Source') -Recurse -Filter '*.cs' -File | Where-Object LastWriteTimeUtc -gt $assemblyTime).Count) { throw 'Sources changed after the production build; rebuild before reload.' }
$fresh=Get-Content -LiteralPath (Join-Path $testRoot 'OutpostTabProbe.json') -Raw | ConvertFrom-Json
$freshResult=Get-Content -LiteralPath (Join-Path $testRoot 'Result.json') -Raw | ConvertFrom-Json
$digest=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
if (!$freshResult.passed -or $freshResult.component -ne 'OutpostTab' -or $freshResult.runtimeErrors -ne 0 -or (!$CompatibilityReload -and $freshResult.productionAssemblySha256 -ne $digest)) { throw 'Require a clean fresh OutpostTab result on the installed production DLL.' }
if (!$fresh.passed -or !(Test-Path -LiteralPath (Join-Path $testRoot 'Saves\TMM-OutpostTabFlight.rws'))) { throw 'A passing fresh outpost tab fixture and its real save are required.' }
$probeAssembly=Join-Path $projectPath 'Tools\Build\OutpostTabProbe.dll'
if (!(Test-Path -LiteralPath $probeAssembly)) { throw 'Build the root OutpostTab FullSet probe first; it must include the render probe.' }
$installed=Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll'
if (Test-Path -LiteralPath $installed) { throw 'A test probe is already installed.' }
$process=$null; $installedOwnProbe=$false
try {
    foreach ($name in @('GpuMapProbe.json','OutpostTabReloadProbe.json')) {
        $path=Join-Path $testRoot $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Copy-Item -LiteralPath $probeAssembly -Destination $installed
    $installedOwnProbe=$true
    $process=Start-Process -FilePath (Join-Path $gamePath 'RimWorld.exe') -WorkingDirectory $gamePath -ArgumentList @(('"-savedatafolder='+$testRoot+'"'),'-tmm-outpost-tab-reload','-tmm-render-probe') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'reload-stdout.log') -RedirectStandardError (Join-Path $testRoot 'reload-stderr.log') -PassThru
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (!(Test-Path -LiteralPath (Join-Path $testRoot 'OutpostTabReloadProbe.json')) -or !(Test-Path -LiteralPath (Join-Path $testRoot 'GpuMapProbe.json'))) {
        if ($process.WaitForExit(1000)) { throw 'Game exited before reload probes completed.' }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Outpost tab reload timed out.' }
    }
    $probe=Get-Content -LiteralPath (Join-Path $testRoot 'OutpostTabReloadProbe.json') -Raw | ConvertFrom-Json
    $activity=Get-Content -LiteralPath (Join-Path $testRoot 'GpuMapProbe.json') -Raw | ConvertFrom-Json
    $log=Join-Path $testRoot 'reload-stdout.log'
    $runtimeErrors=@(Select-String -LiteralPath $log -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
    $gpuLost=[bool](Select-String -LiteralPath $log -SimpleMatch 'VK_ERROR_DEVICE_LOST')
    $brokenReferences=@(Select-String -LiteralPath $log -Pattern 'Could not find think node with key|Could not resolve cross-reference|Could not resolve reference to object with loadID|Could not find class|missing reference|Failed to load reference|Could not load reference')
    $result=[ordered]@{outpostTabReload=$probe;mapActivity=$activity;runtimeErrors=$runtimeErrors.Count;unresolvedReferences=$brokenReferences.Count;gpuLost=$gpuLost;isolatedFixtureSave=$true;userSaveTested=$false;compatibilityReload=[bool]$CompatibilityReload;freshAssemblySha256=$freshResult.productionAssemblySha256;productionAssemblySha256=$digest}
    $result.passed=$probe.passed -and $activity.framesAdvanced -gt 0 -and $activity.ticksAdvanced -gt 0 -and $runtimeErrors.Count -eq 0 -and $brokenReferences.Count -eq 0 -and !$gpuLost
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'ReloadResult.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    if (!$result.passed) { throw 'Outpost tab reload failed; inspect the isolated profile.' }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\OutpostTab-Reload.json') -Encoding utf8
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($installedOwnProbe -and (Test-Path -LiteralPath $installed)) { Remove-Item -LiteralPath $installed -Force }
}
