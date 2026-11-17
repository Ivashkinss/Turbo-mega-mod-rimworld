param([Parameter(Mandatory=$true)][string]$Profile, [int]$TimeoutSeconds=240)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$gamePath=Split-Path -Parent (Split-Path -Parent $projectPath)
$testRoot=(Resolve-Path -LiteralPath $Profile).Path
$allowed=(Join-Path $projectPath 'TestRuns')+[IO.Path]::DirectorySeparatorChar
if (!$testRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Use an isolated TestRuns profile only.' }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before running the isolated reload.' }
$assemblyTime=(Get-Item -LiteralPath (Join-Path $projectPath 'Assemblies\TurboMegaMod.dll')).LastWriteTimeUtc
 if (@(Get-ChildItem -LiteralPath @((Join-Path $projectPath 'Source'),(Join-Path $projectPath 'Source\Wholesale')) -Filter '*.cs' -File | Where-Object LastWriteTimeUtc -gt $assemblyTime).Count) { throw 'Sources changed after the production build; rebuild before reload.' }
$fresh=Get-Content -LiteralPath (Join-Path $testRoot 'RegionalWorldProbe.json') -Raw | ConvertFrom-Json
$freshResult=Get-Content -LiteralPath (Join-Path $testRoot 'Result.json') -Raw | ConvertFrom-Json
$digest=(Get-FileHash -LiteralPath (Join-Path $projectPath 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
if(!$freshResult.passed -or $freshResult.component -ne 'RegionalWorld' -or $freshResult.runtimeErrors -ne 0 -or $freshResult.productionAssemblySha256 -ne $digest){throw 'Require a clean fresh result on the installed production DLL.'}
if (!$fresh.passed -or !(Test-Path -LiteralPath (Join-Path $testRoot 'Saves\TMM-RegionalWorld.rws'))) { throw 'A passing fresh regionalWorld fixture and its save are required.' }
$installed=Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll'
if (Test-Path -LiteralPath $installed) { throw 'A test probe is already installed.' }
$process=$null
try {
    foreach ($name in @('GpuMapProbe.json','RegionalWorldReloadProbe.json')) {
        $path=Join-Path $testRoot $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build\RegionalWorldProbe.dll') -Destination $installed
    $process=Start-Process -FilePath (Join-Path $gamePath 'RimWorld.exe') -WorkingDirectory $gamePath -ArgumentList @(('"-savedatafolder='+$testRoot+'"'),'-tmm-regional-reload','-tmm-render-probe') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'reload-stdout.log') -RedirectStandardError (Join-Path $testRoot 'reload-stderr.log') -PassThru
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (!(Test-Path -LiteralPath (Join-Path $testRoot 'RegionalWorldReloadProbe.json')) -or !(Test-Path -LiteralPath (Join-Path $testRoot 'GpuMapProbe.json'))) {
        if ($process.WaitForExit(1000)) { throw 'Game exited before reload probes completed.' }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Reload probes timed out.' }
    }
    $probe=Get-Content -LiteralPath (Join-Path $testRoot 'RegionalWorldReloadProbe.json') -Raw | ConvertFrom-Json
    $activity=Get-Content -LiteralPath (Join-Path $testRoot 'GpuMapProbe.json') -Raw | ConvertFrom-Json
    $runtimeErrors=@(Select-String -LiteralPath (Join-Path $testRoot 'reload-stdout.log') -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
    $gpuLost=[bool](Select-String -LiteralPath (Join-Path $testRoot 'reload-stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')
    $brokenReferences=@(Select-String -LiteralPath (Join-Path $testRoot 'reload-stdout.log') -Pattern 'Could not find think node with key|Could not resolve cross-reference|Could not find class|missing reference|Failed to load reference|Could not load reference')
    $result=[ordered]@{regionalWorldReload=$probe; mapActivity=$activity; runtimeErrors=$runtimeErrors.Count; unresolvedReferences=$brokenReferences.Count; gpuLost=$gpuLost; isolatedFixtureSave=$true; userSaveTested=$false; productionAssemblySha256=(Get-FileHash -LiteralPath (Join-Path $projectPath 'Assemblies\TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant()}
    $result.passed=$probe.passed -and $activity.framesAdvanced -gt 0 -and $activity.ticksAdvanced -gt 0 -and $runtimeErrors.Count -eq 0 -and $brokenReferences.Count -eq 0 -and !$gpuLost
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'ReloadResult.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    if (!$result.passed) { throw 'RegionalWorld reload failed; inspect the isolated profile.' }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\RegionalWorld-Reload.json') -Encoding utf8
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if (Test-Path -LiteralPath $installed) { Remove-Item -LiteralPath $installed -Force }
}
