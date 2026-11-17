# Metadata only; do not load or execute provider/game code.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$h=[Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $root 'Components/Harmony/1.6/Assemblies/0Harmony.dll')).Path)
$read=$h.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly',[type[]]@([string]))
$records=@()
foreach($version in @('1.6','1.7')){
    foreach($spec in @(@{provider='Hospitality';type='Hospitality.Utilities.GenericUtility';method='FillIncidentQueue'},@{provider='Hospitality';type='Hospitality.IncidentWorker_VisitorGroup';method='GiveItems'},@{provider='Hospitality';type='Hospitality.ModSettings_Hospitality';method='.cctor'},@{provider='Hospital';type='Hospital.IncidentScheduler';method='MapComponentTick'})){
        $relative='Components/'+$spec.provider+'/'+$version+'/Assemblies/'+$spec.provider+'.dll';$path=Join-Path $root $relative
        if(!(Test-Path -LiteralPath $path)){continue}
        $a=$read.Invoke($null,@([string]$path))
        try{
            $type=$a.MainModule.Types|Where-Object FullName -eq $spec.type
            $method=$type.Methods|Where-Object Name -eq $spec.method|Select-Object -First 1
            $instructions=@($method.Body.Instructions|Where-Object {$_.OpCode.Name -eq 'ldc.r4' -or $_.OpCode.Name -match 'ldsfld|stsfld' -and [string]$_.Operand -match 'silverMultiplier' -or $_.OpCode.Name -match '^call' -and [string]$_.Operand -match 'Rand::(Range|Gaussian)|InverseLerp|FreeMedicalBeds'})
            $records+=@{provider=$relative;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();type=$spec.type;method=$spec.method;selectedInstructions=@($instructions|ForEach-Object {$_.ToString()})}
        }finally{$a.Dispose()}
    }
}
@{metadataOnly=$true;runtimeParametersOrFrequencyMeasured=$false;records=$records}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $root 'Documentation/VisitorFlowNativeMetadata.json') -Encoding utf8
Write-Output 'Wrote read-only visitor-flow metadata evidence.'
