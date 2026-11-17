param([Parameter(Mandatory=$true)][string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$game=Split-Path -Parent (Split-Path -Parent $root)
$source=(Resolve-Path -LiteralPath $Profile).Path
$allowed=(Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar
if(!$source.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Use an isolated TestRuns fixture only.'}
if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'Close RimWorld before testing.'}
$save=Join-Path $source 'Saves/TMM-ClinicAdmission.rws'
$originalHash=(Get-FileHash -LiteralPath $save).Hash
[xml]$xml=Get-Content -LiteralPath $save -Raw -Encoding UTF8
if($xml.SelectSingleNode('//TMM_regionalGeneration')){throw 'Require a pre-regional fixture.'}
$target=Join-Path $root ('TestRuns/RegionalWorld-OldClinic-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $target 'Saves') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'Config') -Destination $target -Recurse
Copy-Item -LiteralPath $save -Destination (Join-Path $target 'Saves/TMM-ClinicAdmission.rws')
@($xml.SelectNodes('//world/worldObjects/worldObjects/li[@Class="Settlement"]')|ForEach-Object {$_.ID+'|'+$_.tile}) | Set-Content -LiteralPath (Join-Path $target 'LegacySettlementPositions.txt') -Encoding utf8
$refs=@('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','System.Xml.dll','System.Xml.Linq.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','Unity.Mathematics.dll')|ForEach-Object {'/reference:'+(Join-Path $root ('Tools/Build/RimRef-1.6.4871/ref/net472/'+$_))}
$refs+='/reference:'+(Join-Path $root 'Assemblies/TurboMegaMod.dll')
$refs+='/reference:'+(& (Join-Path $PSScriptRoot 'get_harmony_reference.ps1'))
$probe=Join-Path $root 'Tools/Build/RegionalWorldLegacyProbe.dll'
$sources=@('InGameLocalizationProbe.cs','InGameRenderingProbe.cs','InGameGrammarProbe.cs','InGameRegionalWorldProbe.cs','InGameClinicAdmissionAcceptanceProbe.cs')|ForEach-Object {Join-Path $root ('Tests/'+$_)}
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /noconfig /nologo /nostdlib+ /target:library ('/out:'+$probe) @refs @sources
if($LASTEXITCODE -ne 0){throw 'Legacy probe compile failed.'}
$installed=Join-Path $root 'Assemblies/TurboMegaMod.TestProbe.dll'
if(Test-Path -LiteralPath $installed){throw 'Test probe already installed.'}
$process=$null
try{
 Copy-Item -LiteralPath $probe -Destination $installed
 $process=Start-Process -FilePath (Join-Path $game 'RimWorld.exe') -WorkingDirectory $game -ArgumentList @(('"-savedatafolder='+$target+'"'),'-tmm-regional-legacy','-tmm-render-probe') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log') -PassThru
 $end=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
 while(!(Test-Path -LiteralPath (Join-Path $target 'RegionalWorldLegacyProbe.json')) -or !(Test-Path -LiteralPath (Join-Path $target 'GpuMapProbe.json'))){if($process.WaitForExit(1000)){throw 'Game exited before probes.'};if([DateTime]::UtcNow -gt $end){throw 'Legacy load timed out.'}}
 $functional=Get-Content (Join-Path $target 'RegionalWorldLegacyProbe.json') -Raw|ConvertFrom-Json
 $activity=Get-Content (Join-Path $target 'GpuMapProbe.json') -Raw|ConvertFrom-Json
 $errors=@(Select-String -LiteralPath (Join-Path $target 'stdout.log') -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
 $broken=@(Select-String -LiteralPath (Join-Path $target 'stdout.log') -Pattern 'Could not find think node with key|Could not resolve cross-reference|Could not find class|missing reference|Failed to load reference|Could not load reference')
 $gpu=[bool](Select-String -LiteralPath (Join-Path $target 'stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')
 $result=[ordered]@{probe=$functional;mapActivity=$activity;runtimeErrors=$errors.Count;unresolvedReferences=$broken.Count;gpuLost=$gpu;sourceSaveUnchanged=((Get-FileHash -LiteralPath $save).Hash -eq $originalHash);profile=$target;productionAssemblySha256=(Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant()}
 $result.passed=$functional.passed -and $activity.ticksAdvanced -gt 0 -and $activity.framesAdvanced -gt 0 -and !$gpu -and !$errors.Count -and !$broken.Count -and $result.sourceSaveUnchanged
 $result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $target 'Result.json') -Encoding utf8
 $result|ConvertTo-Json -Depth 8
 if(!$result.passed){throw 'Old world regression failed.'}
 $result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $root 'Documentation/RegionalWorld-OldClinic.json') -Encoding utf8
}finally{if($process -and !$process.HasExited){$process.Kill();$process.WaitForExit()};if(Test-Path -LiteralPath $installed){Remove-Item -LiteralPath $installed};if((Get-FileHash -LiteralPath $save).Hash -ne $originalHash){throw 'Original fixture changed.'}}
