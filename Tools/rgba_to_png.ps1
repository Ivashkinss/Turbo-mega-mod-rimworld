# Converts the .rgba files written by Tests/InGameTextureExportProbe.cs (int width, int height, RGBA bytes top-down)
# into transparent PNGs and prints each image's opaque bounding box (useful to fit new apparel onto vanilla bodies).
# Usage: ./Tools/rgba_to_png.ps1 -In <TestRun>\TextureExport -Out Documentation\ArtTemplates
param([Parameter(Mandatory = $true)][string]$In, [Parameter(Mandatory = $true)][string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null
foreach ($f in Get-ChildItem $In -Filter *.rgba) {
    $bytes = [IO.File]::ReadAllBytes($f.FullName)
    $w = [BitConverter]::ToInt32($bytes, 0); $h = [BitConverter]::ToInt32($bytes, 4)
    $bmp = New-Object Drawing.Bitmap $w, $h, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $data = $bmp.LockBits((New-Object Drawing.Rectangle 0, 0, $w, $h), [Drawing.Imaging.ImageLockMode]::WriteOnly, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $buf = New-Object byte[] ($w * $h * 4)
    $minX = $w; $minY = $h; $maxX = -1; $maxY = -1
    for ($i = 0; $i -lt $w * $h; $i++) {
        $o = 8 + $i * 4
        $buf[$i * 4] = $bytes[$o + 2]; $buf[$i * 4 + 1] = $bytes[$o + 1]; $buf[$i * 4 + 2] = $bytes[$o]; $buf[$i * 4 + 3] = $bytes[$o + 3]
        if ($bytes[$o + 3] -gt 20) { $x = $i % $w; $y = [math]::Floor($i / $w); if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }; if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y } }
    }
    [Runtime.InteropServices.Marshal]::Copy($buf, 0, $data.Scan0, $buf.Length)
    $bmp.UnlockBits($data)
    $png = Join-Path $Out ($f.BaseName + '.png')
    $bmp.Save($png, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    "{0} {1}x{2} bbox x={3}..{4} y={5}..{6}" -f $f.BaseName, $w, $h, $minX, $maxX, $minY, $maxY
}
