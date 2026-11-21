# Rasterizes an SVG into a transparent PNG with headless Microsoft Edge (no extra software needed).
# Usage: ./Tools/svg_to_png.ps1 -Svg art/T60.svg -Png Textures/Things/Apparel/T60/T60_south.png -Size 128
param([Parameter(Mandatory = $true)][string]$Svg, [Parameter(Mandatory = $true)][string]$Png, [int]$Size = 128)
$ErrorActionPreference = 'Stop'
$edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (!$edge) { throw 'Microsoft Edge not found' }
$svgPath = (Resolve-Path $Svg).Path
$pngPath = [IO.Path]::GetFullPath($Png)
New-Item -ItemType Directory -Force (Split-Path $pngPath) | Out-Null
$work = Join-Path $env:TEMP ('tmm-svg-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null
try {
    $svgText = [IO.File]::ReadAllText($svgPath, [Text.Encoding]::UTF8)
    $html = "<!DOCTYPE html><html><head><meta charset='utf-8'><style>html,body{margin:0;padding:0;background:transparent;overflow:hidden}svg{display:block;width:${Size}px;height:${Size}px}</style></head><body>$svgText</body></html>"
    $htmlPath = Join-Path $work 'page.html'
    [IO.File]::WriteAllText($htmlPath, $html, (New-Object Text.UTF8Encoding $false))
    $shot = Join-Path $work 'shot.png'
    $args = @('--headless=new', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000', "--window-size=$Size,$Size", "--user-data-dir=$work\profile", "--screenshot=$shot", ('file:///' + $htmlPath.Replace('\', '/')))
    $p = Start-Process -FilePath $edge -ArgumentList $args -PassThru -WindowStyle Hidden
    if (!$p.WaitForExit(60000)) { $p.Kill(); throw 'Edge timed out' }
    if (!(Test-Path $shot)) { throw 'Edge produced no screenshot' }
    Copy-Item $shot $pngPath -Force
    Add-Type -AssemblyName System.Drawing
    $img = [Drawing.Image]::FromFile($pngPath); $w = $img.Width; $h = $img.Height; $img.Dispose()
    "$pngPath ${w}x${h}"
} finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
