param([switch]$OriginalActive, [int]$TimeoutSeconds = 240)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before starting the isolated test.' }
if ($OriginalActive -and !(Test-Path -LiteralPath (Join-Path $gamePath 'Mods\Interaction Bubbles (Речевые пузыри)\Assemblies\Bubbles.dll'))) {
    throw 'The original Interaction Bubbles is not installed; restore it before testing -OriginalActive.'
}
& (Join-Path $PSScriptRoot 'build_assembly.ps1')
$referencePath = Join-Path $PSScriptRoot 'Build\RimRef-1.6.4871\ref\net472'
$references = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','System.Xml.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll','Unity.Mathematics.dll') |
    ForEach-Object { '/reference:' + (Join-Path $referencePath $_) }
$references += '/reference:' + (& (Join-Path $PSScriptRoot 'get_harmony_reference.ps1'))
$builtProbe = Join-Path $PSScriptRoot 'Build\BubblesProbe.dll'
$sources = @('InGameLocalizationProbe.cs','InGameRenderingProbe.cs','InGameBubblesProbe.cs') |
    ForEach-Object { Join-Path $projectPath ('Tests\' + $_) }
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /noconfig /nologo /nostdlib+ /target:library (('/out:') + $builtProbe) @references @sources
if ($LASTEXITCODE -ne 0) { throw 'Bubbles probe compile failed' }
$label = if ($OriginalActive) { 'OriginalActive' } else { 'Integrated' }
$testRoot = Join-Path $projectPath ('TestRuns\Bubbles-' + $label + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $testRoot 'Config') -Force | Out-Null
$ids = @('ludeon.rimworld')
foreach ($expansion in @('Royalty','Ideology','Biotech','Anomaly','Odyssey')) {
    if (Test-Path -LiteralPath (Join-Path $gamePath ('Data\' + $expansion + '\About\About.xml'))) {
        $ids += 'ludeon.rimworld.' + $expansion.ToLowerInvariant()
    }
}

if ($OriginalActive) { $ids += 'jaxe.bubbles' }
$ids += 'colonyproject.integrated'
$modsXml = '<ModsConfigData><version>1.7.5000 rev1275</version><activeMods>' + (($ids | ForEach-Object { '<li>' + $_ + '</li>' }) -join '') + '</activeMods></ModsConfigData>'
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
        ('"-savedatafolder=' + $testRoot + '"'), '-quicktest','-tmm-localization-probe','-tmm-render-probe','-tmm-bubbles-probe'
    ) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'stdout.log') -RedirectStandardError (Join-Path $testRoot 'stderr.log') -PassThru
    Write-Output ('TestRoot: ' + $testRoot)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $reports = @('LocalizationProbe.json','BubblesProbe.json','GpuMapProbe.json')
    while (@($reports | Where-Object { !(Test-Path -LiteralPath (Join-Path $testRoot $_)) }).Count) {
        if ($process.WaitForExit(1000)) { throw ('Game exited before completing the probes: ' + $testRoot) }
        if ([DateTime]::UtcNow -gt $deadline) { throw ('Timed out waiting for probes: ' + $testRoot) }
    }
    $localization = Get-Content -LiteralPath (Join-Path $testRoot $reports[0]) -Raw | ConvertFrom-Json
    $bubbles = Get-Content -LiteralPath (Join-Path $testRoot $reports[1]) -Raw | ConvertFrom-Json
    $activity = Get-Content -LiteralPath (Join-Path $testRoot $reports[2]) -Raw | ConvertFrom-Json
    $errors = @(Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -Pattern '^\d+:\d+:\d+\s+(ERROR|CRIT)')
    $knownExternal = 0
    if ($errors.Count -eq 2 -and $errors[0].Line -match 'ERROR Bad string pass when reading rule \[tradeAdj_fem\] \[tradeNoun_fem\]$' -and
        $errors[1].Line -match 'ERROR Failed to resolve text\. Trying again with English\. Exception: System.ArgumentNullException:' -and
        (Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'RimWorld.FactionGenerator.NewGeneratedFaction')) { $knownExternal = 2 }
    $result = [ordered]@{
        originalBubblesActive = [bool]$OriginalActive
        gameVersion = [IO.File]::ReadAllText((Join-Path $gamePath 'Version.txt')).Trim()
        localization = $localization; bubbles = $bubbles; mapActivity = $activity
        runtimeErrors = $errors.Count; knownExternalRuntimeErrors = $knownExternal
        unexpectedRuntimeErrors = $errors.Count - $knownExternal
        gpuLost = [bool](Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'VK_ERROR_DEVICE_LOST')
        visualUiVerified = $false; stoppedByTest = $true
    }
    $result.passed = $localization.passed -and $bubbles.passed -and $activity.framesAdvanced -gt 0 -and
        $activity.ticksAdvanced -gt 0 -and $errors.Count -eq $knownExternal -and !$result.gpuLost
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'Result.json') -Encoding utf8
    $result | ConvertTo-Json -Depth 8
    if (!$result.passed) { throw ('Bubbles checks failed; inspect ' + $testRoot) }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectPath ('Documentation\Bubbles-' + $label + '.json')) -Encoding utf8
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($probeInstalled -and (Test-Path -LiteralPath $installedProbe)) { Remove-Item -LiteralPath $installedProbe -Force }
}
