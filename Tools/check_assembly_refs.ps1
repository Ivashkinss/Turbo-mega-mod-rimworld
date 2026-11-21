# Static compatibility check of a mod assembly against the installed game: every type/method/field it references in
# Assembly-CSharp (and other game assemblies) is resolved with Mono.Cecil against the game's Managed folder.
# Nothing is executed. Prints the references that no longer exist (missing members = would crash at runtime).
# Usage: ./Tools/check_assembly_refs.ps1 -Dll path\to\Mod.dll [-Extra dir1,dir2]
param([Parameter(Mandatory = $true)][string]$Dll, [string[]]$Extra = @(), [string]$Scopes = '^Assembly-CSharp')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$harmony = [Reflection.Assembly]::LoadFrom((Join-Path $root 'Components/Harmony/1.6/Assemblies/0Harmony.dll'))
$game = Split-Path -Parent (Split-Path -Parent $root)
$managed = Join-Path $game 'RimWorldWin64_Data\Managed'
if (!(Test-Path (Join-Path $managed 'Assembly-CSharp.dll'))) { $managed = $game }
$resolverType = $harmony.GetType('Mono.Cecil.DefaultAssemblyResolver')
$resolver = [Activator]::CreateInstance($resolverType)
$resolver.AddSearchDirectory($managed)
$resolver.AddSearchDirectory((Split-Path -Parent (Resolve-Path $Dll).Path))
$resolver.AddSearchDirectory((Join-Path $root 'Components/Harmony/1.6/Assemblies'))
foreach ($e in $Extra) { $resolver.AddSearchDirectory($e) }
$paramsType = $harmony.GetType('Mono.Cecil.ReaderParameters')
$rp = [Activator]::CreateInstance($paramsType)
$paramsType.GetProperty('AssemblyResolver').SetValue($rp, $resolver)
$read = $harmony.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly', [type[]]@([string], $paramsType))
$asm = $read.Invoke($null, [object[]]@((Resolve-Path $Dll).Path, $rp))
$mod = $asm.MainModule
$missing = New-Object System.Collections.Generic.List[string]
$checked = 0
foreach ($t in $mod.GetTypeReferences()) {
    $scope = $t.Scope.Name
    if ($scope -notmatch $Scopes) { continue }
    $checked++
    try { if ($null -eq $t.Resolve()) { $missing.Add("TYPE   $($t.FullName)") } } catch { $missing.Add("TYPE   $($t.FullName) ($($_.Exception.InnerException.Message))") }
}
foreach ($m in $mod.GetMemberReferences()) {
    $dt = $m.DeclaringType
    if ($null -eq $dt) { continue }
    $et = $dt.GetElementType()
    $scope = $et.Scope.Name
    if ($scope -notmatch $Scopes) { continue }
    $checked++
    try { if ($null -eq $m.Resolve()) { $missing.Add("MEMBER $($m.FullName)") } } catch { $missing.Add("MEMBER $($m.FullName)") }
}
"checked $checked references, missing $($missing.Count)"
$missing | Sort-Object -Unique
