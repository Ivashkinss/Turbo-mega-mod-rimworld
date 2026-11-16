$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$h=[Reflection.Assembly]::LoadFrom((Join-Path $project 'Components/Harmony/1.6/Assemblies/0Harmony.dll'))
$read=$h.GetType('Mono.Cecil.AssemblyDefinition').GetMethod('ReadAssembly',[type[]]@([string]))
$results=@()
foreach($version in @('1.6','1.7')){
 $path=Join-Path $project ('Components/Hospitality/'+$version+'/Assemblies/Hospitality.dll')
 $assembly=$read.Invoke($null,@([string]$path))
 try{
  foreach($pair in @(
   @('Hospitality.IncidentWorker_VisitorGroup','GiveItems',1,'System.Void'),
   @('Hospitality.IncidentWorker_VisitorGroup','GetPawnsToSpawn',1,'System.Collections.Generic.IEnumerable`1<Verse.Pawn>'),
   @('Hospitality.JobGiver_ClaimBed','TryGiveJob',1,'Verse.AI.Job'),
   @('Hospitality.Utilities.BedUtility','FindBedFor',1,'Hospitality.Building_GuestBed'),
   @('Hospitality.CompGuest','Leave',1,'System.Void'),
   @('Hospitality.LordToil_VisitPoint','GetVisitScore',1,'System.Single')
  )){
   $type=$assembly.MainModule.Types|Where-Object FullName -eq $pair[0]
   $methods=@($type.Methods|Where-Object {$_.Name -eq $pair[1] -and $_.Parameters.Count -eq $pair[2]})
   if($methods.Count -ne 1 -or $methods[0].ReturnType.FullName -ne $pair[3]){throw ('Hook signature changed: '+$version+' '+$pair[0]+'.'+$pair[1])}
   $results+=@{version=$version;signature=$methods[0].FullName;hasBody=$methods[0].HasBody}
  }
  $type=$assembly.MainModule.Types|Where-Object FullName -eq 'Hospitality.Utilities.BedUtility'
  $method=$type.Methods|Where-Object Name -eq 'CalculateBedValue'|Sort-Object {$_.Parameters.Count} -Descending|Select-Object -First 1
  if($method.ReturnType.FullName -ne 'System.Single' -or $method.Parameters[0].ParameterType.FullName -ne 'Hospitality.Building_GuestBed' -or $method.Parameters[1].ParameterType.FullName -ne 'Verse.Pawn'){throw 'Bed scoring hook changed'}
  $results+=@{version=$version;signature=$method.FullName;hasBody=$method.HasBody}
 }finally{$assembly.Dispose()}
}
$result=[ordered]@{passed=$true;metadataOnly=$true;liveGame16Verified=$false;checks=$results.Count;hooks=$results}
$result|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $project 'Documentation/VipHookVerification.json') -Encoding utf8
'Verified '+$results.Count+' native hook signatures (metadata only)'
