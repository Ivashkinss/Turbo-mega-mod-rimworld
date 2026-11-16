param([string]$Cases)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!$Cases){$Cases=Join-Path $root 'Tests\DeathrestBudgetCases.json'}
Add-Type -Path (Join-Path $root 'Source\GuestDeathrestRules.cs')
$data=Get-Content -LiteralPath $Cases -Raw | ConvertFrom-Json
if($data.cases.Count -ne 20){throw 'Expected 20 independently prepared cases'}
foreach($case in $data.cases){
    $i=$case.inputs;$e=$case.expected
    $budget=[TurboMegaMod.GuestDeathrestRules]::Budget($i.now,$i.nextDue,$i.duration,$i.rate)
    $affordable=[TurboMegaMod.GuestDeathrestRules]::Affordable($i.silver,$budget)
    if($budget -ne $e.budget -or $affordable -ne $e.affordable){throw ('C# mismatch '+$case.id)}
}
[ordered]@{passed=$true;cases=$data.cases.Count;actualCSharpSource=$true;sourceSha256=(Get-FileHash (Join-Path $root 'Source\GuestDeathrestRules.cs')).Hash.ToLowerInvariant();casesSha256=(Get-FileHash $Cases).Hash.ToLowerInvariant();runtimeTest=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'Documentation\DeathrestBudgetVerification.json') -Encoding utf8
