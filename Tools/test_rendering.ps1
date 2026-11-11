param([string]$Label = 'FullMods-Fullscreen', [switch]$Windowed, [switch]$Hidden, [string[]]$Exclude = @('sarg.smartspeed','dhultgren.smarterconstruction','brrainz.cameraplus'), [int]$ObserveSeconds = 100, [string]$ReuseFrom, [switch]$UseSavedCopy, [string]$SaveFile, [string]$UserDataPath)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
$userData = if ($UserDataPath) { $UserDataPath } else { Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios' }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'RimWorld is already running' }
& (Join-Path $PSScriptRoot 'build_assembly.ps1')
$referencePath = Join-Path $PSScriptRoot 'Build\RimRef-1.6.4871\ref\net472'
$references = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','System.Xml.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll','Unity.Mathematics.dll') | ForEach-Object { '/reference:' + (Join-Path $referencePath $_) }
$references += '/reference:' + (Join-Path $gamePath 'Mods\2009463077\1.6\Assemblies\0Harmony.dll')
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /noconfig /nologo /nostdlib+ /target:library (('/out:') + (Join-Path $PSScriptRoot 'Build\RenderingProbe.dll')) @references (Join-Path $projectPath 'Tests\InGameRenderingProbe.cs') (Join-Path $projectPath 'Tests\InGameLocalizationProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Rendering probe compile failed' }
if ($UseSavedCopy -and !$SaveFile) { throw 'Specify -SaveFile for a copied-save test.' }
$testRoot = Join-Path $projectPath ('TestRuns\Gpu-' + $Label + '-' + (Get-Date -Format 'HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $testRoot 'Config') -Force | Out-Null
[xml]$mods = [IO.File]::ReadAllText((Join-Path $userData 'Config\ModsConfig.xml'))
foreach ($node in @($mods.SelectNodes('/ModsConfigData/activeMods/li'))) { if ($node.InnerText -in $Exclude) { $null = $node.ParentNode.RemoveChild($node) } }
$mods.Save((Join-Path $testRoot 'Config\ModsConfig.xml'))
if ($ReuseFrom) {
    foreach ($folder in @('ShaderCache','TextureCache','BundleCache','HardwareProfile')) {
        $source = Join-Path $ReuseFrom $folder
        if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $testRoot $folder) -Recurse -Force }
    }
}
[xml]$prefs = [IO.File]::ReadAllText((Join-Path $userData 'Config\Prefs.xml'))
foreach ($name in @('volumeMaster','volumeMusic','volumeGame','volumeAmbient','volumeUI')) { $prefs.PrefsData.$name = '0' }
$prefs.PrefsData.runInBackground = 'True'
if ($Windowed) { $prefs.PrefsData.fullscreen = 'False'; $prefs.PrefsData.screenWidth = '1280'; $prefs.PrefsData.screenHeight = '800' }
$prefs.Save((Join-Path $testRoot 'Config\Prefs.xml'))
$process = $null
$installProbe = $false
try {
$arguments = @(('"-savedatafolder=' + $testRoot + '"'), '-quicktest')
if ($UseSavedCopy) {
    New-Item -ItemType Directory -Path (Join-Path $testRoot 'Saves') -Force | Out-Null
    Copy-Item -LiteralPath (Resolve-Path -LiteralPath $SaveFile).Path -Destination (Join-Path $testRoot 'Saves\CrashRepro.rws')
    $arguments = @(('"-savedatafolder=' + $testRoot + '"'), '-tmm-load-repro')
}
$installProbe = @($mods.SelectNodes('/ModsConfigData/activeMods/li') | Where-Object { $_.InnerText -eq 'colonyproject.integrated' }).Count -gt 0
if ($installProbe) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build\RenderingProbe.dll') -Destination (Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll') -Force
    $arguments += @('-tmm-render-probe','-tmm-startup-profile','-tmm-localization-probe')
}
foreach ($source in @(Get-ChildItem -LiteralPath (Join-Path $userData 'Config') -File -Filter 'Mod_*.xml')) {
    Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $testRoot ('Config\' + $source.Name)) -Force
}
$colors = Join-Path $userData 'Config\TrueTerrainColorsCache.xml'
if (Test-Path -LiteralPath $colors) { Copy-Item -LiteralPath $colors -Destination (Join-Path $testRoot 'Config\TrueTerrainColorsCache.xml') -Force }
    $windowStyle = if ($Hidden) { 'Hidden' } else { 'Normal' }
    $process = Start-Process -FilePath (Join-Path $gamePath 'RimWorld.exe') -WorkingDirectory $gamePath -ArgumentList $arguments -WindowStyle $windowStyle -RedirectStandardOutput (Join-Path $testRoot 'stdout.log') -RedirectStandardError (Join-Path $testRoot 'stderr.log') -PassThru
    Write-Output ('TestRoot: ' + $testRoot)
    Write-Output ('PID: ' + $process.Id)
    $exited = $process.WaitForExit($ObserveSeconds * 1000)
    $errors = @(Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
    $result = [ordered]@{label=$Label; exited=$exited; observedSeconds=$ObserveSeconds; stoppedByTestTimer=(!$exited); exitCode=$(if($exited){$process.ExitCode}else{$null}); gpuLost=([bool](Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')); errors=@($errors.Line); path=$testRoot}
    $mapReport = Join-Path $testRoot 'GpuMapProbe.json'
    $languageReport = Join-Path $testRoot 'LocalizationProbe.json'
    $result.mapActivity = if (Test-Path -LiteralPath $mapReport) { Get-Content -LiteralPath $mapReport -Raw | ConvertFrom-Json } else { $null }
    $result.localization = if (Test-Path -LiteralPath $languageReport) { Get-Content -LiteralPath $languageReport -Raw | ConvertFrom-Json } else { $null }
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $testRoot 'Result.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 5
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($installProbe) { Remove-Item -LiteralPath (Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll') -Force }
}
