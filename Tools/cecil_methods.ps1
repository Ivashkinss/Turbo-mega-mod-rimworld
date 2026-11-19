# Lists the method signatures of one game type by reading Assembly-CSharp metadata with Mono.Cecil.
# Game code is never executed. Usage: ./Tools/cecil_methods.ps1 -Type Verse.LetterStack [-Name ReceiveLetter]
param([Parameter(Mandatory = $true)][string]$Type, [string]$Name = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$harmony = [Reflection.Assembly]::LoadFrom((Join-Path $root 'Components/Harmony/1.6/Assemblies/0Harmony.dll'))
$read = $harmony.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly', [type[]]@([string]))
[string]$game = Join-Path (Split-Path -Parent (Split-Path -Parent $root)) 'Assembly-CSharp.dll'
$asm = $read.Invoke($null, [object[]]@($game))
$t = $asm.MainModule.Types | Where-Object { $_.FullName -eq $Type }
if ($null -eq $t) { throw "type not found: $Type" }
foreach ($m in $t.Methods) {
    if ($Name -and $m.Name -ne $Name) { continue }
    $p = ($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', '
    "$($m.ReturnType.Name) $($m.Name)($p)"
}
