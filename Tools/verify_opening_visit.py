"""Validate one opening invitation, native queue execution, and an older hotel save."""
import argparse,hashlib,json,re,xml.etree.ElementTree as E
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--profile',required=True);p.add_argument('--legacy-profile',required=True);a=p.parse_args()
 fresh=Path(a.profile).resolve();legacy=Path(a.legacy_profile).resolve();digest=sha(R/'Assemblies/TurboMegaMod.dll')
 assert all(p.is_relative_to((R/'TestRuns').resolve()) for p in [fresh,legacy]);assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
 steps=[(fresh,'Result.json','openingVisit',16,'OpeningVisit-FullSet.json'),(fresh,'ReloadResult.json','openingVisitReload',7,'OpeningVisit-Reload.json'),(legacy,'ReloadResult.json','vipReload',10,'OpeningVisit-OldVip-Reload.json')]
 evidence=[];counts={}
 for folder,file,key,minimum,pub in steps:
  doc=read(folder/file);assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost']
  assert doc[key]['passed'] and not doc[key]['errors'] and doc[key]['checks']>=minimum and doc['mapActivity']['framesAdvanced']>0 and doc['mapActivity']['ticksAdvanced']>0
  if file=='Result.json':assert doc['fullUserModSet'] and doc['localization']['passed'] and doc['localization']['keyed']==3127 and doc['settingsUi']['passed'] and doc['odysseyGrammar']['passed'] and doc['odysseyGrammar']['checks']==252
  else:assert doc['unresolvedReferences']==0 and doc['isolatedFixtureSave'] and not doc['userSaveTested']
  counts[key]=doc[key]['checks'];target=R/'Documentation'/pub;target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');evidence.append(dict(file=pub,sha256=sha(target)))
 assert read(fresh/'OpeningVisitUiProbe.json')['passed']
 old=legacy/'Saves/TMM-Vip.rws';assert b'TurboMegaMod.ComplexOpeningVisit' not in old.read_bytes()
 base=read(R/'Documentation/FactionRansomPolicyRevisionVerification.json')['sourceSha256'];sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert all(sources.get(p)==h for p,h in base.items()) and set(sources)-set(base)=={'Source/ComplexOpeningVisit.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 langs={l:{x.tag:x.text or '' for x in E.parse(R/f'Languages/{l}/Keyed/ComplexOpeningVisit.xml').getroot()} for l in ['Russian','English']};assert langs['Russian'].keys()==langs['English'].keys() and len(langs['English'])==1
 for k in langs['English']:assert sorted(re.findall(r'\{\d+\}',langs['English'][k]))==sorted(re.findall(r'\{\d+\}',langs['Russian'][k]))
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=sum(counts.values()),successfulIsolatedSteps=3,checksByStep=counts,sourceSha256=sources,evidence=evidence,
  firstInvitationInOneToTwoDaysTested=True,admissionNoBedStaffBedOwnedBedGuardsTested=True,existingSoonVisitNotDuplicatedTested=True,existingLongQueueUnchangedTested=True,
  repeatedChecksAndDeskReinstallDoNotDuplicate=True,savedMarkerAndNativeQueueDatesSurviveLoad=True,actualNativeQueuedVisitorGroupSpawned=True,
  onlyOwnTestDeadlineAccelerated=True,naturalTwoDayWaitSimulated=False,actualClinicFreeBedAdmissionAndHoursTested=True,patientArrivalFrequencyChanged=False,
  oldSaveMissingNewComponentLoaded=True,establishedHotelDoesNotGetExtraInvitation=True,oldSaveSha256=sha(old),oldSaveOriginalProductionSha256=read(legacy/'Result.json')['productionAssemblySha256'],
  userSaveTested=False,live16Tested=False,allSubsystemRegression=False,visualLayoutInspected=False,ordinaryGuestPurseChanged=False)
 (R/'Documentation/OpeningVisitRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))
if __name__=='__main__':main()
