"""Require successful same-binary VIP and affected service evidence."""
import hashlib
import json
from pathlib import Path
from audit_localization import entries

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'Documentation'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    binary=ROOT/'Assemblies/TurboMegaMod.dll'
    digest=sha(binary)
    assert not (ROOT/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    assert all(p.stat().st_mtime<=binary.stat().st_mtime for p in (ROOT/'Source').rglob('*.cs'))
    progress=read(ROOT/'TestRuns/VipRegressionProgress.json')
    steps=[('Vip','vip','Result.json'),('Vip reload','vipReload','ReloadResult.json'),('Vip migration','vipReload','MigrationResult.json'),
           ('Dossier','dossier','Result.json'),('Dossier reload','dossierReload','ReloadResult.json'),
           ('Hospitality','hospitality','Result.json'),('Hospitality reload','hotelReload','ReloadResult.json'),
           ('Restaurant','restaurant','Result.json'),('Restaurant reload','restaurantReload','ReloadResult.json')]
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert progress['productionAssemblySha256']==digest
    assert [r['name'] for r in progress['completed']]==[s[0] for s in steps]
    checks=0; evidence=[]; counts={}
    for record,(name,key,file) in zip(progress['completed'],steps):
        profile=Path(record['profile']);assert profile.resolve().is_relative_to((ROOT/'TestRuns').resolve())
        path=profile/file; report=read(path)
        published=DOCS/('Vip-Migration.json' if name=='Vip migration' else
                       name.split()[0]+('-Reload.json' if file=='ReloadResult.json' else '-FullSet.json'))
        assert report==read(published),name
        assert report['passed'] and report['runtimeErrors']==0 and not report['gpuLost'],name
        assert report['productionAssemblySha256']==digest,name
        assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
        assert report[key]['passed'] and not report[key]['errors']
        if file=='Result.json':
            assert report['fullUserModSet'] and not report['originalComponentActive'] and report['localization']['passed'] and report['settingsUi']['passed']
            assert [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2912,701,90]
            assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks']==252
        else:
            assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
        if name=='Vip migration':assert report['migrationFromMissingProfileFields']
        checks+=report[key]['checks'];counts[name]=report[key]['checks']
        evidence.append(dict(path=str(path.relative_to(ROOT)),sha256=sha(path)))
        evidence.append(dict(path=str(published.relative_to(ROOT)),sha256=sha(published)))
    assert counts['Vip']>=47 and counts['Vip reload']>=8 and counts['Vip migration']>=11
    rules=read(DOCS/'VipRuleVerification.json');assert rules['passed'] and rules['cases']==38
    assert rules['sourceSha256']==sha(ROOT/'Source/GuestProfileRules.cs')
    assert rules['vectorsSha256']==sha(ROOT/'Tests/VipLoyaltyCases.json')
    hooks=read(DOCS/'VipHookVerification.json');assert hooks['passed'] and hooks['checks']==14 and hooks['metadataOnly']
    utility=read(DOCS/'UtilityVerification.json');assert utility['passed'] and utility['resourceHashesVerified']==4456
    russian,duplicates=entries([ROOT/'Languages/Russian']);assert not duplicates and len(russian)==3626
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(steps),runtimeChecks=checks,checksByStep=counts,
                actualCSharpRuleCases=rules['cases'],nativeHookMetadataChecks=14,resourceHashesVerified=4456,russianEntries=len(russian),
                vipImplemented=True,regularGuestsImplemented=True,defaults=dict(vipChancePercent=3,targetPurse=[1500,3000],reservedCapacityKg=3,privateRoomImpressiveness=85,
                minimumObservedLodgingTicks=15000,completedStays=3,goodStays=2,goodNativeScore=0.6,regularSelectionChance=0.5),
                financialEvidence='Native GiveItems then capacity-limited physical grant; native 600-silver check-in and actual billing/receipt. Normal service regressions verify existing income paths.',
                timeEvidence='Actual observed tick accrual plus controlled settlement state transitions; no claim of three full naturally elapsed guest visits.',
                migrationEvidence='An isolated fixture copy has non-VIP profile/experience nodes removed, then loads with empty defaults while another saved VIP retains its data.',
                baselineCommit='5224f39',fullAllSubsystemRegressionOnThisAssembly=False,liveGame16Verified=False,visualLayoutInspected=False,userSaveTested=False,
                worldPawnRetentionGuaranteed=False,storyIncidentsImplemented=False,
                sourceSha256={str(p.relative_to(ROOT)):sha(p) for p in sorted((ROOT/'Source').rglob('*.cs'))},evidence=evidence)
    (DOCS/'VipStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))

if __name__=='__main__':
    main()
