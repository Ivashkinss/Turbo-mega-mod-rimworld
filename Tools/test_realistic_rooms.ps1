param([switch]$OriginalActive, [switch]$FullSet, [int]$TimeoutSeconds = 240)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component RealisticRooms @PSBoundParameters
