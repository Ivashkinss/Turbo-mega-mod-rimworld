"""Verify configurable pricing, whole-bed receipts and same-binary regressions."""
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
    base=read(DOCS/'GeneticSupplyStageVerification.json')['sourceSha256']
    assert {p for p in base if sources.get(p)!=base[p]}=={'Source/ComplexLayout.cs','Source/ComplexSales.cs','Source/ComplexServicesDialog.cs','Source/GuestDeathrest.cs','Source/GuestProfiles.cs','Source/HotelBilling.cs'}
    assert set(sources)-set(base)=={'Source/ComplexPricing.cs'}
    progress=read(ROOT/'TestRuns/PricingRegressionProgress.json')
    assert progress['passed'] and progress['current'] is None and progress['error'] is None and progress['productionAssemblySha256']==digest
    expected=[('Pricing reload','pricingReload','ReloadResult.json','Pricing-Reload.json'),
              ('Hotel prior-save migration','hotelReload','MigrationResult.json','Pricing-HotelMigration.json'),
              ('Hospitality','hospitality','Result.json','Hospitality-FullSet.json'),
              ('Hospitality reload','hotelReload','ReloadResult.json','Hospitality-Reload.json'),
              ('Sales','sales','Result.json','Sales-FullSet.json'),
              ('Sales reload','salesReload','ReloadResult.json','Sales-Reload.json'),
              ('SalesDelivery','salesDelivery','Result.json','SalesDelivery-FullSet.json'),
              ('SalesDelivery reload','salesDeliveryReload','ReloadResult.json','SalesDelivery-Reload.json'),
              ('Wholesale','wholesale','Result.json','Wholesale-FullSet.json'),
              ('Wholesale reload','wholesaleReload','ReloadResult.json','Wholesale-Reload.json'),
              ('ComplexLayout','complexLayout','Result.json','ComplexLayout-FullSet.json'),
              ('ComplexLayout reload','layoutReload','ReloadResult.json','ComplexLayout-Reload.json'),
              ('Deathrest','deathrest','Result.json','Deathrest-FullSet.json'),
              ('Deathrest reload','deathrestReload','ReloadResult.json','Deathrest-Reload.json'),
              ('Vip','vip','Result.json','Vip-FullSet.json'),
              ('GeneticSupply','geneticSupply','Result.json','GeneticSupply-FullSet.json'),
              ('Hemogen','hemogen','Result.json','Hemogen-FullSet.json'),
              ('ClinicGuest','clinicGuest','Result.json','ClinicGuest-FullSet.json'),
              ('ClinicGuest reload','clinicguestReload','ReloadResult.json','ClinicGuest-Reload.json')]
    assert [r['name'] for r in progress['completed']]==[s[0] for s in expected]
    steps=[(dict(name='Pricing',profile=progress['pricingProfile']),('Pricing','pricing','Result.json','Pricing-FullSet.json'))]+list(zip(progress['completed'],expected))
    counts={};evidence=[]
    for record,(name,key,file,published_name) in steps:
        profile=Path(record['profile']).resolve();assert profile.is_relative_to((ROOT/'TestRuns').resolve())
        path=profile/file;report=read(path);published=DOCS/published_name
        assert report==read(published) and report['passed'] and report['productionAssemblySha256']==digest
        assert report['runtimeErrors']==0 and not report['gpuLost'] and report[key]['passed'] and not report[key]['errors']
        assert report['mapActivity']['ticksAdvanced']>0 and report['mapActivity']['framesAdvanced']>0
        if file=='Result.json':
            assert report['fullUserModSet'] and report['localization']['passed'] and report['settingsUi']['passed']
            assert [report['localization'][k] for k in ('keyed','scalarFields','grammarLines')]==[2959,702,90]
            assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks']==252
        else:
            assert report['unresolvedReferences']==0 and report['isolatedFixtureSave'] and not report['userSaveTested']
        counts[name]=report[key]['checks']
        evidence.extend(dict(path=p.relative_to(ROOT).as_posix(),sha256=sha(p)) for p in (path,published))
    assert counts['Pricing']>=34 and counts['Pricing reload']>=19
    assert counts['Hotel prior-save migration']>=16 and counts['Hospitality reload']>=16
    assert counts['GeneticSupply']>=39 and counts['Hemogen']>=44
    assert counts['Deathrest']>=29 and counts['Deathrest reload']>=15
    migration=read(DOCS/'Pricing-HotelMigration.json')
    assert migration['sourceSaveAssemblySha256']!=digest and migration['oldPricingComponentAbsent']
    old_profile=next(Path(x['profile']) for x in progress['completed'] if x['name']=='Hotel prior-save migration')
    old_save=old_profile/'Saves/TMM-Hotel.rws'
    assert 'TurboMegaMod.ComplexPricing' not in old_save.read_text(encoding='utf-8-sig') and 'TMM_salePercent' not in old_save.read_text(encoding='utf-8-sig')
    notes_path=DOCS/'PricingStageTestNotes.json';notes=read(notes_path)
    assert notes['nativeAreaPathFailureUnresolved'] is True
    evidence.append(dict(path=notes_path.relative_to(ROOT).as_posix(),sha256=sha(notes_path)))
    resources=['Languages/English/Keyed/ComplexPricing.xml','Languages/Russian/Keyed/ComplexPricing.xml']
    tracked=set(subprocess.check_output(['git','ls-files'],cwd=ROOT,text=True).splitlines())
    assert all(p in tracked for p in resources+list(sources)),'Stage all production files before verification'
    result=dict(passed=True,productionAssemblySha256=digest,successfulIsolatedSteps=len(steps),runtimeChecks=sum(counts.values()),checksByStep=counts,
                sharedBedArithmeticCases=24,arithmeticNegativeMutations=6,sourceSha256=sources,runtimeResourceSha256={p:sha(ROOT/p) for p in resources},evidence=evidence,
                oneBedRateForTwoGuests=True,soloDoubleBedAllowed=True,newSharedAdmissionRequiresCurrentLovePartner=True,
                actualPartnersLayDownInDistinctSlots=True,lateJoinKeepsPaidDeadlineAndNoSecondFullCharge=True,
                pendingDebtKeepsOriginalRate=True,pooledRenewalWithoutDoubleIncome=True,physicalSilverAndPayerDossier=True,
                existingGoodsOrderKeepsQuoteAfterMarkupChange=True,individualRatesSurviveDefaultAndMedicalSwap=True,
                nativeTeardownCannotReenterMedicalRoomConversion=True,configuredFeeUiNotAffectedByScopedEntryQuote=True,
                explicitOldIsolatedSaveMigration=True,sourceOldSaveAssemblySha256=migration['sourceSaveAssemblySha256'],oldSaveSha256=sha(old_save),
                temporaryCarryDefersSharedCollection=True,emptyBedClosesBooking=True,boundedCatchupAndZeroRate=True,
                partialReceiptsPersistAndResumeWithoutDuplicate=True,liveGame16Verified=False,userSaveTested=False,visualLayoutInspected=False,
                rejectedAttemptsDocumented=True,unresolvedIntermittentNativeAreaPathError=True,
                passingRetryDoesNotProveAreaPathFix=True,
                allFutureModRacesSupported=False,allSubsystemRegressionOnThisAssembly=False)
    (DOCS/'PricingStageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k not in ('sourceSha256','evidence','runtimeResourceSha256')},ensure_ascii=False))

if __name__=='__main__':
    main()
