param([Parameter(Mandatory=$true)][string]$Profile, [int]$TimeoutSeconds=900, [string]$ProjectRoot)
# Loads TMM-OutpostTier10.rws written by a passing Outposts reload (UpperLevels) and checks the
# saved level10 outpost, one actual native production day with eight workers, and deploy/recall.
$ErrorActionPreference='Stop'
if (!$ProjectRoot) { $ProjectRoot=Split-Path -Parent $PSScriptRoot }
$projectPath=(Resolve-Path -LiteralPath $ProjectRoot).Path
$gamePath=Split-Path -Parent (Split-Path -Parent $projectPath)
$testRoot=(Resolve-Path -LiteralPath $Profile).Path
$allowed=(Join-Path $projectPath 'TestRuns')+[IO.Path]::DirectorySeparatorChar
if (!$testRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Use an isolated TestRuns profile only.' }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before running the isolated reload.' }
$production=Join-Path $projectPath 'Assemblies\TurboMegaMod.dll'
$assemblyTime=(Get-Item -LiteralPath $production).LastWriteTimeUtc
if (@(Get-ChildItem -LiteralPath (Join-Path $projectPath 'Source') -Recurse -Filter '*.cs' -File | Where-Object LastWriteTimeUtc -gt $assemblyTime).Count) { throw 'Sources changed after the production build; rebuild before reload.' }
$digest=(Get-FileHash -LiteralPath $production -Algorithm SHA256).Hash.ToLowerInvariant()
$previous=Get-Content -LiteralPath (Join-Path $testRoot 'ReloadResult.json') -Raw | ConvertFrom-Json
if (!$previous.passed -or $previous.runtimeErrors -ne 0 -or $previous.productionAssemblySha256 -ne $digest) { throw 'Require a clean Outposts reload (which writes the level10 save) on the installed production DLL.' }
$save=Join-Path $testRoot 'Saves\TMM-OutpostTier10.rws'
if (!(Test-Path -LiteralPath $save)) { throw 'The level10 fixture save is missing.' }
$saveHash=(Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash.ToLowerInvariant()
$probeAssembly=Join-Path $projectPath 'Tools\Build\OutpostsProbe.dll'
if (!(Test-Path -LiteralPath $probeAssembly)) { throw 'Build the root Outposts probe first (test_component.ps1 -Component Outposts -CompileProbeOnly -UseBuiltAssembly).' }
$installed=Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll'
if (Test-Path -LiteralPath $installed) { throw 'A test probe is already installed.' }
$process=$null; $installedOwnProbe=$false
$stdout=Join-Path $testRoot 'tier10-stdout.log'
try {
    foreach ($name in @('GpuMapProbe.json','OutpostTier10ReloadProbe.json')) {
        $path=Join-Path $testRoot $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Copy-Item -LiteralPath $probeAssembly -Destination $installed
    $installedOwnProbe=$true
    $process=Start-Process -FilePath (Join-Path $gamePath 'RimWorld.exe') -WorkingDirectory $gamePath -ArgumentList @(('"-savedatafolder='+$testRoot+'"'),'-tmm-outpost-tier10-reload','-tmm-render-probe') -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError (Join-Path $testRoot 'tier10-stderr.log') -PassThru
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (!(Test-Path -LiteralPath (Join-Path $testRoot 'OutpostTier10ReloadProbe.json')) -or !(Test-Path -LiteralPath (Join-Path $testRoot 'GpuMapProbe.json'))) {
        if ($process.WaitForExit(1000)) { throw 'Game exited before level10 reload probes completed.' }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Level10 reload timed out.' }
    }
    $probe=Get-Content -LiteralPath (Join-Path $testRoot 'OutpostTier10ReloadProbe.json') -Raw | ConvertFrom-Json
    $activity=Get-Content -LiteralPath (Join-Path $testRoot 'GpuMapProbe.json') -Raw | ConvertFrom-Json
    $runtimeErrors=@(Select-String -LiteralPath $stdout -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
    $gpuLost=[bool](Select-String -LiteralPath $stdout -SimpleMatch 'VK_ERROR_DEVICE_LOST')
    $brokenReferences=@(Select-String -LiteralPath $stdout -Pattern 'Could not find think node with key|Could not resolve cross-reference|Could not resolve reference to object with loadID|Could not find class|missing reference|Failed to load reference|Could not load reference')
    $result=[ordered]@{outpostsTier10Reload=$probe;mapActivity=$activity;runtimeErrors=$runtimeErrors.Count;unresolvedReferences=$brokenReferences.Count;gpuLost=$gpuLost;isolatedFixtureSave=$true;userSaveTested=$false;fixtureSave='TMM-OutpostTier10.rws';fixtureSaveSha256=$saveHash;fixtureWriterAssemblySha256=$previous.productionAssemblySha256;productionAssemblySha256=$digest}
    $result.passed=$probe.passed -and $activity.framesAdvanced -gt 0 -and $activity.ticksAdvanced -gt 0 -and $runtimeErrors.Count -eq 0 -and $brokenReferences.Count -eq 0 -and !$gpuLost
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'Tier10ReloadResult.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    if (!$result.passed) { throw 'Level10 reload failed; inspect the isolated profile.' }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\Outposts-Tier10-Reload.json') -Encoding utf8
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($installedOwnProbe -and (Test-Path -LiteralPath $installed)) { Remove-Item -LiteralPath $installed -Force }
}
