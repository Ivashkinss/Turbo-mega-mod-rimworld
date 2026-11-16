"""Verify targeted carry billing evidence and unchanged outdoor-stage sources."""
import hashlib
import json
from pathlib import Path

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
    baseline=read(DOCS/'OutdoorStageVerification.json');assert baseline['passed'] and baseline['runtimeChecks']==200
    current={str(p.relative_to(ROOT)):sha(p) for p in sorted((ROOT/'Source').rglob('*.cs'))}
    assert set(current)==set(baseline['sourceSha256'])
    changed=[k.replace('\\','/') for k,v in current.items() if v!=baseline['sourceSha256'][k]]
    assert changed==['Source/HotelBilling.cs'],changed
    for resource,value in baseline['runtimeResourceSha256'].items():assert sha(ROOT/resource)==value
    progress=read(ROOT/'TestRuns/HotelCarryRegressionProgress.json')
    steps=[('Hospitality','hospitality','Result.json'),('Hospitality reload','hotelReload','ReloadResult.json'),
           ('ClinicGuest','clinicGuest','Result.json'),('ClinicGuest reload','clinicguestReload','ReloadResult.json')]
    assert progress['passed'] and progress['current'] is None and progress['error'] is None and progress['productionAssemblySha256']==digest
    assert [r['name'] for r in progress['completed']]==[s[0] for s in steps]
    checks=0;counts={};evidence=[]
    for record,(name,key,file) in zip(progress['completed'],steps):
        profile=Path(record['profile']);assert profile.resolve().is_relative_to((ROOT/'TestRuns').resolve())
        path=profile/file;report=read(path)
        published=DOCS/(name.split()[0]+('-Reload.json' if file=='ReloadResult.json' else '-FullSet.json'))
        assert report==read(published) and report['passed'] and report['productionAssemblySha256']==digest
        assert report['runtimeErrors']==0 and not report['gpuLost'] and report[key]['passed'] and not report[key]['errors']
        assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
        if file=='Result.json':
            assert report['fullUserModSet'] and report['localization']['passed'] and report['settingsUi']['passed']
            assert [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2925,702,90]
        else:assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
        checks+=report[key]['checks'];counts[name]=report[key]['checks']
        evidence.extend([dict(path=str(p.relative_to(ROOT)),sha256=sha(p)) for p in (path,published)])
    assert counts['Hospitality']>=47 and counts['Hospitality reload']>=15
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=4,runtimeChecks=checks,checksByStep=counts,
                changesFromOutdoorStage=changed,outdoorBaselineCommit='1bb6de9',outdoorBaselineAssemblySha256=baseline['productionAssemblySha256'],
                unchangedNonBillingProductionSources=True,carriedStayRetainedByMapHeld=True,heldGuestSaveLoadVerified=True,
                noCollectionWhileUnspawnedVerified=True,absoluteDeadlineAndPartialPaymentPreserved=True,
                restoredGuestUsesOriginalRoom=True,realPostLoadDailyPaymentAndDedupeVerified=True,
                testNote='Held overdue/partial state is exercised through direct ProcessPayments calls. A real fixture save serializes the guest inside the carrier and reloads before physical payment. All native care checks still use actual tending/feeding.',
                fullAllSubsystemRegressionOnThisAssembly=False,liveGame16Verified=False,userSaveTested=False,visualLayoutInspected=False,
                sourceSha256=current,evidence=evidence)
    (DOCS/'HotelCarryStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))

if __name__=='__main__':
    main()
