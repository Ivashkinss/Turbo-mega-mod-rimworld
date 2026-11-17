"""Check real policy save/load, physical receipts and a pre-policy ransom save."""
import argparse,hashlib,json,re,xml.etree.ElementTree as E
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--profile',required=True);p.add_argument('--legacy-profile',required=True);p.add_argument('--visual-reviewed',action='store_true');a=p.parse_args()
 digest=sha(R/'Assemblies/TurboMegaMod.dll');fresh=Path(a.profile).resolve();legacy=Path(a.legacy_profile).resolve()
 assert all(p.is_relative_to((R/'TestRuns').resolve()) for p in [fresh,legacy])
 assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
 steps=[(fresh,'Result.json','factionRansomPolicy',24,'FactionRansomPolicy-FullSet.json'),(fresh,'ReloadResult.json','factionRansomPolicyReload',13,'FactionRansomPolicy-Reload.json'),(legacy,'ReloadResult.json','factionRansomReload',9,'FactionRansomPolicy-OldRansom-Reload.json')]
 evidence=[];counts={}
 for folder,file,key,minimum,pub in steps:
  doc=read(folder/file);assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost']
  assert doc[key]['passed'] and not doc[key]['errors'] and doc[key]['checks']>=minimum
  assert doc['mapActivity']['framesAdvanced']>0 and doc['mapActivity']['ticksAdvanced']>0
  if file=='Result.json':
   assert doc['fullUserModSet'] and doc['localization']['passed'] and doc['localization']['keyed']==3126 and doc['settingsUi']['passed']
   assert doc['odysseyGrammar']['passed'] and doc['odysseyGrammar']['checks']==252
  else:assert doc['unresolvedReferences']==0 and doc['isolatedFixtureSave'] and not doc['userSaveTested']
  counts[key]=doc[key]['checks'];target=R/'Documentation'/pub;target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');evidence.append(dict(file=pub,sha256=sha(target)))
 assert read(fresh/'FactionRansomPolicyUiProbe.json')['passed']
 assert read(legacy/'ReloadResult.json')['factionRansomReload']['afterDeception']
 old=legacy/'Saves/TMM-FactionRansom.rws';assert b'TMM_defaultRansomChance' not in old.read_bytes() and b'TMM_ransomOverrides' not in old.read_bytes()
 original=read(legacy/'Result.json')['productionAssemblySha256'];assert original!=digest
 base=read(R/'Documentation/ServiceReadinessRevisionVerification.json')['sourceSha256'];sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={'Source/FactionRansom.cs','Source/WorkBuyout.cs'} and set(sources)-set(base)=={'Source/FactionRansomPolicy.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 langs={l:{x.tag:x.text or '' for x in E.parse(R/f'Languages/{l}/Keyed/FactionRansomPolicy.xml').getroot()} for l in ['Russian','English']}
 assert langs['Russian'].keys()==langs['English'].keys() and len(langs['English'])==16
 for k in langs['English']:assert sorted(re.findall(r'\{\d+\}',langs['English'][k]))==sorted(re.findall(r'\{\d+\}',langs['Russian'][k]))
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=sum(counts.values()),successfulIsolatedSteps=len(steps),checksByStep=counts,sourceSha256=sources,evidence=evidence,
  sameDefLiveFactionInstancesTested=True,settingsAccessibleWithoutWorkersTested=True,defaultAndOverrideBoundsAndResetTested=True,zeroAndHundredActualOffersTested=True,
  fiftyPercentMatchesPreviousSeededAlgorithm=True,outerRngPreservedAtZeroFiftyHundred=True,acceptedAndRefusedOffersCannotBeRerolled=True,
  actualSilverStackIdsSurviveGameSaveLoad=True,rawOutOfBoundsAndNullDuplicateRulesCleanedByProductionLoad=True,physicalReceiptRetryExactlyOnce=True,
  factionMoneyDoesNotFundRoomsTested=True,deceptionAndPaidNeutralMapExitTested=True,oldSaveWithoutPolicyTagsDefaultsToFifty=True,
  oldSaveSha256=sha(old),oldSaveOriginalProductionSha256=original,visualLayoutInspected=a.visual_reviewed,visualResolution='1280x800',screenshotSha256=sha(fresh/'FactionRansomPolicy.png'),
  fullLongNameRowVisuallyInspected=False,otherResolutionsOrDpiTested=False,userSaveTested=False,live16Tested=False,allSubsystemRegression=False)
 (R/'Documentation/FactionRansomPolicyRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))
if __name__=='__main__':main()
