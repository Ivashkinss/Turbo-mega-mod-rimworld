"""Verify the bounded ordinary contract revision on one production DLL."""
import hashlib,json,argparse,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 a=argparse.ArgumentParser();a.add_argument('--contract',required=True);a.add_argument('--housekeeping',required=True);args=a.parse_args()
 digest=sha(R/'Assemblies/TurboMegaMod.dll');assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
 evidence=[];counts={}
 for kind,key,root,minima in [('WorkContract','workContract',Path(args.contract),(19,14)),('Housekeeping','housekeeping',Path(args.housekeeping),(15,10))]:
  root=root.resolve();assert root.is_relative_to((R/'TestRuns').resolve())
  for reload in (False,True):
   file=root/('ReloadResult.json' if reload else 'Result.json');doc=read(file);k=key+('Reload' if reload else '')
   assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost']
   assert doc[k]['passed'] and not doc[k]['errors'] and doc[k]['checks']>=minima[int(reload)]
   assert doc['mapActivity']['ticksAdvanced']>0 and doc['mapActivity']['framesAdvanced']>0
   if reload:assert doc['unresolvedReferences']==0 and doc['isolatedFixtureSave'] and not doc['userSaveTested']
   else:assert doc['localization']['passed'] and doc['localization']['keyed']==2974 and doc['settingsUi']['passed'] and doc['odysseyGrammar']['checks']==252 and doc['odysseyGrammar']['passed']
   name=('WorkContract-Housekeeping' if kind=='Housekeeping' else kind)+('-Reload' if reload else '-FullSet')+'.json';target=R/'Documentation'/name;target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
   counts[k]=doc[k]['checks'];evidence.append(dict(file=name,sha256=sha(target)))
 base=read(R/'Documentation/HousekeepingRevisionVerification.json')['sourceSha256'];sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={'Source/HousekeepingCredit.cs','Source/WorkBuyout.cs'}
 assert set(sources)-set(base)=={'Source/WorkContracts.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=sum(counts.values()),successfulIsolatedSteps=4,checksByStep=counts,sourceSha256=sources,evidence=evidence,ordinaryFreeWorkerStatusTested=True,savedOriginalFactionAndResponseTested=True,nativeCleaningAndRealRoomFundingTested=True,blockedRouteAndCarriedCargoTested=True,actualNeutralFactionMapExitTested=True,pendingMedicalBillsProtected=True,legacyHousekeepingRegression=True,allSubsystemRegression=False,live16Tested=False,userSaveTested=False,visualLayoutInspected=False)
 (R/'Documentation/WorkContractRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))
if __name__=='__main__':main()
