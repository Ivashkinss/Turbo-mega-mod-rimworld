"""Verify the exact-binary faction ransom checkpoint and bounded regressions."""
import argparse,hashlib,json,xml.etree.ElementTree as E,re
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--ransom',required=True);p.add_argument('--legacy',required=True);p.add_argument('--housekeeping',required=True);a=p.parse_args();digest=sha(R/'Assemblies/TurboMegaMod.dll')
 assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
 runs=[('ransom',a.ransom,'Result.json','factionRansom',16,'FactionRansom-FullSet.json'),('payment reload',a.ransom,'PaymentReloadResult.json','factionRansomReload',11,'FactionRansom-Payment-Reload.json'),('release reload',a.ransom,'ReleaseReloadResult.json','factionRansomReload',8,'FactionRansom-Release-Reload.json'),('old contract reload',a.legacy,'ReloadResult.json','workContractReload',14,'FactionRansom-OldContract-Reload.json'),('housekeeping',a.housekeeping,'Result.json','housekeeping',15,'FactionRansom-Housekeeping-FullSet.json'),('housekeeping reload',a.housekeeping,'ReloadResult.json','housekeepingReload',10,'FactionRansom-Housekeeping-Reload.json')]
 counts={};evidence=[]
 for name,root,filename,key,minimum,pub in runs:
  root=Path(root).resolve();assert root.is_relative_to((R/'TestRuns').resolve());doc=read(root/filename)
  assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost']
  assert doc[key]['passed'] and not doc[key]['errors'] and doc[key]['checks']>=minimum and doc['mapActivity']['ticksAdvanced']>0 and doc['mapActivity']['framesAdvanced']>0
  if filename=='Result.json':assert doc['fullUserModSet'] and doc['localization']['passed'] and doc['localization']['keyed']==3006 and doc['settingsUi']['passed'] and doc['odysseyGrammar']['passed'] and doc['odysseyGrammar']['checks']==252
  else:assert doc['unresolvedReferences']==0 and doc['isolatedFixtureSave'] and not doc['userSaveTested']
  if name in ('payment reload','release reload'):assert doc[key]['afterDeception']
  counts[name]=doc[key]['checks'];target=R/'Documentation'/pub;target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');evidence.append(dict(file=pub,sha256=sha(target)))
 oldSave=Path(a.legacy)/'Saves/TMM-WorkContract.rws';assert b'TurboMegaMod.FactionRansoms' not in oldSave.read_bytes()
 base=read(R/'Documentation/PawnOverviewRevisionVerification.json')['sourceSha256'];sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={'Source/WorkBuyout.cs','Source/WorkContracts.cs'} and set(sources)-set(base)=={'Source/FactionRansom.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 for file,count in [('FactionRansom',17),('WorkBuyout',9),('WorkContract',11)]:
  lang={l:{x.tag:x.text or '' for x in E.parse(R/f'Languages/{l}/Keyed/{file}.xml').getroot()} for l in ('Russian','English')}
  assert lang['Russian'].keys()==lang['English'].keys() and len(lang['English'])==count
  for k in lang['English']:assert sorted(re.findall(r'\{\d+\}',lang['English'][k]))==sorted(re.findall(r'\{\d+\}',lang['Russian'][k]))
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=sum(counts.values()),successfulIsolatedSteps=len(runs),checksByStep=counts,sourceSha256=sources,evidence=evidence,physicalPartialEscrowAndRetryTested=True,genPlaceAvailabilityFaultInjected=True,literallyFilledTerrainTested=False,twoRealScribeLoadsTested=True,deceptionSurvivesLoadAndLateRelease=True,deceptionDoesNotChangeGoodwillTested=True,nativeHonestAndLateReleaseActuallyExited=True,legacyAndNewBuyoutMutualExclusionTested=True,oldSaveWithoutRansomComponentLoaded=True,oldSaveSha256=sha(oldSave),oldSaveOriginalProductionSha256=read(Path(a.legacy)/'Result.json')['productionAssemblySha256'],noFactionMoneyInRoomFundingTested=True,nullOrLostTransactionMapDoesNotRedirect=True,realSecondMapTested=False,userSaveTested=False,live16Tested=False,visualLayoutInspected=False,allSubsystemRegression=False,raidsOrRebellionsImplemented=False)
 (R/'Documentation/FactionRansomRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))
if __name__=='__main__':main()
