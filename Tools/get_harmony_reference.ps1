$projectPath = Split-Path -Parent $PSScriptRoot
foreach ($relative in @('Components\Harmony\1.6\Assemblies\0Harmony.dll', '..\2009463077\1.6\Assemblies\0Harmony.dll', '..\2009463077\Current\Assemblies\0Harmony.dll')) {
    $candidate = Join-Path $projectPath $relative
    if (Test-Path -LiteralPath $candidate) { return (Resolve-Path -LiteralPath $candidate).Path }
}
throw 'Missing Harmony 1.6 compile reference. Restore Components/Harmony from Git.'
