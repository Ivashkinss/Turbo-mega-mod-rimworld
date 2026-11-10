$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
$managedPath = Join-Path $gamePath 'RimWorldWin64_Data\Managed'
$frameworkPath = Join-Path $gamePath 'Mods\Adaptive Storage Framework (Библиотека)\1.6\Assemblies'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputPath = Join-Path $projectPath 'Assemblies'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$references = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','UnityEngine.CoreModule.dll','Assembly-CSharp.dll') |
    ForEach-Object { '/reference:' + (Join-Path $managedPath $_) }
$references += @('AdaptiveStorageFramework.dll','1ITransformable.dll') |
    ForEach-Object { '/reference:' + (Join-Path $frameworkPath $_) }
$references += '/reference:' + (Join-Path $gamePath 'Mods\2009463077\Current\Assemblies\0Harmony.dll')
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectPath 'Source') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
& $compilerPath /noconfig /nologo /target:library /optimize+ /nostdlib+ @references (('/out:') + (Join-Path $outputPath 'TurboMegaMod.dll')) @sourceFiles
if ($LASTEXITCODE -ne 0) { throw "Compiler failed: $LASTEXITCODE" }
