param([string]$SourcePath = (Join-Path $PSScriptRoot '../Source/GuestTripBudget.cs'))
$ErrorActionPreference='Stop'
# Compile the exact pure production method without loading any game assembly.
$source=Get-Content -LiteralPath $SourcePath -Raw
$start=$source.IndexOf('public static int CalculateTarget(int baseRoll, float multiplier)')
if($start -lt 0){throw 'CalculateTarget signature not found.'}
$brace=$source.IndexOf('{',$start);$depth=1;$end=$brace+1
while($depth -gt 0 -and $end -lt $source.Length){
    if($source[$end] -eq '{'){$depth++};if($source[$end] -eq '}'){$depth--};$end++
}
if($depth -ne 0){throw 'Unbalanced target method.'}
$method=$source.Substring($start,$end-$start)
$ceiling=[regex]::Match($source,'public const int TargetCeiling\s*=\s*\d+\s*;').Value
if(!$ceiling){throw 'TargetCeiling declaration not found.'}
$harness=@'
using System;
public static class GuestBudgetMathCheck {
CEILING
METHOD
public static int Main() {
    float[] multipliers={0f,0.5f,1f,2f,float.MaxValue,float.NaN,float.PositiveInfinity,-1f,0.00001f};
    int[] rolls={200,200,200,200,300,200,200,200,2000000000};
    int[] expected={0,100,200,400,10000,0,0,0,10000};
    int failed=0;
    for(int i=0;i<expected.Length;i++){
        int actual=CalculateTarget(rolls[i],multipliers[i]);
        bool ok=actual==expected[i];if(!ok)failed++;
        Console.WriteLine("case="+i+" expected="+expected[i]+" actual="+actual+" "+(ok?"PASS":"FAIL"));
    }
    Console.WriteLine("checks="+expected.Length+" failed="+failed);
    return failed==0?0:1;
}}
'@
$outDir=Join-Path $PSScriptRoot 'Build/GuestTripMath'
New-Item -ItemType Directory -Path $outDir -Force|Out-Null
$inputFile=Join-Path $outDir 'Check.cs';$exe=Join-Path $outDir 'Check.exe'
$harness.Replace('METHOD',$method).Replace('CEILING',$ceiling)|Set-Content -LiteralPath $inputFile -Encoding utf8
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /optimize+ "/out:$exe" $inputFile
if($LASTEXITCODE -ne 0){throw 'Math harness compilation failed.'}
& $exe
if($LASTEXITCODE -ne 0){throw 'Guest target math failed; no game assemblies were executed.'}
