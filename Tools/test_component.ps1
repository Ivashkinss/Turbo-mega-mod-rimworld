param([Parameter(Mandatory=$true)][ValidateSet('RealisticRooms','AlmostThere','DevInYourLanguage','SnapOut','CommonSense','RunAndGun','SimpleSidearms','PickUpAndHaul','NiceBillTab','MorePlanning','ShowMeYourHands')][string]$Component, [switch]$OriginalActive, [switch]$FullSet, [int]$TimeoutSeconds = 240)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
$definition = switch ($Component) {
    'ShowMeYourHands' { @{ folder='Show Me Your Hands (Анимация рукоружия)'; id='mlie.showmeyourhands'; dll='Components\ShowMeYourHands\Assemblies\ShowMeYourHands.dll'; source='InGameHandsProbe.cs'; flag='-tmm-hands-probe'; report='HandsProbe.json'; ui='HandsUiProbe.json'; key='hands'; prefix='ShowMeYourHands' } }
    'MorePlanning' { @{ folder='More Planning'; id='mlie.moreplanning'; dll='Components\MorePlanning\Assemblies\MorePlanning.dll'; source='InGamePlanningProbe.cs'; flag='-tmm-planning-probe'; report='PlanningProbe.json'; ui='PlanningUiProbe.json'; key='planning'; prefix='MorePlanning' } }
    'NiceBillTab' { @{ folder='Nice Bill Tab (Фильтр рецептов)'; id='andromeda.nicebilltab'; dll='Components\NiceBillTab\Assemblies\NiceBillTab.dll'; source='InGameNiceBillProbe.cs'; flag='-tmm-nicebill-probe'; report='NiceBillProbe.json'; ui='NiceBillUiProbe.json'; key='bills'; prefix='NiceBillTab' } }
    'PickUpAndHaul' { @{ folder='Pick Up And Haul (Переноска нескольких предметов)'; id='mehni.pickupandhaul'; dll='Components\PickUpAndHaul\Assemblies\PickUpAndHaul.dll'; source='InGamePickUpAndHaulProbe.cs'; flag='-tmm-hauling-probe'; report='PickUpAndHaulProbe.json'; ui='PickUpAndHaulUiProbe.json'; key='hauling'; prefix='PickUpAndHaul' } }
    'SimpleSidearms' { @{ folder='Simple Sidearms (Ношение двух видов оружия)'; id='petetimessix.simplesidearms'; dll='Components\SimpleSidearms\Assemblies\SimpleSidearms.dll'; source='InGameSimpleSidearmsProbe.cs'; flag='-tmm-sidearms-probe'; report='SimpleSidearmsProbe.json'; ui='SimpleSidearmsUiProbe.json'; key='sidearms'; prefix='SimpleSidearms' } }
    'RunAndGun' { @{ folder='RunAndGun (Стрельба на ходу)'; id='roolo.runandgun.kotobike'; dll='Components\RunAndGun\Assemblies\RunAndGun.dll'; source='InGameRunAndGunProbe.cs'; flag='-tmm-runandgun-probe'; report='RunAndGunProbe.json'; ui='RunAndGunUiProbe.json'; key='runAndGun'; prefix='RunAndGun' } }
    'CommonSense' { @{ folder='Common Sense (Умные действия колонистов)'; id='avilmask.commonsense'; dll='Components\CommonSense\Assemblies\CommonSense.dll'; source='InGameCommonSenseProbe.cs'; flag='-tmm-commonsense-probe'; report='CommonSenseProbe.json'; ui='CommonSenseUiProbe.json'; key='commonSense'; prefix='CommonSense' } }
    'SnapOut' { @{ folder='Snap Out! (Успокоение срывающихся колонистов)'; id='weilbyte.snapout'; dll='Components\SnapOut\Assemblies\SnapOut.dll'; source='InGameSnapOutProbe.cs'; flag='-tmm-snapout-probe'; report='SnapOutProbe.json'; ui='SnapOutUiProbe.json'; key='snapOut'; prefix='SnapOut' } }
    'DevInYourLanguage' { @{ folder='Dev In Your Language'; id='latta.devl10n'; dll='Components\DevInYourLanguage\Assemblies\DevInYourLanguage.dll'; source='InGameDevMenuProbe.cs'; flag='-tmm-devmenu-probe'; report='DevMenuProbe.json'; ui='DevMenuUiProbe.json'; key='devMenu'; prefix='DevMenu' } }
    'RealisticRooms' { @{ folder='Realistic Rooms Rewritten (Снижение требований к комнатам)'; id='lucifer.realisticrooms'; dll='Components\RealisticRooms\Assemblies\RealisticRoomsRewritten.dll'; source='InGameRoomsProbe.cs'; flag='-tmm-rooms-probe'; report='RoomsProbe.json'; ui='RoomsUiProbe.json'; key='rooms'; prefix='Rooms' } }
    'AlmostThere' { @{ folder='Almost There! (Караваны не спят)'; id='duz.almosttherefork'; dll='Components\AlmostThere\Standard\Assemblies\CaravanDontRest.dll'; source='InGameAlmostThereProbe.cs'; flag='-tmm-almostthere-probe'; report='AlmostThereProbe.json'; ui='AlmostThereUiProbe.json'; key='caravan'; prefix='AlmostThere' } }
}
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before starting the isolated test.' }
if ($OriginalActive -and $FullSet) { throw 'Use -OriginalActive and -FullSet separately.' }
if ($OriginalActive -and !(Test-Path -LiteralPath (Join-Path $gamePath ('Mods\' + $definition.folder + '\About\About.xml')))) {
    throw ('Restore original ' + $Component + ' before testing -OriginalActive.')
}
& (Join-Path $PSScriptRoot 'build_assembly.ps1')
$referencePath = Join-Path $PSScriptRoot 'Build\RimRef-1.6.4871\ref\net472'
$references = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','System.Xml.dll','System.Xml.Linq.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','Unity.Mathematics.dll') |
    ForEach-Object { '/reference:' + (Join-Path $referencePath $_) }
$references += '/reference:' + (Join-Path $gamePath 'Mods\2009463077\1.6\Assemblies\0Harmony.dll')
$references += '/reference:' + (Join-Path $projectPath $definition.dll)
$builtProbe = Join-Path $PSScriptRoot ('Build\' + $Component + 'Probe.dll')
$sources = @('InGameLocalizationProbe.cs','InGameRenderingProbe.cs',$definition.source) |
    ForEach-Object { Join-Path $projectPath ('Tests\' + $_) }
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /noconfig /nologo /nostdlib+ /target:library (('/out:') + $builtProbe) @references @sources
if ($LASTEXITCODE -ne 0) { throw ($Component + ' probe compile failed') }
$label = if ($FullSet) { 'FullSet' } elseif ($OriginalActive) { 'OriginalActive' } else { 'Integrated' }
$testRoot = Join-Path $projectPath ('TestRuns\' + $definition.prefix + '-' + $label + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $testRoot 'Config') -Force | Out-Null
$ids = @('brrainz.harmony','ludeon.rimworld')
foreach ($expansion in @('Royalty','Ideology','Biotech','Anomaly','Odyssey')) {
    if (Test-Path -LiteralPath (Join-Path $gamePath ('Data\' + $expansion + '\About\About.xml'))) { $ids += 'ludeon.rimworld.' + $expansion.ToLowerInvariant() }
}
$ids += @('unlimitedhugs.hugslib','adaptive.storage.framework','co.uk.epicguru.meleeanimation','zeracronius.dynamictradeinterface')
if (!$OriginalActive) { $ids = @($ids | Where-Object { $_ -ne $definition.id }) }
if ($OriginalActive) { $ids += $definition.id }
$ids += 'colonyproject.integrated'
$modsXml = '<ModsConfigData><version>1.7.5000 rev1275</version><activeMods>' + (($ids | ForEach-Object { '<li>' + $_ + '</li>' }) -join '') + '</activeMods></ModsConfigData>'
if ($FullSet) {
    $userConfig = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config'
    [xml]$fullConfig = [IO.File]::ReadAllText((Join-Path $userConfig 'ModsConfig.xml'))
    foreach ($node in @($fullConfig.SelectNodes('/ModsConfigData/activeMods/li'))) {
        if ($node.InnerText.ToLowerInvariant() -eq $definition.id) { $null = $node.ParentNode.RemoveChild($node) }
    }
    if ('colonyproject.integrated' -notin @($fullConfig.ModsConfigData.activeMods.li)) { throw 'Turbo mega mod must be active in the full-set profile.' }
    $modsXml = $fullConfig.OuterXml
    foreach ($file in @(Get-ChildItem -LiteralPath $userConfig -File -Filter 'Mod_*.xml')) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $testRoot ('Config\' + $file.Name))
    }
    $cache = Join-Path $userConfig 'TrueTerrainColorsCache.xml'
    if (Test-Path -LiteralPath $cache) { Copy-Item -LiteralPath $cache -Destination (Join-Path $testRoot 'Config\TrueTerrainColorsCache.xml') }
    $userHugs = Join-Path (Split-Path -Parent $userConfig) 'HugsLib'
    if (Test-Path -LiteralPath $userHugs) {
        $testHugs = Join-Path $testRoot 'HugsLib'
        New-Item -ItemType Directory -Path $testHugs -Force | Out-Null
        foreach ($name in @('ModSettings.xml','LastSeenNews.xml','SpottedMods.xml')) {
            $source = Join-Path $userHugs $name
            if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $testHugs $name) }
        }
    }
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
        ('"-savedatafolder=' + $testRoot + '"'), '-quicktest','-tmm-localization-probe','-tmm-render-probe',$definition.flag
    ) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'stdout.log') -RedirectStandardError (Join-Path $testRoot 'stderr.log') -PassThru
    Write-Output ('TestRoot: ' + $testRoot)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $reports = @('LocalizationProbe.json',$definition.report,'GpuMapProbe.json',$definition.ui)
    while (@($reports | Where-Object { !(Test-Path -LiteralPath (Join-Path $testRoot $_)) }).Count) {
        if ($process.WaitForExit(1000)) { throw ('Game exited before completing the probes: ' + $testRoot) }
        if ([DateTime]::UtcNow -gt $deadline) { throw ('Timed out waiting for probes: ' + $testRoot) }
    }
    $localization = Get-Content -LiteralPath (Join-Path $testRoot $reports[0]) -Raw | ConvertFrom-Json
    $componentResult = Get-Content -LiteralPath (Join-Path $testRoot $reports[1]) -Raw | ConvertFrom-Json
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
        component = $Component; originalComponentActive = [bool]$OriginalActive; fullUserModSet = [bool]$FullSet
        gameVersion = [IO.File]::ReadAllText((Join-Path $gamePath 'Version.txt')).Trim()
        localization = $localization; mapActivity = $activity
        settingsUi = $ui
        runtimeErrors = $errors.Count; knownExternalRuntimeErrors = $knownExternal; unexpectedRuntimeErrors = $errors.Count - $knownExternal
        knownExternalMessages = @($errors | ForEach-Object { $_.Line -replace '^\d+:\d+:\d+\s+ERROR ', '' })
        gpuLost = [bool](Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')
        visualUiVerified = $false; stoppedByTest = $true
    }
    $result[$definition.key] = $componentResult
    $result.passed = $localization.passed -and $componentResult.passed -and $ui.passed -and $activity.framesAdvanced -gt 0 -and
        $activity.ticksAdvanced -gt 0 -and $errors.Count -eq $knownExternal -and !$result.gpuLost
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'Result.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    if (!$result.passed) { throw ($Component + ' checks failed; inspect ' + $testRoot) }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectPath ('Documentation\' + $Component + '-' + $label + '.json')) -Encoding utf8
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($probeInstalled -and (Test-Path -LiteralPath $installedProbe)) { Remove-Item -LiteralPath $installedProbe -Force }
}
