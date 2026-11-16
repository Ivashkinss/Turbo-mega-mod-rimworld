param([Parameter(Mandatory=$true)][string]$Profile,[int]$TimeoutSeconds=300)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$profilePath=(Resolve-Path -LiteralPath $Profile).Path
$allowed=(Join-Path $root 'TestRuns')+[IO.Path]::DirectorySeparatorChar
if(!$profilePath.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Use isolated old fixture only'}
$source=Get-Content -LiteralPath (Join-Path $profilePath 'Result.json') -Raw|ConvertFrom-Json
$saveFile=Join-Path $profilePath 'Saves/TMM-Hotel.rws'
$saveText=Get-Content -LiteralPath $saveFile -Raw
if($saveText.Contains('TurboMegaMod.ComplexPricing') -or $saveText.Contains('TMM_salePercent')){throw 'Old fixture must predate pricing component'}
$current=(Get-FileHash -LiteralPath (Join-Path $root 'Assemblies/TurboMegaMod.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
if(!$source.passed -or $source.productionAssemblySha256 -eq $current){throw 'Passing prior-assembly hotel fixture required'}
& (Join-Path $PSScriptRoot 'test_component.ps1') -Component Hospitality -FullSet -UseBuiltAssembly -CompileProbeOnly | Out-Null
& (Join-Path $PSScriptRoot 'test_hotel_reload.ps1') -Profile $profilePath -TimeoutSeconds $TimeoutSeconds | Out-Null
$loaded=Get-Content -LiteralPath (Join-Path $profilePath 'ReloadResult.json') -Raw|ConvertFrom-Json
if(!$loaded.passed -or $loaded.productionAssemblySha256 -ne $current){throw 'Prior-save migration failed'}
$loaded|Add-Member -NotePropertyName sourceSaveAssemblySha256 -NotePropertyValue $source.productionAssemblySha256
$loaded|Add-Member -NotePropertyName oldPricingComponentAbsent -NotePropertyValue $true
$loaded|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $profilePath 'MigrationResult.json') -Encoding utf8
$loaded|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $root 'Documentation/Pricing-HotelMigration.json') -Encoding utf8
Write-Output ('TestRoot: '+$profilePath)
$loaded|ConvertTo-Json -Depth 8
