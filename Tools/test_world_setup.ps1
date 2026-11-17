param([string]$Seed='tmm-regions-wide-20261117',[int]$TimeoutSeconds=360)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$game=Split-Path -Parent (Split-Path -Parent $root)
if(Get-Process RimWorld -ErrorAction SilentlyContinue){throw 'Close RimWorld before the isolated entry-screen test.'}
$installed=Join-Path $root 'Assemblies/TurboMegaMod.TestProbe.dll'
if(Test-Path -LiteralPath $installed){throw 'A test probe is already installed.'}
& (Join-Path $PSScriptRoot 'build_assembly.ps1')
$refs=@('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','Unity.Mathematics.dll')|ForEach-Object {'/reference:'+(Join-Path $root ('Tools/Build/RimRef-1.6.4871/ref/net472/'+$_))}
$refs+='/reference:'+(Join-Path $root 'Assemblies/TurboMegaMod.dll')
$refs+='/reference:'+(& (Join-Path $PSScriptRoot 'get_harmony_reference.ps1'))
$probe=Join-Path $PSScriptRoot 'Build/WorldSetupProbe.dll'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /noconfig /nologo /nostdlib+ /target:library ('/out:'+$probe) @refs (Join-Path $root 'Tests/InGameWorldSetupProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Entry-screen probe compile failed.'}
$target=Join-Path $root ('TestRuns/WorldSetup-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $target 'Config') -Force | Out-Null
$profile=Join-Path $env:USERPROFILE 'AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios'
Copy-Item -LiteralPath (Join-Path $profile 'Config/ModsConfig.xml') -Destination (Join-Path $target 'Config')
foreach($file in Get-ChildItem -LiteralPath (Join-Path $profile 'Config') -File -Filter 'Mod_*.xml'){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $target 'Config')}
if(Test-Path -LiteralPath (Join-Path $profile 'Config/TrueTerrainColorsCache.xml')){Copy-Item -LiteralPath (Join-Path $profile 'Config/TrueTerrainColorsCache.xml') -Destination (Join-Path $target 'Config')}
[xml]$prefs=Get-Content -LiteralPath (Join-Path $profile 'Config/Prefs.xml') -Raw -Encoding UTF8
$prefs.PrefsData.fullscreen='False';$prefs.PrefsData.screenWidth='1280';$prefs.PrefsData.screenHeight='800';$prefs.PrefsData.runInBackground='True'
foreach($name in @('volumeMaster','volumeMusic','volumeGame','volumeAmbient','volumeUI')){$prefs.PrefsData.$name='0'}
$prefs.Save((Join-Path $target 'Config/Prefs.xml'))
$process=$null
try{
 Copy-Item -LiteralPath $probe -Destination $installed
 $process=Start-Process -FilePath (Join-Path $game 'RimWorld.exe') -WorkingDirectory $game -ArgumentList @(('"-savedatafolder='+$target+'"'),'-tmm-world-setup-probe',('-tmm-world-seed='+$Seed)) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log') -PassThru
 Write-Output ('TestRoot: '+$target)
 $end=[DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
 while(!(Test-Path -LiteralPath (Join-Path $target 'WorldSetupProbe.json'))){if($process.WaitForExit(1000)){throw 'Game exited before entry probe completed.'};if([DateTime]::UtcNow -gt $end){throw 'World selection probe timed out.'}}
 $functional=Get-Content (Join-Path $target 'WorldSetupProbe.json') -Raw|ConvertFrom-Json
 $errors=@(Select-String -LiteralPath (Join-Path $target 'stdout.log') -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
 $gpu=[bool](Select-String -LiteralPath (Join-Path $target 'stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')
 $result=[ordered]@{probe=$functional;seed=$Seed;runtimeErrors=$errors.Count;gpuLost=$gpu;quicktestUsed=$false;productionAssemblySha256=(Get-FileHash (Join-Path $root 'Assemblies/TurboMegaMod.dll')).Hash.ToLowerInvariant();passed=($functional.passed -and !$errors.Count -and !$gpu)}
 $result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $target 'Result.json') -Encoding utf8
 $result|ConvertTo-Json -Depth 8
 if(!$result.passed){throw 'World selection regression failed; inspect isolated profile.'}
 $result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $root 'Documentation/WorldSetupVerification.json') -Encoding utf8
}finally{if($process -and !$process.HasExited){$process.Kill();$process.WaitForExit()};if(Test-Path -LiteralPath $installed){Remove-Item -LiteralPath $installed}}
