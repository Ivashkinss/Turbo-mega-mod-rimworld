# Installs staged Neon SVG art: rasterizes each SVG to a transparent PNG under Textures/ and points the ThingDef's
# graphicData texPath at it (removing the "TODO art" placeholder marker).
# Usage: ./Tools/install_art.ps1 [-Staged Delegation/StoryContent/Art/Art/Weapons] [-TexDir Things/Item/Equipment/TMM] [-Size 256]
param([string]$Staged = 'Delegation/StoryContent/Art/Art/Weapons', [string]$TexDir = 'Things/Item/Equipment/TMM', [int]$Size = 256)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$u = New-Object Text.UTF8Encoding $false
$defFiles = Get-ChildItem Defs -Recurse -Filter *.xml
foreach ($svg in Get-ChildItem $Staged -Filter *.svg) {
    $def = $svg.BaseName
    $png = Join-Path $root ("Textures/$TexDir/$def.png")
    & (Join-Path $PSScriptRoot 'svg_to_png.ps1') -Svg $svg.FullName -Png $png -Size $Size | Out-Null
    $tex = "$TexDir/$def"
    $done = $false
    foreach ($f in $defFiles) {
        $t = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
        $i = $t.IndexOf("<defName>$def</defName>")
        if ($i -lt 0) { continue }
        $end = $t.IndexOf('</ThingDef>', $i)
        $gd = $t.IndexOf('<graphicData>', $i)
        if ($gd -lt 0 -or $gd -gt $end) { Write-Output "$def : no graphicData"; break }
        $ge = $t.IndexOf('</graphicData>', $gd)
        $block = $t.Substring($gd, $ge - $gd)
        $newBlock = [regex]::Replace($block, '<texPath>[^<]*</texPath>(\s*<!--\s*TODO art\s*-->)?', "<texPath>$tex</texPath>")
        $newBlock = [regex]::Replace($newBlock, '<graphicClass>Graphic_StackCount</graphicClass>', '<graphicClass>Graphic_Single</graphicClass>')
        $t = $t.Substring(0, $gd) + $newBlock + $t.Substring($ge)
        [IO.File]::WriteAllText($f.FullName, $t, $u)
        Write-Output "$def -> Textures/$tex.png ($($f.Name))"
        $done = $true
        break
    }
    if (!$done) { Write-Output "$def : def not found" }
}
