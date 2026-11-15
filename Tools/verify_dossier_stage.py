"""Require same-binary dossier, admission and affected service evidence."""
import hashlib
import json
from pathlib import Path
from audit_localization import entries

PROJECT=Path(__file__).resolve().parents[1]
DOCS=PROJECT/'Documentation'
COMPONENTS=[('Dossier','dossier','dossierReload'),('Hospitality','hospitality','hotelReload'),
            ('Restaurant','restaurant','restaurantReload'),('Company','company','companyReload'),
            ('Clinic','clinic','clinicReload'),('ClinicGuest','clinicGuest','clinicguestReload'),
            ('Sales','sales','salesReload'),('SalesDelivery','salesDelivery','salesDeliveryReload'),
            ('Wholesale','wholesale','wholesaleReload'),('Complex','complex','complexReload'),
            ('Farming','farming','farmingReload'),('ComplexLayout','complexLayout','layoutReload')]
COMPONENTS=[COMPONENTS[-1],COMPONENTS[-2]]+COMPONENTS[:-2]


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    binary=PROJECT/'Assemblies/TurboMegaMod.dll'
    digest=sha(binary)
    assert all(p.stat().st_mtime<=binary.stat().st_mtime for p in (PROJECT/'Source').rglob('*.cs'))
    assert not (PROJECT/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    progress=read(PROJECT/'TestRuns/DossierRevisionProgress.json')
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert progress['productionAssemblySha256']==digest
    expected=[s for name,_,_ in COMPONENTS for s in (name,name+' reload')]
    assert [s['name'] for s in progress['completed']]==expected
    checks,components,evidence=0,{},[]
    for index,(name,key,reload_key) in enumerate(COMPONENTS):
        profile=Path(progress['completed'][index*2]['profile'])
        assert profile.resolve().is_relative_to((PROJECT/'TestRuns').resolve())
        assert profile==Path(progress['completed'][index*2+1]['profile'])
        components[name]={}
        for reload,label,file in [(False,'fresh','Result.json'),(True,'reload','ReloadResult.json')]:
            path=profile/file
            report=read(path)
            published=DOCS/(name+('-Reload.json' if reload else '-FullSet.json'))
            assert report==read(published),name
            assert report['passed'] and report['runtimeErrors']==0 and not report['gpuLost'],name
            assert report['productionAssemblySha256']==digest,name
            assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
            node=reload_key if reload else key
            assert report[node]['passed'] and not report[node]['errors']
            if reload:
                assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
            else:
                assert report['fullUserModSet'] and not report['originalComponentActive'] and report['settingsUi']['passed']
                assert report['localization']['passed'] and [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2904,691,90]
                assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks']==252
            components[name][label]=report[node]['checks'];checks+=report[node]['checks']
            evidence.extend([dict(path=str(path.relative_to(PROJECT)),sha256=sha(path)),dict(path=str(published.relative_to(PROJECT)),sha256=sha(published))])
    assert components['Dossier']['fresh']>=30 and components['Dossier']['reload']>=8
    russian,duplicates=entries([PROJECT/'Languages/Russian'])
    assert not duplicates and len(russian)==3608
    utility=read(DOCS/'UtilityVerification.json')
    assert utility['passed'] and utility['resourceHashesVerified']==4456
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(expected),runtimeChecks=checks,components=components,
        guestDossiersImplemented=True,personalBlacklistImplemented=True,manualGreenhousePartsImplemented=True,automaticExpansionRetired=True,unifiedReceptionPages=5,nativeGuestAndPatientBottomTabsHidden=True,patientAdmissionHoursVisible=24,historyLimits=dict(visitsPerGuest=50,paymentsPerGuest=200),
        lifetimeTotalsRetained=True,receiptDedupeTokensRetained=True,worldPawnRetentionGuaranteed=False,
        scope='Native arrival/departure and admission exceptions, lost-reference payment links and actual service income; five reception pages and rosters, native hour controls with persistence; manual glass roof, panel binding, room restoration, heating/cooling, conduit connection and legacy expansion cancellation/refund.',
        financialTestNote='The dossier probe uses synthetic ledger entries for deduplication. Actual hotel/restaurant/conversation/shop/clinical income is separately cross-checked in the service probes.',
        fullRegressionOnThisAssembly=False,baselineMedicalCommit='893a7bc',baselineFullRegressionCommit='04729c6e6d3f4ae359d6c2a3e094458c025b2745',
        vipImplemented=False,storyIncidentsImplemented=False,allInboundTransportModsTested=False,
        userSaveTested=False,liveGame16Verified=False,visualLayoutInspected=False,russianEntries=len(russian),
        sourceSha256={str(p.relative_to(PROJECT)):sha(p) for p in sorted((PROJECT/'Source').rglob('*.cs'))},evidence=evidence)
    (DOCS/'DossierStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence')},ensure_ascii=False))


if __name__=='__main__':
    main()
