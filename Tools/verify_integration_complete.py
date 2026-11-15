"""Verify the final installed state and collect the integration evidence."""
import json
import hashlib
import os
from pathlib import Path
from lxml import etree as ET
from audit_localization import entries

PROJECT = Path(__file__).resolve().parents[1]
DOCS = PROJECT / 'Documentation'


def read(name):
    return json.loads((DOCS / name).read_text(encoding='utf-8-sig'))


def main():
    inventory = read('inventory.json')
    planned = [m for m in inventory['mods'] if m['include']]
    additional = read('AdditionalComponents.json')['mods']
    manifest = read('UtilityComponents.json')
    expected = {m['packageId'].lower() for m in planned + additional}
    actual = {m['packageId'].lower() for m in manifest.values()} | {'sbz.neatstorage', 'sbz.neatstoragefridge'}
    assert actual == expected and len(actual) == 30, (actual ^ expected)
    for mod in planned + additional:
        assert not (PROJECT.parent / mod['folder']).exists(), ('Original still installed', mod['folder'])
    assert not (PROJECT / 'Assemblies/TurboMegaMod.TestProbe.dll').exists(), 'Test DLL left installed'
    assert not ET.parse(str(PROJECT / 'About/About.xml')).xpath('/ModMetaData/modDependencies/li')
    config_path = Path(os.environ['USERPROFILE']) / 'AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/ModsConfig.xml'
    active = ET.parse(str(config_path)).xpath('/ModsConfigData/activeMods/li/text()')
    assert active.count('colonyproject.integrated') == 1
    assert not expected.intersection(x.lower() for x in active), 'Original packageIds still enabled'
    assembly_hash = hashlib.sha256((PROJECT / 'Assemblies/TurboMegaMod.dll').read_bytes()).hexdigest()
    reports = {name: read(name + '-FullSet.json') for name in ('Harmony', 'MeleeAnimation', 'HugsLib', 'Farming', 'Hospitality', 'Restaurant', 'Complex', 'Clinic', 'ClinicGuest', 'ComplexLayout', 'Company', 'Recruit', 'Sales', 'SalesDelivery')}
    for name, report in reports.items():
        assert report['passed'] and report['unexpectedRuntimeErrors'] == 0 and not report['gpuLost'], name
        assert report['mapActivity']['ticksAdvanced'] > 0, name
        assert report['fullUserModSet'] and not report['originalComponentActive'], name
        assert report['localization']['keyed'] == 2824 and report['localization']['scalarFields'] == 691 and report['localization']['grammarLines'] == 90, name
        assert report['productionAssemblySha256'] == assembly_hash, ('Stale production binary report', name)
        assert report['knownExternalRuntimeErrors'] == 0, name
        assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks'] == 252, name
        assert not any('Field injection' in message for message in report['knownExternalMessages']), name
    related = reports['Harmony']['relatedRegressions']
    assert set(related) == {'VEF', 'MapPreview', 'AdaptiveStorage', 'CommonSense', 'HugsLib'}
    assert all(result['passed'] for result in related.values())
    assert reports['MeleeAnimation']['relatedHands']['passed']
    assert reports['HugsLib']['relatedRunAndGun']['passed']
    checks = reports['Harmony']['harmony']['checks'] + sum(value['runtime']['checks'] for value in related.values())
    checks += reports['MeleeAnimation']['melee']['checks'] + reports['MeleeAnimation']['relatedHands']['runtime']['checks']
    checks += reports['HugsLib']['hugsLib']['checks'] + reports['HugsLib']['relatedRunAndGun']['runtime']['checks']
    integration_checks = checks
    reload = read('Farming-Reload.json')
    assert reload['passed'] and reload['runtimeErrors'] == 0 and not reload['gpuLost']
    assert reload['productionAssemblySha256'] == assembly_hash
    assert reload['mapActivity']['ticksAdvanced'] > 0 and reload['isolatedFixtureSave'] and not reload['userSaveTested']
    assert read('FarmingPowerTests.json')['passed']
    checks += reports['Farming']['farming']['checks'] + reload['farmingReload']['checks']
    hotel_reload = read('Hospitality-Reload.json')
    assert hotel_reload['passed'] and hotel_reload['runtimeErrors'] == 0 and not hotel_reload['gpuLost']
    assert hotel_reload['productionAssemblySha256'] == assembly_hash
    assert hotel_reload['mapActivity']['ticksAdvanced'] > 0 and hotel_reload['isolatedFixtureSave'] and not hotel_reload['userSaveTested']
    assert hotel_reload['unresolvedReferences'] == 0
    hotel_checks = reports['Hospitality']['hospitality']['checks'] + hotel_reload['hotelReload']['checks']
    checks += hotel_checks
    restaurant_reload = read('Restaurant-Reload.json')
    assert restaurant_reload['passed'] and restaurant_reload['runtimeErrors'] == 0 and not restaurant_reload['gpuLost']
    assert restaurant_reload['productionAssemblySha256'] == assembly_hash and restaurant_reload['unresolvedReferences'] == 0
    assert restaurant_reload['isolatedFixtureSave'] and not restaurant_reload['userSaveTested']
    checks += reports['Restaurant']['restaurant']['checks'] + restaurant_reload['restaurantReload']['checks']
    complex_reload = read('Complex-Reload.json')
    assert complex_reload['passed'] and complex_reload['runtimeErrors'] == 0 and not complex_reload['gpuLost']
    assert complex_reload['productionAssemblySha256'] == assembly_hash and complex_reload['unresolvedReferences'] == 0
    assert complex_reload['isolatedFixtureSave'] and not complex_reload['userSaveTested']
    checks += reports['Complex']['complex']['checks'] + complex_reload['complexReload']['checks']
    extra_service_checks = {}
    for component, key in [('Clinic', 'clinic'), ('ClinicGuest', 'clinicguest'), ('ComplexLayout', 'layout'), ('Company', 'company'), ('Recruit', 'recruit'), ('Sales', 'sales'), ('SalesDelivery', 'salesDelivery')]:
        extra_reload = read(component + '-Reload.json')
        assert extra_reload['passed'] and extra_reload['runtimeErrors'] == 0 and not extra_reload['gpuLost'], component
        assert extra_reload['productionAssemblySha256'] == assembly_hash and extra_reload['unresolvedReferences'] == 0, component
        assert extra_reload['isolatedFixtureSave'] and not extra_reload['userSaveTested'], component
        runtime_key = {'ComplexLayout': 'complexLayout', 'ClinicGuest': 'clinicGuest'}.get(component, key)
        checks += reports[component][runtime_key]['checks'] + extra_reload[key + 'Reload']['checks']
        extra_service_checks[component] = dict(fresh=reports[component][runtime_key]['checks'], reload=extra_reload[key + 'Reload']['checks'])
    from verify_complex_assets import main as verify_complex_assets
    verify_complex_assets()
    from verify_restaurant_assets import main as verify_restaurant_assets
    verify_restaurant_assets()
    utility = read('UtilityVerification.json')
    assert utility['passed'] and utility['externalRequiredDependencies'] == 0
    storage = read('Verification.json')
    assert storage['storagesWithVerifiedDoubleCapacity'] == 27 and storage['fridgesWithUnchangedCapacity'] == 7
    assert read('RuntimeLocalizationTests.json')['passed'] and read('RealisticRoomsRuntime.json')['passed']
    assert read('VefInitializerTests.json')['passed']
    russian, duplicates = entries([PROJECT / 'Languages/Russian'])
    assert not duplicates and len(russian) == 3528
    artwork = read('GeminiFarmingAssets.json')
    accepted_assets = artwork['assets'] + artwork['glassAssets']
    assert len(accepted_assets) == 9
    for asset in accepted_assets:
        folder = 'UI/Farming' if asset['file'] in ('GreenhouseZone.png', 'LitHydroponics.png') else 'Things/Farming'
        file = PROJECT / 'Textures' / folder / asset['file']
        assert hashlib.sha256(file.read_bytes()).hexdigest() == asset['sha256'], str(file)
    soil_artwork = read('GeminiSoilAssets.json')
    assert soil_artwork['verifiedIndependently'] and soil_artwork['previewInspected']
    assert len(soil_artwork['assets']) == 2
    for asset in soil_artwork['assets']:
        file = PROJECT / asset['projectFile']
        assert hashlib.sha256(file.read_bytes()).hexdigest() == asset['sha256'], str(file)
    company_artwork = read('GeminiCompanyAssets.json')
    assert company_artwork['verifiedIndependently'] and company_artwork['previewInspected']
    assert hashlib.sha256((PROJECT / company_artwork['projectFile']).read_bytes()).hexdigest() == company_artwork['sha256']
    assert reports['Farming']['settingsUi']['tillingDesignatorDrawInvokedInRepaint']
    result = dict(plannedComponents=28, additionalComponents=len(additional), integratedComponents=len(actual), originalFoldersRemaining=0,
                  externalRequiredDependencies=0, activeMods=active, russianEntries=len(russian),
                  componentResourceHashesVerified=utility['resourceHashesVerified'],
                  finalRuntimeChecks=checks,
                  integratedComponentRuntimeChecks=integration_checks,
                  farmingRuntimeChecks=reports['Farming']['farming']['checks'], farmingReloadChecks=reload['farmingReload']['checks'],
                  farmingPowerChecks=read('FarmingPowerTests.json')['checks'], farmingAssetsVerified=len(accepted_assets) + len(soil_artwork['assets']),
                  isolatedFarmingSaveReloadVerified=True,
                  hotelRuntimeChecks=reports['Hospitality']['hospitality']['checks'], hotelReloadChecks=hotel_reload['hotelReload']['checks'],
                  isolatedHotelSaveReloadVerified=True,
                  restaurantRuntimeChecks=reports['Restaurant']['restaurant']['checks'], restaurantReloadChecks=restaurant_reload['restaurantReload']['checks'],
                  complexRuntimeChecks=reports['Complex']['complex']['checks'], complexReloadChecks=complex_reload['complexReload']['checks'],
                  complexAssetsVerified=3, isolatedComplexSaveReloadVerified=True,
                  restaurantAssetsVerified=3, isolatedRestaurantSaveReloadVerified=True,
                  serviceChecks=extra_service_checks, companyAssetsVerified=1,
                  productionAssemblySha256=assembly_hash,
                  fullSetReports={name: name + '-FullSet.json' for name in reports},
                  libraryRegressions={name: value['runtime'].get('checks') for name, value in related.items()},
                  gameVersion=reports['Harmony']['gameVersion'], liveGame16Verified=False,
                  userSaveTested=False, visualLayoutInspected=False,
                  odysseyGrammarChecks=252, odysseyRussianGrammarRuntimeErrors=0,
                  knownRemainingIssue=None,
                  passed=True)
    notes_path = DOCS / 'ServiceRevisionTestNotes.json'
    if notes_path.exists():
        notes = read(notes_path.name)
        if notes['productionAssemblySha256'] == assembly_hash:
            result['testStabilityLimitations'] = [note['component'] + ': ' + note['observed']
                for note in notes['failedAttemptsOutsideSuccessfulSteps'] if note.get('causeEstablished') is False]
    (DOCS / 'IntegrationCompletion.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
