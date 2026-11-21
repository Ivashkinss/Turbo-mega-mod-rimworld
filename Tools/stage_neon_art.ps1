# Turns Neon's SVG answers into game textures.
# 1. Concatenates all *.md answer parts in Delegation/StoryContent/<Folder> (by name).
# 2. Extracts every "Art/<Kind>/<Name>.svg" + ```svg block into Delegation/StoryContent/<Folder>/Art/...
# 3. Rasterizes each SVG with headless Edge into Textures/Things/TMM/<Kind>/... (weapons and items 128 px, projectiles 64 px).
#    Stack variants Name_a/_b/_c go into a folder Textures/Things/TMM/Items/Name/ for Graphic_StackCount.
# 4. Points the matching ThingDef graphicData at the new texture and drops its "TODO art" marker.
# Usage: ./Tools/stage_neon_art.ps1 -Folder Art1 [-NoDefs]
param([Parameter(Mandatory = $true)][string]$Folder, [switch]$NoDefs)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root "Delegation\StoryContent\$Folder"
$u = New-Object Text.UTF8Encoding $false
$parts = Get-ChildItem $dir -File -Filter *.md | Where-Object { $_.Name -notin 'README.md' } | Sort-Object Name
if (!$parts) { throw "no answer parts in $dir" }
$src = ($parts | ForEach-Object { [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8) }) -join "`n"
$m = [regex]::Matches($src, '(?s)[`*\s]*(Art/(Weapons|Items|Projectiles|Apparel|Buildings)/([\w\-]+)\.svg)[`*\s]*\r?\n\s*```(?:svg|xml)?\s*\r?\n(.*?)```')
"svg blocks: $($m.Count)"
$done = @{}
foreach ($x in $m) {
    $rel = $x.Groups[1].Value; $kind = $x.Groups[2].Value; $name = $x.Groups[3].Value; $svg = $x.Groups[4].Value.Trim()
    if ($svg -notmatch '^<svg' -and $svg -notmatch '^<\?xml') { "skip $rel (not an svg)"; continue }
    $svgPath = Join-Path $dir $rel
    New-Item -ItemType Directory -Force (Split-Path $svgPath) | Out-Null
    [IO.File]::WriteAllText($svgPath, $svg, $u)
    $size = if ($kind -eq 'Projectiles') { 64 } else { 128 }
    $stack = [regex]::Match($name, '^(.+)_([abc])$')
    if ($stack.Success) { $def = $stack.Groups[1].Value; $png = "Textures\Things\TMM\$kind\$def\$name.png"; $done[$def] = "Things/TMM/$kind/$def|stack" }
    else { $def = $name; $png = "Textures\Things\TMM\$kind\$name.png"; $done[$def] = "Things/TMM/$kind/$name|single" }
    & (Join-Path $PSScriptRoot 'svg_to_png.ps1') -Svg $svgPath -Png (Join-Path $root $png) -Size $size | Out-Null
    "png  $png"
}
if ($NoDefs) { return }
$changed = 0
foreach ($f in Get-ChildItem Defs -Recurse -Filter *.xml) {
    $t = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8); $orig = $t
    foreach ($def in $done.Keys) {
        $i = $t.IndexOf("<defName>$def</defName>"); if ($i -lt 0) { continue }
        $e = $t.IndexOf('</ThingDef>', $i); if ($e -lt 0) { continue }
        $blk = $t.Substring($i, $e - $i)
        $path, $mode = $done[$def].Split('|')
        $cls = if ($mode -eq 'stack') { 'Graphic_StackCount' } else { 'Graphic_Single' }
        $nb = [regex]::Replace($blk, '(?s)(<graphicData>.*?<texPath>)[^<]*(</texPath>)[ \t]*(<!--\s*TODO art\s*-->)?', "`${1}$path`${2}", 1)
        $nb = [regex]::Replace($nb, '(?s)(<graphicData>.*?<graphicClass>)Graphic_(Single|StackCount)(</graphicClass>)', "`${1}$cls`${3}", 1)
        if ($nb -ne $blk) { $t = $t.Substring(0, $i) + $nb + $t.Substring($e); $changed++ }
    }
    if ($t -ne $orig) { [IO.File]::WriteAllText($f.FullName, $t, $u) }
}
"defs updated: $changed of $($done.Count) textures"
