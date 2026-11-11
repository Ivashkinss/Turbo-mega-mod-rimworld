param([int]$ObserveSeconds = 30)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before starting the isolated test.' }
& (Join-Path $PSScriptRoot 'build_assembly.ps1')
$refPath = Join-Path $PSScriptRoot 'Build\RimRef-1.6.4871\ref\net472'
if (!(Test-Path -LiteralPath $refPath)) { $refPath = Join-Path $gamePath 'RimWorldWin64_Data\Managed' }
$refs = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','System.Xml.dll','Assembly-CSharp.dll','UnityEngine.CoreModule.dll') |
    ForEach-Object { '/reference:' + (Join-Path $refPath $_) }
$harmony = Join-Path $gamePath 'Mods\2009463077\1.6\Assemblies\0Harmony.dll'
if (!(Test-Path -LiteralPath $harmony)) { $harmony = Join-Path $gamePath 'Mods\2009463077\Current\Assemblies\0Harmony.dll' }
$refs += '/reference:' + $harmony
$builtProbe = Join-Path $PSScriptRoot 'Build\TurboMegaMod.TestProbe.dll'
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /noconfig /nologo /target:library /nostdlib+ (('/out:') + $builtProbe) @refs (Join-Path $projectPath 'Tests\InGameLocalizationProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Could not compile the in-game probe.' }
$testRoot = Join-Path $projectPath ('TestRuns\Localization-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path (Join-Path $testRoot 'Config') -Force | Out-Null
$ids = @('brrainz.harmony','ludeon.rimworld')
foreach ($expansion in @('Royalty','Ideology','Biotech','Anomaly','Odyssey')) {
    if (Test-Path -LiteralPath (Join-Path $gamePath ('Data\' + $expansion + '\About\About.xml'))) {
        $ids += 'ludeon.rimworld.' + $expansion.ToLowerInvariant()
    }
}
$ids += @('adaptive.storage.framework','co.uk.epicguru.meleeanimation','zeracronius.dynamictradeinterface','colonyproject.integrated')
$modsXml = '<ModsConfigData><version>1.7.5000 rev1275</version><activeMods>' + (($ids | ForEach-Object { '<li>' + $_ + '</li>' }) -join '') + '</activeMods></ModsConfigData>'
[IO.File]::WriteAllText((Join-Path $testRoot 'Config\ModsConfig.xml'), $modsXml)
[IO.File]::WriteAllText((Join-Path $testRoot 'Config\Prefs.xml'), '<PrefsData><langFolderName>Russian (Русский)</langFolderName><devMode>True</devMode><screenWidth>1280</screenWidth><screenHeight>800</screenHeight><fullscreen>False</fullscreen><runInBackground>True</runInBackground></PrefsData>')
$installedProbe = Join-Path $projectPath 'Assemblies\TurboMegaMod.TestProbe.dll'
$process = $null
try {
    Copy-Item -LiteralPath $builtProbe -Destination $installedProbe -Force
    $process = Start-Process -FilePath (Join-Path $gamePath 'RimWorld.exe') -WorkingDirectory $gamePath -ArgumentList @(('"-savedatafolder=' + $testRoot + '"'), '-quicktest','-tmm-localization-probe') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testRoot 'stdout.log') -RedirectStandardError (Join-Path $testRoot 'stderr.log') -PassThru
    $reportPath = Join-Path $testRoot 'LocalizationProbe.json'
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    while (!(Test-Path -LiteralPath $reportPath)) {
        if ($process.WaitForExit(1000)) { throw 'Game exited before completing the probe.' }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Timed out waiting for the in-game probe.' }
    }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if (!$report.passed) { throw ('In-game localization errors: ' + ($report.errors -join '; ')) }
    if ($process.WaitForExit($ObserveSeconds * 1000)) { throw 'Game exited during tick observation.' }
    $runtimeErrors = @(Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -Pattern '^\d+:\d+:\d+\s+ERROR')
    $knownExternal = 0
    # Installed Odyssey's Russian name rules contain a line without "keyword->".
    # Record this exact pair separately; every other ERROR still fails this run.
    if ($runtimeErrors.Count -eq 2 -and $runtimeErrors[0].Line -match 'ERROR Bad string pass when reading rule \[tradeAdj_fem\] \[tradeNoun_fem\]$' -and
        $runtimeErrors[1].Line -match 'ERROR Failed to resolve text\. Trying again with English\. Exception: System.ArgumentNullException:' -and
        (Select-String -LiteralPath (Join-Path $testRoot 'stdout.log') -SimpleMatch 'RimWorld.FactionGenerator.NewGeneratedFaction')) {
        $knownExternal = 2
    }
    $report | Add-Member -NotePropertyName runtimeErrors -NotePropertyValue $runtimeErrors.Count
    $report | Add-Member -NotePropertyName knownExternalRuntimeErrors -NotePropertyValue $knownExternal
    $report | Add-Member -NotePropertyName unexpectedRuntimeErrors -NotePropertyValue ($runtimeErrors.Count - $knownExternal)
    $report | Add-Member -NotePropertyName externalIssue -NotePropertyValue 'Installed Odyssey Russian faction-name rule lacks keyword-> at DefInjected/RulePackDef/RulePacks_Namers_Factions.xml:138. Game files unchanged.'
    $report | Add-Member -NotePropertyName observedSecondsAfterProbe -NotePropertyValue $ObserveSeconds
    $report | Add-Member -NotePropertyName gameVersion -NotePropertyValue ([IO.File]::ReadAllText((Join-Path $gamePath 'Version.txt')).Trim())
    $report | Add-Member -NotePropertyName visualUiVerified -NotePropertyValue $false
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $projectPath 'Documentation\InGameLocalization.json') -Encoding utf8
    if ($runtimeErrors.Count -ne $knownExternal) { throw ($runtimeErrors.Line -join "`n") }
    $report | ConvertTo-Json -Depth 5
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if (Test-Path -LiteralPath $installedProbe) { Remove-Item -LiteralPath $installedProbe -Force }
}
