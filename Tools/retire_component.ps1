param([Parameter(Mandatory=$true)][string]$Component)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$modsPath = (Resolve-Path -LiteralPath (Split-Path -Parent $projectPath)).Path
if (Get-Process RimWorld -ErrorAction SilentlyContinue) { throw 'Close RimWorld before retiring an original.' }
$manifest = Get-Content -LiteralPath (Join-Path $projectPath 'Documentation\UtilityComponents.json') -Raw | ConvertFrom-Json
if ($Component -notin @($manifest.PSObject.Properties.Name)) { throw 'Component is not in the provenance manifest.' }
$entry = $manifest.$Component
foreach ($label in @('Integrated','OriginalActive')) {
    $report = Get-Content -LiteralPath (Join-Path $projectPath ('Documentation\' + $Component + '-' + $label + '.json')) -Raw | ConvertFrom-Json
    if (!$report.passed -or $report.component -cne $Component -or $report.originalComponentActive -ne ($label -eq 'OriginalActive')) { throw ('Missing successful ' + $label + ' verification.') }
}
$folder = $entry.files[0].source.Split('/')[0]
if ([string]::IsNullOrWhiteSpace($folder) -or $folder -in @('.','..') -or [IO.Path]::GetFileName($folder) -cne $folder) { throw 'Unsafe source folder.' }
$originalPath = (Resolve-Path -LiteralPath (Join-Path $modsPath $folder)).Path
if ((Split-Path -Parent $originalPath) -cne $modsPath -or $originalPath -eq $projectPath) { throw 'Source folder is outside the intended Mods directory.' }
if ((Get-Item -LiteralPath $originalPath).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Source folder is a reparse point.' }
[xml]$about = [IO.File]::ReadAllText((Join-Path $originalPath 'About\About.xml'))
if ($about.ModMetaData.packageId.ToLowerInvariant() -cne $entry.packageId.ToLowerInvariant()) { throw 'Original packageId does not match manifest.' }
$stamp = Get-Date -Format 'yyyyMMdd'
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
    foreach ($file in $entry.files) {
        $packed = @($zip.Entries | Where-Object { $_.FullName.Replace('\','/') -ceq $file.source })
        if ($packed.Count -ne 1) { throw ('Backup is missing ' + $file.source) }
        $stream = $packed[0].Open()
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
# Check the resolved deletion boundary again immediately before removal.
if ((Split-Path -Parent (Resolve-Path -LiteralPath $originalPath).Path) -cne $modsPath) { throw 'Deletion target changed.' }
if ($nodes.Count -ne 0) { $config.Save($configPath) }
Remove-Item -LiteralPath $originalPath -Recurse -Force
[ordered]@{ component=$Component; retiredPackageId=$entry.packageId; sourceFilesVerified=$entry.files.Count; archive=$archive; configBackup=$configBackup; remainingActiveMods=$after.Count; orderPreserved=$true } | ConvertTo-Json
