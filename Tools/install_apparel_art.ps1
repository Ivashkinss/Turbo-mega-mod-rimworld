# Installs Neon apparel SVGs. Input: <Staged>/<defName>_south.svg, _north.svg, _east.svg drawn on the Male body
# (512x512 canvas = the 128x128 vanilla texture scaled by 4, fitted to Documentation/ArtTemplates/Naked_Male_*).
# - Headgear (Overhead/EyeCover layers): Textures/Things/Pawn/Humanlike/Apparel/TMM/<def>/<def>_<rot>.png
# - Body apparel: the Male drawing is refit to Female/Thin/Hulk/Fat by mapping the vanilla naked-body bounding box of
#   Male onto each body type (per rotation), giving <def>_<Body>_<rot>.png for all five body types.
# - Icon: the south view cropped and centred, Textures/Things/Item/TMM/Apparel/<def>.png
# - Defs: graphicData texPath -> icon, apparel wornGraphicPath -> the worn set; "TODO art" markers removed.
# Usage: ./Tools/install_apparel_art.ps1 -Staged Delegation/StoryContent/Apparel1/Art/Apparel [-Size 256]
param([Parameter(Mandatory = $true)][string]$Staged, [int]$Size = 256)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Add-Type -AssemblyName System.Drawing
$u = New-Object Text.UTF8Encoding $false
# Bounding boxes (minX, maxX, minY, maxY) of vanilla garments in 128-px texture space, measured with Tools/rgba_to_png.ps1.
# A Male drawing is refit to another body type with the boxes of the vanilla garment of the same kind, so shoulders and
# hems scale the way the vanilla artists drew them: Shell -> Duster, power armor -> PowerArmor, the rest -> ShirtBasic.
$ref = @{
    'Duster' = @{
        'Male_south' = 25, 103, 39, 111; 'Male_north' = 25, 103, 39, 111; 'Male_east' = 34, 89, 39, 116
        'Female_south' = 33, 93, 36, 113; 'Female_north' = 33, 93, 36, 114; 'Female_east' = 32, 83, 42, 112
        'Thin_south' = 42, 84, 40, 112; 'Thin_north' = 42, 84, 40, 113; 'Thin_east' = 33, 78, 40, 114
        'Hulk_south' = 16, 113, 28, 127; 'Hulk_north' = 16, 112, 27, 126; 'Hulk_east' = 17, 96, 32, 127
        'Fat_south' = 17, 109, 33, 115; 'Fat_north' = 17, 109, 33, 116; 'Fat_east' = 9, 103, 33, 118 }
    'PowerArmor' = @{
        'Male_south' = 30, 97, 40, 111; 'Male_north' = 30, 97, 40, 109; 'Male_east' = 35, 89, 38, 112
        'Female_south' = 38, 90, 40, 110; 'Female_north' = 37, 91, 40, 110; 'Female_east' = 36, 81, 37, 110
        'Thin_south' = 38, 89, 39, 111; 'Thin_north' = 38, 89, 39, 109; 'Thin_east' = 41, 79, 40, 112
        'Hulk_south' = 21, 105, 32, 127; 'Hulk_north' = 21, 105, 31, 127; 'Hulk_east' = 23, 98, 33, 126
        'Fat_south' = 20, 106, 35, 120; 'Fat_north' = 21, 107, 35, 116; 'Fat_east' = 21, 104, 34, 116 }
    'ShirtBasic' = @{
        'Male_south' = 38, 89, 43, 107; 'Male_north' = 38, 89, 43, 107; 'Male_east' = 42, 85, 43, 106
        'Female_south' = 40, 87, 38, 112; 'Female_north' = 40, 87, 38, 112; 'Female_east' = 36, 81, 39, 112
        'Thin_south' = 50, 77, 43, 107; 'Thin_north' = 52, 75, 44, 106; 'Thin_east' = 43, 74, 44, 107
        'Hulk_south' = 26, 101, 32, 126; 'Hulk_north' = 27, 100, 33, 125; 'Hulk_east' = 27, 94, 36, 124
        'Fat_south' = 24, 103, 38, 112; 'Fat_north' = 24, 102, 39, 111; 'Fat_east' = 23, 99, 38, 114 }
}
$tmp = Join-Path $env:TEMP ('tmm-apparel-' + [Guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Force $tmp | Out-Null
$defFiles = Get-ChildItem Defs -Recurse -Filter *.xml

function Get-DefBlock($def) {
    foreach ($f in $defFiles) {
        $t = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
        $i = $t.IndexOf("<defName>$def</defName>"); if ($i -lt 0) { continue }
        $e = $t.IndexOf('</ThingDef>', $i)
        return @{ file = $f.FullName; text = $t; start = $i; end = $e; block = $t.Substring($i, $e - $i) }
    }
    return $null
}

function Render-Fitted($svgText, $refName, $fromKey, $toKey, $png) {
    $m = $ref[$refName][$fromKey]; $b = $ref[$refName][$toKey]
    $sx = (($b[1] + 1) - $b[0]) / (($m[1] + 1) - $m[0]); $sy = (($b[3] + 1) - $b[2]) / (($m[3] + 1) - $m[2])
    $tx = $b[0] * 4 - $m[0] * 4 * $sx; $ty = $b[2] * 4 - $m[2] * 4 * $sy
    $open = [regex]::Match($svgText, '<svg[^>]*>')
    $inner = $svgText.Substring($open.Index + $open.Length); $inner = $inner.Substring(0, $inner.LastIndexOf('</svg>'))
    $g = [string]::Format([Globalization.CultureInfo]::InvariantCulture, '<g transform="translate({0:0.###},{1:0.###}) scale({2:0.#####},{3:0.#####})">', $tx, $ty, $sx, $sy)
    $out = $open.Value + $g + $inner + '</g></svg>'
    $p = Join-Path $tmp ([Guid]::NewGuid().ToString('N') + '.svg'); [IO.File]::WriteAllText($p, $out, $u)
    & (Join-Path $PSScriptRoot 'svg_to_png.ps1') -Svg $p -Png $png -Size $Size | Out-Null
}

function Make-Icon($srcPng, $iconPng) {
    $src = New-Object Drawing.Bitmap $srcPng
    $minX = $src.Width; $minY = $src.Height; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt $src.Height; $y += 2) { for ($x = 0; $x -lt $src.Width; $x += 2) { if ($src.GetPixel($x, $y).A -gt 20) { if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }; if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y } } } }
    $dst = New-Object Drawing.Bitmap $Size, $Size
    if ($maxX -ge 0) {
        $w = $maxX - $minX + 2; $h = $maxY - $minY + 2; $scale = [math]::Min(($Size * 0.88) / $w, ($Size * 0.88) / $h)
        $gr = [Drawing.Graphics]::FromImage($dst); $gr.InterpolationMode = 'HighQualityBicubic'; $gr.Clear([Drawing.Color]::Transparent)
        $dw = $w * $scale; $dh = $h * $scale
        $gr.DrawImage($src, (New-Object Drawing.RectangleF (($Size - $dw) / 2), (($Size - $dh) / 2), $dw, $dh), (New-Object Drawing.RectangleF $minX, $minY, $w, $h), [Drawing.GraphicsUnit]::Pixel)
        $gr.Dispose()
    }
    New-Item -ItemType Directory -Force (Split-Path $iconPng) | Out-Null
    $dst.Save($iconPng, [Drawing.Imaging.ImageFormat]::Png); $dst.Dispose(); $src.Dispose()
}

$defs = Get-ChildItem $Staged -Filter '*_south.svg' | ForEach-Object { $_.BaseName -replace '_south$', '' }
try {
    foreach ($def in $defs) {
        $info = Get-DefBlock $def
        if (!$info) { "$def : def not found"; continue }
        $isHead = $info.block -match '<li[^>]*>(Overhead|EyeCover)</li>'
        $refName = if ($info.block -match '<li[^>]*>TMM_PowerArmor</li>') { 'PowerArmor' } elseif ($info.block -match '<li[^>]*>Shell</li>') { 'Duster' } else { 'ShirtBasic' }
        $wornDir = "Textures/Things/Pawn/Humanlike/Apparel/TMM/$def"
        $wornBase = "Things/Pawn/Humanlike/Apparel/TMM/$def/$def"
        $missing = @()
        foreach ($rot in 'south', 'north', 'east') {
            $svgPath = Join-Path $Staged "${def}_$rot.svg"
            if (!(Test-Path $svgPath)) { $missing += $rot; continue }
            $svgText = [IO.File]::ReadAllText($svgPath, [Text.Encoding]::UTF8)
            if ($isHead) {
                & (Join-Path $PSScriptRoot 'svg_to_png.ps1') -Svg $svgPath -Png (Join-Path $root "$wornDir/${def}_$rot.png") -Size $Size | Out-Null
            } else {
                foreach ($body in 'Male', 'Female', 'Thin', 'Hulk', 'Fat') { Render-Fitted $svgText $refName "Male_$rot" "${body}_$rot" (Join-Path $root "$wornDir/${def}_${body}_$rot.png") }
            }
        }
        if ($missing.Count) { "$def : missing views $($missing -join ',')"; continue }
        $southPng = if ($isHead) { Join-Path $root "$wornDir/${def}_south.png" } else { Join-Path $root "$wornDir/${def}_Male_south.png" }
        Make-Icon $southPng (Join-Path $root "Textures/Things/Item/TMM/Apparel/$def.png")
        # Defs
        $info = Get-DefBlock $def
        $nb = [regex]::Replace($info.block, '(?s)(<graphicData>.*?<texPath>)[^<]*(</texPath>)[ \t]*(<!--\s*TODO art\s*-->)?', "`${1}Things/Item/TMM/Apparel/$def`${2}", 1)
        $nb = [regex]::Replace($nb, '(?s)(<graphicData>.*?<graphicClass>)Graphic_\w+(</graphicClass>)', '${1}Graphic_Single${2}', 1)
        if ($nb -match '<wornGraphicPath>') { $nb = [regex]::Replace($nb, '<wornGraphicPath>[^<]*</wornGraphicPath>[ \t]*(<!--\s*TODO art\s*-->)?', "<wornGraphicPath>$wornBase</wornGraphicPath>", 1) }
        else { $nb = $nb -replace '<apparel>', "<apparel>`n      <wornGraphicPath>$wornBase</wornGraphicPath>" }
        $t = $info.text.Substring(0, $info.start) + $nb + $info.text.Substring($info.end)
        [IO.File]::WriteAllText($info.file, $t, $u)
        $kind = if ($isHead) { 'head' } else { 'body x5' }
        "$def -> $wornBase ($kind) + icon"
    }
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
