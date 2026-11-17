param([Parameter(Mandatory=$true)][string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$game=Split-Path -Parent (Split-Path -Parent $root)
$source=(Resolve-Path -LiteralPath $Profile).Path
$allowed=(Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar
if(!$source.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Use an isolated TestRuns checkpoint.'}
if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'Close RimWorld before the isolated checkpoint test.'}
$save=Join-Path $source 'Saves/TMM-OutpostDay.rws'
$original=(Get-FileHash -LiteralPath $save).Hash
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component Outposts -FullSet -UseBuiltAssembly -CompileProbeOnly
$target=Join-Path $root ('TestRuns/Outposts-DayCheckpoint-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $target 'Saves') -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $source 'Config') -Destination $target -Recurse
Copy-Item -LiteralPath $save -Destination (Join-Path $target 'Saves/TMM-OutpostDay.rws')
$installed=Join-Path $root 'Assemblies/TurboMegaMod.TestProbe.dll'
if(Test-Path -LiteralPath $installed){throw 'Another test probe is installed.'}
$process=$null
try {
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build/OutpostsProbe.dll') -Destination $installed
 $process=Start-Process -FilePath (Join-Path $game 'RimWorld.exe') -WorkingDirectory $game -ArgumentList @(('"-savedatafolder='+$target+'"'),'-tmm-outpost-resume-day','-tmm-outpost-probe','-tmm-render-probe') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log') -PassThru
 Write-Output ('CheckpointRoot: '+$target)
 $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
 while(!(Test-Path -LiteralPath (Join-Path $target 'OutpostProbe.json')) -or !(Test-Path -LiteralPath (Join-Path $target 'GpuMapProbe.json'))) {
  if($process.WaitForExit(1000)){throw 'Game exited before checkpoint probes.'}
  $partial=Join-Path $target 'OutpostProbe.json'
  if(Test-Path -LiteralPath $partial){$r=Get-Content -LiteralPath $partial -Raw|ConvertFrom-Json;if(!$r.passed){throw ($r.errors -join ' ')}}
  if([DateTime]::UtcNow -gt $deadline){throw 'Checkpoint test timed out.'}
 }
 $probe=Get-Content -LiteralPath (Join-Path $target 'OutpostProbe.json') -Raw|ConvertFrom-Json
 $activity=Get-Content -LiteralPath (Join-Path $target 'GpuMapProbe.json') -Raw|ConvertFrom-Json
 $log=Join-Path $target 'stdout.log'
 $errors=@(Select-String -LiteralPath $log -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
 $refs=@(Select-String -LiteralPath $log -Pattern 'Could not resolve cross-reference|Could not resolve reference to object with loadID|Could not find class|missing reference|Failed to load reference|Could not load reference')
 $gpu=[bool](Select-String -LiteralPath $log -SimpleMatch 'VK_ERROR_DEVICE_LOST')
 $result=[ordered]@{component='OutpostsDayCheckpoint';checkpointSource=$source;probe=$probe;runtimeErrors=$errors.Count;unresolvedReferences=$refs.Count;gpuLost=$gpu;mapActivity=$activity;sourceSaveUnchanged=((Get-FileHash -LiteralPath $save).Hash -eq $original);productionAssemblySha256=(Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant();passed=($probe.passed -and !$errors.Count -and !$refs.Count -and !$gpu -and $activity.ticksAdvanced -gt 0)}
 $result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $target 'Result.json') -Encoding utf8
 $result|ConvertTo-Json -Depth 8
 if(!$result.passed){throw 'Checkpoint regression failed.'}
}finally {
 if($process -and !$process.HasExited){$process.Kill();$process.WaitForExit()}
 if(Test-Path -LiteralPath $installed){Remove-Item -LiteralPath $installed}
 if((Get-FileHash -LiteralPath $save).Hash -ne $original){throw 'Source checkpoint changed.'}
}
