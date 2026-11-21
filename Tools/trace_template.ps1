# Traces the opaque silhouette of a 128x128 template PNG into an SVG path on a 512x512 canvas (scale 4):
# for every row the leftmost and rightmost opaque pixels; left edge going down, right edge going up.
# Good enough for body/garment outlines given to the artist as fitting guides.
# Usage: ./Tools/trace_template.ps1 -Png Documentation/ArtTemplates/Naked_Male_south.png [-Step 2]
param([Parameter(Mandatory = $true)][string]$Png, [int]$Step = 2, [int]$Alpha = 40)
Add-Type -AssemblyName System.Drawing
$bmp = New-Object Drawing.Bitmap ((Resolve-Path $Png).Path)
$left = New-Object System.Collections.Generic.List[string]; $right = New-Object System.Collections.Generic.List[string]
for ($y = 0; $y -lt $bmp.Height; $y += $Step) {
    $l = -1; $r = -1
    for ($x = 0; $x -lt $bmp.Width; $x++) { if ($bmp.GetPixel($x, $y).A -gt $Alpha) { if ($l -lt 0) { $l = $x }; $r = $x } }
    if ($l -ge 0) { $left.Add(('{0},{1}' -f ($l * 4), ($y * 4))); $right.Insert(0, ('{0},{1}' -f (($r + 1) * 4), ($y * 4))) }
}
$bmp.Dispose()
if ($left.Count -eq 0) { return '' }
'M' + ($left -join ' L') + ' L' + ($right -join ' L') + ' Z'
