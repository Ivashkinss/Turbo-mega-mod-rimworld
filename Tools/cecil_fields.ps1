# Lists the public fields (with types) of a type in any assembly via Mono.Cecil, including inherited ones from the
# same assembly. Nothing is executed. Usage: ./Tools/cecil_fields.ps1 -Dll path\Vehicles.dll -Type Vehicles.VehicleDef
param([Parameter(Mandatory = $true)][string]$Dll, [Parameter(Mandatory = $true)][string]$Type, [switch]$NoInherited)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$harmony = [Reflection.Assembly]::LoadFrom((Join-Path $root 'Components/Harmony/1.6/Assemblies/0Harmony.dll'))
$read = $harmony.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly', [type[]]@([string]))
$asm = $read.Invoke($null, [object[]]@((Resolve-Path $Dll).Path))
$all = @{}; foreach ($t in $asm.MainModule.GetTypes()) { $all[$t.FullName] = $t }
$t = $all[$Type]; if (!$t) { throw "type not found: $Type" }
while ($t) {
    "== $($t.FullName)"
    foreach ($f in $t.Fields) { if ($f.IsPublic -and !$f.IsStatic) { "  {0} {1}" -f $f.FieldType.Name, $f.Name } }
    if ($NoInherited -or !$t.BaseType) { break }
    $t = $all[$t.BaseType.FullName]
}
