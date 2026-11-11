param([switch]$OriginalActive, [switch]$FullSet, [int]$TimeoutSeconds = 240)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before starting the isolated test.' }
if ($OriginalActive -and $FullSet) { throw 'Use -OriginalActive and -FullSet separately.' }
if ($OriginalActive -and !(Test-Path -LiteralPath (Join-Path $gamePath 'Mods\Realistic Rooms Rewritten (Снижение требований к комнатам)\About\About.xml'))) {
    throw 'Restore the original Realistic Rooms before testing -OriginalActive.'
}
& (Join-Path $PSScriptRoot 'build_assembly.ps1')
$referencePath = Join-Path $PSScriptRoot 'Build\RimRef-1.6.4871\ref\net472'
$references = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','System.Xml.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','Unity.Mathematics.dll') |
    ForEach-Object { '/reference:' + (Join-Path $referencePath $_) }
$references += '/reference:' + (Join-Path $gamePath 'Mods\2009463077\1.6\Assemblies\0Harmony.dll')
$references += '/reference:' + (Join-Path $projectPath 'Components\RealisticRooms\Assemblies\RealisticRoomsRewritten.dll')
$builtProbe = Join-Path $PSScriptRoot 'Build\RoomsProbe.dll'
$sources = @('InGameLocalizationProbe.cs','InGameRenderingProbe.cs','InGameRoomsProbe.cs') |
    ForEach-Object { Join-Path $projectPath ('Tests\' + $_) }
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /noconfig /nologo /nostdlib+ /target:library (('/out:') + $builtProbe) @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Rooms probe compile failed' }
$label = if ($FullSet) { 'FullSet' } elseif ($OriginalActive) { 'OriginalActive' } else { 'Integrated' }
$testRoot = Join-Path $projectPath ('TestRuns\Rooms-' + $label + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $testRoot 'Config') -Force | Out-Null
$ids = @('brrainz.harmony','ludeon.rimworld')
foreach ($expansion in @('Royalty','Ideology','Biotech','Anomaly','Odyssey')) {
    if (Test-Path -LiteralPath (Join-Path $gamePath ('Data\' + $expansion + '\About\About.xml'))) { $ids += 'ludeon.rimworld.' + $expansion.ToLowerInvariant() }
}
$ids += @('adaptive.storage.framework','latta.devl10n','co.uk.epicguru.meleeanimation','zeracronius.dynamictradeinterface')
if ($OriginalActive) { $ids += 'lucifer.realisticrooms' }
$ids += 'colonyproject.integrated'
$modsXml = '<ModsConfigData><version>1.7.5000 rev1275</version><activeMods>' + (($ids | ForEach-Object { '<li>' + $_ + '</li>' }) -join '') + '</activeMods></ModsConfigData>'
if ($FullSet) {
    $userConfig = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config'
    [xml]$fullConfig = [IO.File]::ReadAllText((Join-Path $userConfig 'ModsConfig.xml'))
    foreach ($node in @($fullConfig.SelectNodes('/ModsConfigData/activeMods/li'))) {
        if ($node.InnerText.ToLowerInvariant() -eq 'lucifer.realisticrooms') { $null = $node.ParentNode.RemoveChild($node) }
    }
    if ('colonyproject.integrated' -notin @($fullConfig.ModsConfigData.activeMods.li)) { throw 'Turbo mega mod must be active in the full-set profile.' }
    $modsXml = $fullConfig.OuterXml
    foreach ($file in @(Get-ChildItem -LiteralPath $userConfig -File -Filter 'Mod_*.xml')) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $testRoot ('Config\' + $file.Name))
    }
    $cache = Join-Path $userConfig 'TrueTerrainColorsCache.xml'
    if (Test-Path -LiteralPath $cache) { Copy-Item -LiteralPath $cache -Destination (Join-Path $testRoot 'Config\TrueTerrainColorsCache.xml') }
}
[IO.File]::WriteAllText((Join-Path $testRoot 'Config\ModsConfig.xml'), $modsXml)
[IO.File]::WriteAllText((Join-Path $testRoot 'Config\Prefs.xml'), '<PrefsData><langFolderName>Russian (Русский)</langFolderName><devMode>True</devMode><screenWidth>1280</screenWidth><screenHeight>800</screenHeight><fullscreen>False</fullscreen><runInBackground>True</runInBackground><volumeMaster>0</volumeMaster></PrefsData>')
$installedProbe = Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll'
$process = $null
$probeInstalled = $false
try {
    if (Test-Path -LiteralPath $installedProbe) { throw 'A test probe is already installed.' }
    Copy-Item -LiteralPath $builtProbe -Destination $installedProbe
    $probeInstalled = $true
    $process = Start-Process -FilePath (Join-Path $gamePath 'RimWorld.exe') -WorkingDirectory $gamePath -ArgumentList @(
        ('"-savedatafolder=' + $testRoot + '"'), '-quicktest','-tmm-localization-probe','-tmm-render-probe','-tmm-rooms-probe'
    ) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'stdout.log') -RedirectStandardError (Join-Path $testRoot 'stderr.log') -PassThru
    Write-Output ('TestRoot: ' + $testRoot)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $reports = @('LocalizationProbe.json','RoomsProbe.json','GpuMapProbe.json','RoomsUiProbe.json')
    while (@($reports | Where-Object { !(Test-Path -LiteralPath (Join-Path $testRoot $_)) }).Count) {
        if ($process.WaitForExit(1000)) { throw ('Game exited before completing the probes: ' + $testRoot) }
        if ([DateTime]::UtcNow -gt $deadline) { throw ('Timed out waiting for probes: ' + $testRoot) }
    }
    $localization = Get-Content -LiteralPath (Join-Path $testRoot $reports[0]) -Raw | ConvertFrom-Json
    $rooms = Get-Content -LiteralPath (Join-Path $testRoot $reports[1]) -Raw | ConvertFrom-Json
    $activity = Get-Content -LiteralPath (Join-Path $testRoot $reports[2]) -Raw | ConvertFrom-Json
    $ui = Get-Content -LiteralPath (Join-Path $testRoot $reports[3]) -Raw | ConvertFrom-Json
    $errors = @(Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
    $knownExternal = 0
    $vefIssue = 'Field injection: VEF: VEF.AestheticScaling.CachedPawnDataExtensions.GetCachePrePatched: initializer GetDefaultCache must be exactly one static method on VEF.AestheticScaling.CachedPawnDataExtensions returning VEF.AestheticScaling.CachedPawnData and taking the target or nothing (0 found).'
    $odysseyErrors = $errors
    if ($FullSet) {
        $vefErrors = @($errors | Where-Object { ($_.Line -replace '^\d+:\d+:\d+\s+ERROR ', '') -ceq $vefIssue })
        if ($vefErrors.Count -eq 1) {
            $knownExternal = 1
            $odysseyErrors = @($errors | Where-Object { $_ -ne $vefErrors[0] })
        }
    }
    if ($odysseyErrors.Count -eq 2 -and $odysseyErrors[0].Line -match 'ERROR Bad string pass when reading rule \[tradeAdj_fem\] \[tradeNoun_fem\]$' -and
        $odysseyErrors[1].Line -match 'ERROR Failed to resolve text\. Trying again with English\. Exception: System.ArgumentNullException:' -and
        (Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'RimWorld.FactionGenerator.NewGeneratedFaction')) { $knownExternal = 2 }
    if ($FullSet -and $vefErrors.Count -eq 1 -and $knownExternal -eq 2) { $knownExternal = 3 }
    $result = [ordered]@{
        originalRoomsActive = [bool]$OriginalActive; fullUserModSet = [bool]$FullSet
        gameVersion = [IO.File]::ReadAllText((Join-Path $gamePath 'Version.txt')).Trim()
        localization = $localization; rooms = $rooms; mapActivity = $activity
        settingsUi = $ui
        runtimeErrors = $errors.Count; knownExternalRuntimeErrors = $knownExternal; unexpectedRuntimeErrors = $errors.Count - $knownExternal
        knownExternalMessages = @($errors | ForEach-Object { $_.Line -replace '^\d+:\d+:\d+\s+ERROR ', '' })
        gpuLost = [bool](Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')
        visualUiVerified = $false; stoppedByTest = $true
    }
    $result.passed = $localization.passed -and $rooms.passed -and $ui.passed -and $activity.framesAdvanced -gt 0 -and
        $activity.ticksAdvanced -gt 0 -and $errors.Count -eq $knownExternal -and !$result.gpuLost
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'Result.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    if (!$result.passed) { throw ('Rooms checks failed; inspect ' + $testRoot) }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectPath ('Documentation\RealisticRooms-' + $label + '.json')) -Encoding utf8
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($probeInstalled -and (Test-Path -LiteralPath $installedProbe)) { Remove-Item -LiteralPath $installedProbe -Force }
}
