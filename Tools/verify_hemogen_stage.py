"""Require one binary, native ingestion/save keys, transactions and duty regressions."""
import hashlib
import json
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'Documentation'

def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    binary=ROOT/'Assemblies/TurboMegaMod.dll';digest=sha(binary)
    assert not (ROOT/'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    sources={p.relative_to(ROOT).as_posix():sha(p) for p in sorted((ROOT/'Source').rglob('*.cs'))}
    assert all(p.stat().st_mtime<=binary.stat().st_mtime for p in (ROOT/'Source').rglob('*.cs'))
    base=read(DOCS/'DeathrestStageVerification.json')['sourceSha256']
    assert {p for p in base if sources.get(p)!=base[p]}=={'Source/ComplexManagement.cs','Source/ComplexServicesDialog.cs','Source/GuestNeedsDialog.cs','Source/GuestDeathrestHooks.cs'}
    assert set(sources)-set(base)=={'Source/GuestHemogen.cs','Source/GuestDutyTrees.cs'}
    progress=read(ROOT/'TestRuns/HemogenRegressionProgress.json')
    assert progress['passed'] and progress['current'] is None and progress['error'] is None and progress['productionAssemblySha256']==digest
    expected=[('Deathrest','deathrest','Result.json','Deathrest-FullSet.json'),
              ('Deathrest reload','deathrestReload','ReloadResult.json','Deathrest-Reload.json'),
              ('Hemogen ingest reload','hemogenIngestReload','IngestReloadResult.json','Hemogen-IngestReload.json'),
              ('Hemogen reload','hemogenReload','ReloadResult.json','Hemogen-Reload.json'),
              ('SalesDelivery','salesDelivery','Result.json','SalesDelivery-FullSet.json'),
              ('SalesDelivery reload','salesDeliveryReload','ReloadResult.json','SalesDelivery-Reload.json'),
              ('ClinicGuest','clinicGuest','Result.json','ClinicGuest-FullSet.json'),
              ('ClinicGuest reload','clinicguestReload','ReloadResult.json','ClinicGuest-Reload.json'),
              ('Hospitality','hospitality','Result.json','Hospitality-FullSet.json'),
              ('Outdoor','outdoor','Result.json','Outdoor-FullSet.json')]
    assert [r['name'] for r in progress['completed']]==[s[0] for s in expected]
    steps=[(dict(name='Hemogen',profile=progress['hemogenProfile']),('Hemogen','hemogen','Result.json','Hemogen-FullSet.json'))]+list(zip(progress['completed'],expected))
    counts={};evidence=[]
    for record,(name,key,file,published_name) in steps:
        profile=Path(record['profile']).resolve();assert profile.is_relative_to((ROOT/'TestRuns').resolve())
        path=profile/file;report=read(path);published=DOCS/published_name
        assert report==read(published) and report['passed'] and report['productionAssemblySha256']==digest
        assert report['runtimeErrors']==0 and not report['gpuLost'] and report[key]['passed'] and not report[key]['errors']
        assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
        if file=='Result.json':
            assert report['fullUserModSet'] and report['localization']['passed'] and report['settingsUi']['passed']
            assert [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2938,702,90]
            assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks']==252
        else:
            assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
        counts[name]=report[key]['checks']
        evidence.extend(dict(path=p.relative_to(ROOT).as_posix(),sha256=sha(p)) for p in (path,published))
    assert counts['Hemogen']>=44 and counts['Hemogen reload']>=12 and counts['Hemogen ingest reload']>=5
    assert counts['Deathrest']>=29 and counts['Deathrest reload']>=15
    vectors=ROOT/'Tests/HemogenWantedCases.xml';assert len(ET.parse(vectors).getroot().findall('case'))==16
    resources=['Languages/English/Keyed/GuestHemogen.xml','Languages/Russian/Keyed/GuestHemogen.xml']
    tracked=set(subprocess.check_output(['git','ls-files'],cwd=ROOT,text=True).splitlines())
    assert all(p in tracked for p in resources+list(sources)),'Stage all production files before verification'
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(steps),runtimeChecks=sum(counts.values()),checksByStep=counts,
                nativeCountVectors=16,countVectorsSha256=sha(vectors),sourceSha256=sources,runtimeResourceSha256={p:sha(ROOT/p) for p in resources},evidence=evidence,
                personalPacksConsumedWithoutPayment=True,publicSaleThenAutonomousNativeIngest=True,staffStockDeliveredWithoutGuestEntry=True,
                physicalGuestDebitColonySilverAndSalesIncome=True,pendingDeliverySaveLoad=True,automaticIngestSaveLoadWithResolvedGiverKey=True,
                cancelledDeliveryPreservesGoodsAndNoIncome=True,existingDutyKeysPreservedAndRepeatedSyncIdempotent=True,
                mainThinkRootUsedByOutdoorAndDeathrestProbes=True,sourceDutyOnlyTestsWereInsufficientInPriorStages=True,guestCasketUsesRentalOwnershipAndPreservesHomeAssignment=True,
                colonySilverMeasuredIncludingPlayerInventoryAndCarry=True,physicalStackQuantitiesInsteadOfOriginalMergedIdentity=True,
                directlyInjectedHemogen=False,liveGame16Verified=False,userSaveTested=False,visualLayoutInspected=False,
                allThirdPartyHemogenItemsSupported=False,allSubsystemRegressionOnThisAssembly=False)
    (DOCS/'HemogenStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence','runtimeResourceSha256')},ensure_ascii=False))

if __name__=='__main__':
    main()
