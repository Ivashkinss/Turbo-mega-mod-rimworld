$ErrorActionPreference='Stop'
$projectPath=Split-Path -Parent $PSScriptRoot
$harmonyPath=Join-Path $projectPath 'Components\Harmony\1.6\Assemblies\0Harmony.dll'
if(!(Test-Path -LiteralPath $harmonyPath)){$harmonyPath=Join-Path $projectPath '..\2009463077\1.6\Assemblies\0Harmony.dll'}
$h=[Reflection.Assembly]::LoadFrom($harmonyPath)
$read=$h.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly',[type[]]@([string]))
$baseline=$read.Invoke($null,@([string](Join-Path $projectPath 'Components\VanillaExpandedFramework\1.6\Assemblies\VEF.dll')))
$derived=$read.Invoke($null,@([string](Join-Path $projectPath 'Components\VanillaExpandedFramework\1.7\Assemblies\VEF.dll')))
$checked=0
try {
    $oldTypes=@($baseline.MainModule.GetTypes());$newTypes=@($derived.MainModule.GetTypes())
    if(($oldTypes.FullName -join "`n") -cne ($newTypes.FullName -join "`n")){throw 'Unexpected type changes'}
    foreach($old in $oldTypes){
        $new=$newTypes|Where-Object FullName -eq $old.FullName
        if(($old.Fields.FullName -join "`n") -cne ($new.Fields.FullName -join "`n")){throw ('Unexpected field changes '+$old.FullName)}
        $added=@($new.Methods|Where-Object {$_.FullName -notin $old.Methods.FullName})
        $expected=if($old.FullName -eq 'VEF.AestheticScaling.CachedPawnDataExtensions'){1}else{0}
        if($added.Count -ne $expected -or ($expected -eq 1 -and $added[0].Name -ne 'GetDefaultCache')){throw 'Unexpected method additions'}
        foreach($method in $old.Methods){
            $copy=@($new.Methods|Where-Object FullName -eq $method.FullName)
            if($copy.Count -ne 1){throw ('Missing original method '+$method.FullName)}
            $before=($method.Body.Instructions|ForEach-Object ToString) -join "`n"
            $after=($copy[0].Body.Instructions|ForEach-Object ToString) -join "`n"
            if($before -cne $after){throw ('Modified original IL '+$method.FullName)}
            $checked++
        }
    }
    $initializer=($newTypes|Where-Object FullName -eq 'VEF.AestheticScaling.CachedPawnDataExtensions').Methods|Where-Object Name -eq GetDefaultCache
    if($initializer.Body.Instructions.Count -ne 2 -or $initializer.Body.Instructions[0].OpCode.Name -ne 'ldsfld' -or $initializer.Body.Instructions[0].Operand.FullName -ne 'VEF.AestheticScaling.CachedPawnData VEF.AestheticScaling.CachedPawnData::defaultCache' -or $initializer.Body.Instructions[1].OpCode.Name -ne 'ret'){throw 'Wrong initializer IL'}
    $result=@{originalMethodsUnchanged=$checked; addedMethods=1; constructorSafeNeutralSentinel=$true; passed=$true}
    $result|ConvertTo-Json|Set-Content (Join-Path $projectPath 'Documentation\VefInitializerTests.json') -Encoding utf8
    $result|ConvertTo-Json -Compress
} finally {$baseline.Dispose();$derived.Dispose()}
