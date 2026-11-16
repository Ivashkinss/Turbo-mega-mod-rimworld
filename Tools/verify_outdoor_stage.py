"""Validate current-binary outdoor, transport and affected guest evidence."""
import hashlib
import json
from pathlib import Path
import subprocess
from audit_localization import entries

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'Documentation'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    binary=ROOT/'Assemblies/TurboMegaMod.dll';digest=sha(binary)
    assert not (ROOT/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    assert all(p.stat().st_mtime<=binary.stat().st_mtime for p in (ROOT/'Source').rglob('*.cs'))
    progress=read(ROOT/'TestRuns/OutdoorRegressionProgress.json')
    pairs=[('Outdoor','outdoor','outdoorReload'),('Dossier','dossier','dossierReload'),('Hospitality','hospitality','hotelReload'),
           ('Vip','vip','vipReload'),('ClinicGuest','clinicGuest','clinicguestReload')]
    steps=[(name+(' reload' if reload else ''),reloadkey if reload else key,'ReloadResult.json' if reload else 'Result.json')
           for name,key,reloadkey in pairs for reload in (False,True)]
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert progress['productionAssemblySha256']==digest
    assert [r['name'] for r in progress['completed']]==[s[0] for s in steps]
    checks=0;counts={};evidence=[]
    for record,(name,key,file) in zip(progress['completed'],steps):
        profile=Path(record['profile']);assert profile.resolve().is_relative_to((ROOT/'TestRuns').resolve())
        path=profile/file;report=read(path)
        published=DOCS/(name.split()[0]+('-Reload.json' if file=='ReloadResult.json' else '-FullSet.json'))
        assert report==read(published),name
        assert report['passed'] and report['runtimeErrors']==0 and not report['gpuLost'] and report['productionAssemblySha256']==digest,name
        assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
        assert report[key]['passed'] and not report[key]['errors']
        if file=='Result.json':
            assert report['fullUserModSet'] and not report['originalComponentActive'] and report['localization']['passed'] and report['settingsUi']['passed']
            assert [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2925,702,90]
            assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks']==252
        else:
            assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
        checks+=report[key]['checks'];counts[name]=report[key]['checks']
        evidence.extend([dict(path=str(p.relative_to(ROOT)),sha256=sha(p)) for p in (path,published)])
    assert counts['Outdoor']>=27 and counts['Outdoor reload']>=4
    russian,duplicates=entries([ROOT/'Languages/Russian']);assert not duplicates and len(russian)==3640
    resources=['Defs/Complex/GuestOutdoor.xml','Languages/English/Keyed/GuestOutdoor.xml',
               'Languages/Russian/Keyed/GuestOutdoor.xml','Languages/Russian/DefInjected/JobDef/GuestOutdoor.xml']
    for resource in resources:
        subprocess.run(['git','ls-files','--error-unmatch','--',resource],cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
    utility=read(DOCS/'UtilityVerification.json');assert utility['passed'] and utility['resourceHashesVerified']==4456
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(steps),runtimeChecks=checks,checksByStep=counts,
                defaults=dict(outdoorBreaks=True,lowLevel=0.35,finishLevel=0.7,maxBreakTicks=2500,minimumFood=0.55,minimumRest=0.4,
                              searchRadius=60,searchCellLimit=6000,retryTicks=1000),
                naturalNeedRecoveryVerified=True,nativeNeedEligibilityUnchanged=True,disablingBreaksPreservesNativeNeed=True,
                clinicalNeedPauseIncludingCarriedGuestVerified=True,nativeGuestRegistryRestoredAfterTransport=True,
                needsWindowUsesActualNeedTracker=True,runtimeResourcesTracked=True,
                runtimeResourceSha256={p:sha(ROOT/p) for p in resources},russianEntries=len(russian),resourceHashesVerified=4456,
                scope='Physical courtyard travel and native need recovery; native health eligibility, public-only search, hunger/rest/clinical and toxic-fallout guards, carried-patient pause/registry restoration, save/reload, needs/reception repaint API, dossier/hotel/VIP/clinic guest regressions.',
                testEnvironment='Only disposable test maps clear fog/hostile targets and set stable clear weather at21C. Original game files, user settings and user saves are not changed.',
                limits=['No new satisfaction override or ShouldHaveNeed patch; hosted guests can already have Outdoors.','No guarantee of all modded race/DLC needs being serviced.','Safety uses native danger/weather/threat checks; it is not a proof against all future hazards.','The public BFS is bounded; large complexes need nearby courtyards.'],
                baselineCommit='dcfd791',fullAllSubsystemRegressionOnThisAssembly=False,liveGame16Verified=False,userSaveTested=False,visualLayoutInspected=False,
                deathrestGuestSupportImplemented=False,storyIncidentsImplemented=False,
                sourceSha256={str(p.relative_to(ROOT)):sha(p) for p in sorted((ROOT/'Source').rglob('*.cs'))},evidence=evidence)
    (DOCS/'OutdoorStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))

if __name__=='__main__':
    main()
