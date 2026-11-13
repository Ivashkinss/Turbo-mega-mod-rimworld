param([string]$ManagedPath)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$gamePath = Split-Path -Parent (Split-Path -Parent $projectPath)
if (!$ManagedPath) {
    $ManagedPath = Join-Path $gamePath 'RimWorldWin64_Data\Managed'
    if (!(Test-Path -LiteralPath (Join-Path $ManagedPath 'Assembly-CSharp.dll'))) {
        # Keep the shared 1.6/1.7 DLL on the original Unity/.NET Framework ABI.
        # RimWorld 1.7 redirects these references at runtime. Its UnityStubs
        # cannot be mixed with the installed 1.6 Adaptive Storage references.
        $ManagedPath = Join-Path $PSScriptRoot 'Build\RimRef-1.6.4871\ref\net472'
    }
}
if (!(Test-Path -LiteralPath (Join-Path $ManagedPath 'Assembly-CSharp.dll'))) {
    throw 'Supply -ManagedPath with RimWorld 1.6 managed/reference assemblies; see README.md.'
}
$frameworkPath = Join-Path $projectPath 'Components\AdaptiveStorage\Assemblies'
if (!(Test-Path -LiteralPath (Join-Path $frameworkPath 'AdaptiveStorageFramework.dll'))) {
    $frameworkPath = Join-Path $gamePath 'Mods\Adaptive Storage Framework (Библиотека)\1.6\Assemblies'
}
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputPath = Join-Path $projectPath 'Assemblies'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$references = @('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','Unity.Mathematics.dll','Assembly-CSharp.dll') |
    ForEach-Object { '/reference:' + (Join-Path $ManagedPath $_) }
$references += @('AdaptiveStorageFramework.dll','1ITransformable.dll') |
    ForEach-Object { '/reference:' + (Join-Path $frameworkPath $_) }
$harmonyPath = & (Join-Path $PSScriptRoot 'get_harmony_reference.ps1')
$references += '/reference:' + $harmonyPath
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectPath 'Source') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$buildPath = Join-Path $PSScriptRoot 'Build'
New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
$builtAssembly = Join-Path $buildPath 'TurboMegaMod.dll'
& $compilerPath /noconfig /nologo /target:library /optimize+ /nostdlib+ @references (('/out:') + $builtAssembly) @sourceFiles
if ($LASTEXITCODE -ne 0) { throw "Compiler failed: $LASTEXITCODE" }
Copy-Item -LiteralPath $builtAssembly -Destination (Join-Path $outputPath 'TurboMegaMod.dll') -Force
