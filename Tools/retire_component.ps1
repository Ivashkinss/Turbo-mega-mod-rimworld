param([Parameter(Mandatory=$true)][string]$Component)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$modsPath = (Resolve-Path -LiteralPath (Split-Path -Parent $projectPath)).Path
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before retiring an original.' }
$manifest = Get-Content -LiteralPath (Join-Path $projectPath 'Documentation\UtilityComponents.json') -Raw | ConvertFrom-Json
if ($Component -notin @($manifest.PSObject.Properties.Name)) { throw 'Component is not in the provenance manifest.' }
$entry = $manifest.$Component
foreach ($label in @('Integrated','OriginalActive')) {
    if ($Component -in @('SmartSpeed','SmarterConstruction')) {
        # These first utilities predate paired per-component reports. The
        # localization probe verifies their exact integrated DLL and real hooks;
        # static verification covers both original-active load guards.
        $report = Get-Content -LiteralPath (Join-Path $projectPath 'Documentation\InGameLocalization.json') -Raw | ConvertFrom-Json
        $static = Get-Content -LiteralPath (Join-Path $projectPath 'Documentation\UtilityVerification.json') -Raw | ConvertFrom-Json
        if (!$report.passed -or !$static.passed -or !$static.standaloneAndOriginalActiveLoadGuardsVerified -or $Component -notin $static.components) { throw 'Missing legacy utility verification.' }
    } elseif ($Component -eq 'InteractionBubbles') {
        $report = Get-Content -LiteralPath (Join-Path $projectPath ('Documentation\Bubbles-' + $label + '.json')) -Raw | ConvertFrom-Json
        if (!$report.passed -or $report.originalBubblesActive -ne ($label -eq 'OriginalActive')) { throw ('Missing successful Bubbles ' + $label + ' verification.') }
    } else {
        $reportPath = Join-Path $projectPath ('Documentation\' + $Component + '-' + $label + '.json')
        if ($label -eq 'Integrated' -and !(Test-Path -LiteralPath $reportPath)) {
            $reportPath = Join-Path $projectPath ('Documentation\' + $Component + '-FullSet.json')
        }
        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        if ($Component -eq 'RealisticRooms' -and $label -eq 'OriginalActive' -and 'originalRoomsActive' -in $report.PSObject.Properties.Name) {
            if (!$report.passed -or !$report.originalRoomsActive) { throw 'Missing successful legacy Rooms verification.' }
        } elseif (!$report.passed -or $report.component -cne $Component -or $report.originalComponentActive -ne ($label -eq 'OriginalActive')) { throw ('Missing successful ' + $label + ' verification.') }
    }
}
$folder = $entry.files[0].source.Split('/')[0]
if ([string]::IsNullOrWhiteSpace($folder) -or $folder -in @('.','..') -or [IO.Path]::GetFileName($folder) -cne $folder) { throw 'Unsafe source folder.' }
$originalPath = (Resolve-Path -LiteralPath (Join-Path $modsPath $folder)).Path
if ((Split-Path -Parent $originalPath) -cne $modsPath -or $originalPath -eq $projectPath) { throw 'Source folder is outside the intended Mods directory.' }
if ((Get-Item -LiteralPath $originalPath).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Source folder is a reparse point.' }
[xml]$about = [IO.File]::ReadAllText((Join-Path $originalPath 'About\About.xml'))
if ($about.ModMetaData.packageId.ToLowerInvariant() -cne $entry.packageId.ToLowerInvariant()) { throw 'Original packageId does not match manifest.' }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$archive = Join-Path $PSScriptRoot ('Build\Removed' + $Component + '-' + $stamp + '.zip')
$configBackup = Join-Path $projectPath ('TestRuns\' + $Component + 'Removal-' + $stamp + '\ModsConfig.before.xml')
if ((Test-Path -LiteralPath $archive) -or (Test-Path -LiteralPath $configBackup)) { throw 'A retirement backup already exists; inspect it before proceeding.' }
$configPath = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml'
[xml]$config = [IO.File]::ReadAllText($configPath)
$before = @($config.ModsConfigData.activeMods.li | ForEach-Object { [string]$_ })
$nodes = @($config.SelectNodes('/ModsConfigData/activeMods/li') | Where-Object { $_.InnerText.ToLowerInvariant() -ceq $entry.packageId.ToLowerInvariant() })
if ($nodes.Count -gt 1) { throw 'Duplicate original packageId in active mods.' }
Compress-Archive -LiteralPath $originalPath -DestinationPath $archive
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $packedFiles = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($packedFile in $zip.Entries) {
        $key = $packedFile.FullName.Replace('\','/')
        if ($packedFiles.ContainsKey($key)) { throw ('Duplicate backup path: ' + $key) }
        $packedFiles.Add($key, $packedFile)
    }
    foreach ($file in $entry.files) {
        if (!$packedFiles.ContainsKey($file.source)) { throw ('Backup is missing ' + $file.source) }
        $stream = $packedFiles[$file.source].Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant() }
        finally { $sha.Dispose(); $stream.Dispose() }
        if ($hash -cne $file.sourceRawSha256) { throw ('Backup source hash mismatch: ' + $file.source) }
    }
} finally { $zip.Dispose() }
New-Item -ItemType Directory -Path (Split-Path -Parent $configBackup) -Force | Out-Null
Copy-Item -LiteralPath $configPath -Destination $configBackup
foreach ($node in $nodes) { $null = $node.ParentNode.RemoveChild($node) }
$expected = @($before | Where-Object { $_.ToLowerInvariant() -cne $entry.packageId.ToLowerInvariant() })
$after = @($config.ModsConfigData.activeMods.li | ForEach-Object { [string]$_ })
if (($expected -join "`n") -cne ($after -join "`n")) { throw 'Other active mods changed order.' }
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'RimWorld started during backup; original kept.' }
$settingsCopied = @()
$configFolder = Split-Path -Parent $configPath
$oldPrefix = 'Mod_' + $folder + '_'
$newPrefix = 'Mod_' + (Split-Path -Leaf $projectPath) + '_'
foreach ($settingsFile in @(Get-ChildItem -LiteralPath $configFolder -File -Filter 'Mod_*.xml')) {
    if (!$settingsFile.Name.StartsWith($oldPrefix, [StringComparison]::OrdinalIgnoreCase)) { continue }
    $destination = Join-Path $configFolder ($newPrefix + $settingsFile.Name.Substring($oldPrefix.Length))
    # Keep both the original file and any existing integrated settings.
    if (!(Test-Path -LiteralPath $destination)) {
        Copy-Item -LiteralPath $settingsFile.FullName -Destination $destination
        $settingsCopied += Split-Path -Leaf $destination
    }
}
# Check the resolved deletion boundary again immediately before removal.
if ((Split-Path -Parent (Resolve-Path -LiteralPath $originalPath).Path) -cne $modsPath) { throw 'Deletion target changed.' }
if ($nodes.Count -ne 0) { $config.Save($configPath) }
Remove-Item -LiteralPath $originalPath -Recurse -Force
[ordered]@{ component=$Component; retiredPackageId=$entry.packageId; sourceFilesVerified=$entry.files.Count; archive=$archive; configBackup=$configBackup; settingsCopied=$settingsCopied; remainingActiveMods=$after.Count; orderPreserved=$true } | ConvertTo-Json
