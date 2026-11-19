# Takes the newest Neon answer from Delegation/Hydrogen/Inbox (files as "path" + ```code``` blocks), stages them under
# Delegation/StoryContent/<Folder>/<path>, lints C# for syntax newer than C# 5 and checks XML well-formedness.
# Usage: ./Tools/stage_neon_code.ps1 -Folder E3
param([Parameter(Mandatory = $true)][string]$Folder)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$f = Get-ChildItem Delegation\Hydrogen\Inbox -File | Where-Object Name -ne 'README.md' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$f) { throw 'Inbox is empty' }
"file: $($f.Name)"
$src = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
$m = [regex]::Matches($src, '(?s)(?:^|\n)[#*`\s]*`?([\w/\.\-]+\.(?:cs|xml))`?[*\s]*\r?\n\s*```(\w*)\s*\r?\n(.*?)```')
$out = Join-Path $root "Delegation\StoryContent\$Folder"
$u = New-Object Text.UTF8Encoding $false
foreach ($x in $m) {
    $path = $x.Groups[1].Value; $code = $x.Groups[3].Value
    $dst = Join-Path $out $path
    New-Item -ItemType Directory -Force (Split-Path $dst) | Out-Null
    [IO.File]::WriteAllText($dst, $code, $u)
    $lines = $code -split "`n"
    $note = ''
    if ($path.EndsWith('.cs')) {
        $bad = 0
        foreach ($l in $lines) {
            $c = ($l -split '//')[0]
            if ($c -match '\?\.|\?\[|\bnameof\b|\$"|\bout var\b|\bis\s+[A-Z]\w*\s+[a-z]\w*\b') { $bad++ }
            if ($c -match '^\s*(public|private|protected|internal)[^=(]*\)\s*=>' -or $c -match '^\s*(public|private|protected|internal)[^=(]*\s=>\s') { $bad++ }
        }
        $note = "csharp6+ hits: $bad"
    }
    else { try { [xml]$null = $code; $note = 'xml ok' } catch { $note = 'XML BAD: ' + $_.Exception.Message } }
    "{0}: {1} lines, {2}" -f $path, $lines.Count, $note
}
"CHECK API marks: " + ([regex]::Matches($src, 'CHECK API')).Count
Move-Item $f.FullName (Join-Path $out 'neon-raw.md') -Force
"staged: $out"
