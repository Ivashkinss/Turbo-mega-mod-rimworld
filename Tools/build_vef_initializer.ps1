param([string]$InputDll, [string]$OutputDll)
$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$harmonyPath=Join-Path $projectPath 'Components\Harmony\1.6\Assemblies\0Harmony.dll'
if (!(Test-Path -LiteralPath $harmonyPath)) { $harmonyPath=Join-Path $projectPath '..\2009463077\1.6\Assemblies\0Harmony.dll' }
$h=[Reflection.Assembly]::LoadFrom($harmonyPath)
$assemblyType=$h.GetType('Mono.Cecil.AssemblyDefinition')
$resolver=[Activator]::CreateInstance($h.GetType('Mono.Cecil.DefaultAssemblyResolver'))
$resolver.AddSearchDirectory((Join-Path $projectPath 'Tools\Build\RimRef-1.6.4871\ref\net472'))
$resolver.AddSearchDirectory((Split-Path -Parent $InputDll))
$resolver.AddSearchDirectory((Split-Path -Parent $harmonyPath))
$readerType=$h.GetType('Mono.Cecil.ReaderParameters')
$parameters=[Activator]::CreateInstance($readerType)
$parameters.AssemblyResolver=$resolver
$a=$assemblyType.GetMethod('ReadAssembly',[type[]]@([string],$readerType)).Invoke($null,@($InputDll,$parameters))
try {
 $module=$a.MainModule
 $target=@($module.Types|Where-Object FullName -eq 'VEF.AestheticScaling.CachedPawnDataExtensions')
 $cache=@($module.Types|Where-Object FullName -eq 'VEF.AestheticScaling.CachedPawnData')
 if($target.Count -ne 1 -or $cache.Count -ne 1){throw 'Unexpected VEF cache types'}
 if(@($target[0].Methods|Where-Object Name -eq 'GetDefaultCache').Count){throw 'Initializer already exists'}
 $fallback=@($cache[0].Fields|Where-Object Name -eq 'defaultCache')
 $accessor=@($target[0].Methods|Where-Object Name -eq 'GetCachePrePatched')
 if($fallback.Count -ne 1 -or !$fallback[0].IsStatic -or $accessor.Count -ne 1){throw 'Default cache API changed'}
 $attrs=[Enum]::Parse($h.GetType('Mono.Cecil.MethodAttributes'),'Public, Static, HideBySig')
 $method=[Activator]::CreateInstance($h.GetType('Mono.Cecil.MethodDefinition'),@('GetDefaultCache',$attrs,$fallback[0].FieldType))
 $paramAttrs=[Enum]::Parse($h.GetType('Mono.Cecil.ParameterAttributes'),'None')
 $parameter=[Activator]::CreateInstance($h.GetType('Mono.Cecil.ParameterDefinition'),@('pawn',$paramAttrs,$accessor[0].Parameters[0].ParameterType))
 $method.Parameters.Add($parameter)
 $target[0].Methods.Add($method)
 $il=$method.Body.GetILProcessor()
 $op=$h.GetType('Mono.Cecil.Cil.OpCodes')
 # Field injection runs inside Pawn..ctor, before health/needs/def exist.
 # Seed the field with VEF's neutral sentinel. Its existing refresh path
 # replaces that reference with the real per-pawn cache after generation.
 $il.Emit($op.GetField('Ldsfld').GetValue($null),$fallback[0])
 $il.Emit($op.GetField('Ret').GetValue($null))
 $writer=[Activator]::CreateInstance($h.GetType('Mono.Cecil.WriterParameters'))
 $writer.DeterministicMvid=$true
 $writer.Timestamp=[uint32]0
 $a.Write($OutputDll,$writer)
 'Added constructor-safe initializer using the original neutral cache.'
} finally {$a.Dispose()}
