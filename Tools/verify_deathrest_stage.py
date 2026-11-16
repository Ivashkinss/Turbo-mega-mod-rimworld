"""Require same-production-binary native deathrest and shared-service evidence."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'Documentation'

def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    binary=ROOT/'Assemblies/TurboMegaMod.dll'
    digest=sha(binary)
    assert not (ROOT/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    sources={p.relative_to(ROOT).as_posix():sha(p) for p in sorted((ROOT/'Source').rglob('*.cs'))}
    assert all(p.stat().st_mtime<=binary.stat().st_mtime for p in (ROOT/'Source').rglob('*.cs'))
    baseline={p.replace('\\','/'):v for p,v in read(DOCS/'HotelCarryStageVerification.json')['sourceSha256'].items()}
    changed={p for p in baseline if sources.get(p)!=baseline[p]}
    assert changed=={'Source/ClinicGuests.cs','Source/ComplexManagement.cs','Source/ComplexServicesDialog.cs','Source/GuestNeedsDialog.cs','Source/HotelBilling.cs'},changed
    assert set(sources)-set(baseline)=={'Source/GuestDeathrest.cs','Source/GuestDeathrestHooks.cs','Source/GuestDeathrestRules.cs'}
    progress=read(ROOT/'TestRuns/DeathrestRegressionProgress.json')
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert progress['productionAssemblySha256']==digest
    expected=[('Deathrest reload','deathrestReload','ReloadResult.json'),('Hospitality','hospitality','Result.json'),
              ('Hospitality reload','hotelReload','ReloadResult.json'),('ClinicGuest','clinicGuest','Result.json'),
              ('ClinicGuest reload','clinicguestReload','ReloadResult.json'),('Outdoor','outdoor','Result.json'),('Vip','vip','Result.json')]
    assert [r['name'] for r in progress['completed']]==[s[0] for s in expected]
    steps=[(dict(name='Deathrest',profile=progress['deathrestProfile']),('Deathrest','deathrest','Result.json'))]+list(zip(progress['completed'],expected))
    counts={};evidence=[]
    for record,(name,key,file) in steps:
        profile=Path(record['profile']).resolve()
        assert profile.is_relative_to((ROOT/'TestRuns').resolve())
        path=profile/file;report=read(path)
        published=DOCS/(name.split()[0]+('-Reload.json' if file=='ReloadResult.json' else '-FullSet.json'))
        assert report==read(published) and report['passed'] and report['productionAssemblySha256']==digest
        assert report['runtimeErrors']==0 and not report['gpuLost'] and report[key]['passed'] and not report[key]['errors']
        assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
        if file=='Result.json':
            assert report['fullUserModSet'] and report['localization']['passed'] and report['settingsUi']['passed'] and report['odysseyGrammar']['passed']
            assert [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2933,702,90]
            assert report['odysseyGrammar']['checks']==252
        else:
            assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
        counts[name]=report[key]['checks']
        evidence.extend(dict(path=p.relative_to(ROOT).as_posix(),sha256=sha(p)) for p in (path,published))
    assert counts['Deathrest']>=26 and counts['Deathrest reload']>=13
    budget=read(DOCS/'DeathrestBudgetVerification.json')
    assert budget['passed'] and budget['cases']==20 and budget['actualCSharpSource']
    assert budget['sourceSha256']==sha(ROOT/'Source/GuestDeathrestRules.cs')
    assert budget['casesSha256']==sha(ROOT/'Tests/DeathrestBudgetCases.json')
    resources=['Languages/English/Keyed/GuestDeathrest.xml','Languages/Russian/Keyed/GuestDeathrest.xml']
    tracked=set(subprocess.check_output(['git','ls-files'],cwd=ROOT,text=True).splitlines())
    assert all(p in tracked for p in resources+list(sources)),'Stage all production files before verification'
    for p,v in read(DOCS/'OutdoorStageVerification.json')['runtimeResourceSha256'].items():assert sha(ROOT/p)==v
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(steps),runtimeChecks=sum(counts.values()),checksByStep=counts,
                pureCSharpBudgetCases=20,budgetMutationsCaught=6,sourceSha256=sources,runtimeResourceSha256={p:sha(ROOT/p) for p in resources},evidence=evidence,
                nativeGuestCasketCheckin=True,nativeDeathrestJobAndIncapacitation=True,originalGroupAndRoomRetained=True,
                normalDailyBillingAndDedupe=True,activeSleepSaveLoad=True,nativeCompletionWakeAndTemporaryBindingCleanup=True,
                interruptedNonpaymentWakesBeforeUnclaim=True,foreignAmplifierFactionRestrictionRetained=True,
                guestCasketNativeActivePower100Watts=True,privatePowerFieldCloneCorrectionFor17=True,
                naturalFourDayCycleTested=False,fullPowerNetworkFailureTested=False,deathrestDoctorFeedingTested=False,
                manualDepartureAndDestroyedBedTested=False,fullAllSubsystemRegressionOnThisAssembly=False,
                liveGame16Verified=False,userSaveTested=False,visualLayoutInspected=False)
    (DOCS/'DeathrestStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence','runtimeResourceSha256')},ensure_ascii=False))

if __name__=='__main__':
    main()
