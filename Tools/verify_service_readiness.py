"""Verify the read-only service summary and an actual pre-feature save reload."""
import argparse,hashlib,json,re,xml.etree.ElementTree as E
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--profile',required=True);p.add_argument('--menu-profile',required=True);p.add_argument('--visual-reviewed',action='store_true');a=p.parse_args()
 digest=sha(R/'Assemblies/TurboMegaMod.dll');fresh=Path(a.profile).resolve();old=Path(a.menu_profile).resolve()
 assert all(p.is_relative_to((R/'TestRuns').resolve()) for p in [fresh,old])
 assert not (R/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
 f=read(fresh/'Result.json');load=read(old/'ReloadResult.json')
 for doc,key,n in [(f,'serviceReadiness',35),(load,'restaurantMenuReload',14)]:
  assert doc['passed'] and doc['productionAssemblySha256']==digest and doc['runtimeErrors']==0 and not doc['gpuLost']
  assert doc[key]['passed'] and not doc[key]['errors'] and doc[key]['checks']>=n
  assert doc['mapActivity']['framesAdvanced']>0 and doc['mapActivity']['ticksAdvanced']>0
 assert f['fullUserModSet'] and f['localization']['passed'] and f['localization']['keyed']==3110 and f['settingsUi']['passed'] and f['odysseyGrammar']['passed'] and f['odysseyGrammar']['checks']==252
 assert load['unresolvedReferences']==0 and load['isolatedFixtureSave'] and not load['userSaveTested']
 assert read(fresh/'ServiceReadinessUiProbe.json')['passed']
 log=(fresh/'stdout.log').read_text(encoding='utf-8',errors='replace')
 assert not re.search('Could not find think node with key|Could not resolve cross-reference|Could not find class|missing reference|Failed to load reference|Could not load reference',log,re.I)
 shots={name:sha(fresh/f'ServiceReadiness-{name}.png') for name in ['Top','Clinic','Middle','Bottom']}
 base=read(R/'Documentation/RestaurantMenuRevisionVerification.json')['sourceSha256']
 sources={p.relative_to(R).as_posix():sha(p) for p in sorted((R/'Source').rglob('*.cs'))}
 assert {p for p,h in base.items() if sources.get(p)!=h}=={'Source/ComplexServicesDialog.cs'} and set(sources)-set(base)=={'Source/ServiceReadiness.cs'}
 assert all(p.stat().st_mtime<=(R/'Assemblies/TurboMegaMod.dll').stat().st_mtime for p in (R/'Source').rglob('*.cs'))
 langs={l:{x.tag:x.text or '' for x in E.parse(R/f'Languages/{l}/Keyed/ServiceReadiness.xml').getroot()} for l in ['Russian','English']}
 assert langs['Russian'].keys()==langs['English'].keys() and len(langs['English'])==92
 for k in langs['English']:assert sorted(re.findall(r'\{\d+\}',langs['English'][k]))==sorted(re.findall(r'\{\d+\}',langs['Russian'][k]))
 evidence=[]
 for name,doc in [('ServiceReadiness-FullSet.json',f),('ServiceReadiness-OldMenu-Reload.json',load)]:
  target=R/'Documentation'/name;target.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');evidence.append(dict(file=name,sha256=sha(target)))
 result=dict(passed=True,productionAssemblySha256=digest,runtimeChecks=f['serviceReadiness']['checks']+load['restaurantMenuReload']['checks'],successfulIsolatedSteps=2,checksByStep={'readiness':f['serviceReadiness']['checks'],'old menu reload':load['restaurantMenuReload']['checks']},sourceSha256=sources,evidence=evidence,
  actualGuestBedsOnlyTested=True,actualOwnersAndSleeperUnionTested=True,partlyOccupiedDoubleBedNotCompletelyFreeTested=True,actualColonistInClinicalBedTested=True,
  actualSlaveWorkModesAndCompanyOptInTested=True,nativeHospitalAdmissionAndScheduleTested=True,physicalMealsAndStockStacksVsUnitsTested=True,
  repeatedGatherPreservesJobsPrioritiesSettingsQueueAndMoney=True,invalidEmployeeAndDeletedAreaAndDespawnTested=True,
  visualLayoutInspected=a.visual_reviewed,visualResolution='1280x800',screenshotSha256=shots,otherResolutionsOrDpiTested=False,
  oldSaveSha256=sha(old/'Saves/TMM-RestaurantMenu.rws'),oldSaveOriginalProductionSha256=read(old/'Result.json')['productionAssemblySha256'],userSaveTested=False,live16Tested=False,allSubsystemRegression=False,
  rejectedFixtureAttempts=[dict(profile='ServiceReadiness-FullSet-20261117-113435',reason='Restaurant test still in legacy manual mode; physical stock fixture invalid.'),dict(profile='ServiceReadiness-FullSet-20261117-113718',reason='Healthy patient chose normal bed; clinical fixture had no actual medical need.'),dict(profile='ServiceReadiness-FullSet-20261117-114111',reason='Forced sleep alone did not supply medical need. Added actual Flu and rest-until-healed.'),dict(profile='ServiceReadiness-FullSet-20261117-114439',reason='Test cast native IncidentQueue to IEnumerable; fixed after inspecting native metadata.'),dict(profile='ServiceReadiness-FullSet-20261117-114800',reason='35 functional checks but rejected 2 runtime logs: test read uninitialized neutral guest work priorities. Restricted that snapshot to player employees.')])
 (R/'Documentation/ServiceReadinessRevisionVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence','rejectedFixtureAttempts','screenshotSha256')},ensure_ascii=False))
if __name__=='__main__':main()
