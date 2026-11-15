"""Targeted medical billing evidence; deliberately not the full-mod verifier."""
import hashlib
import json
from pathlib import Path
from audit_localization import entries

PROJECT = Path(__file__).resolve().parents[1]
DOCS = PROJECT / 'Documentation'
COMPONENTS = [('Clinic', 'clinic'), ('ClinicGuest', 'clinicguest'), ('Company', 'company')]


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    binary = PROJECT / 'Assemblies/TurboMegaMod.dll'
    digest = sha(binary)
    assert all(p.stat().st_mtime <= binary.stat().st_mtime for p in (PROJECT / 'Source').rglob('*.cs'))
    assert not (PROJECT / 'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    progress = read(PROJECT / 'TestRuns/ClinicAttributionProgress.json')
    expected = [step for name, _ in COMPONENTS for step in (name, name + ' reload')]
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert progress['productionAssemblySha256'] == digest
    assert [s['name'] for s in progress['completed']] == expected
    components, evidence = {}, []
    for i, (name, key) in enumerate(COMPONENTS):
        profile = Path(progress['completed'][i * 2]['profile'])
        assert profile.resolve().is_relative_to((PROJECT / 'TestRuns').resolve())
        assert profile == Path(progress['completed'][i * 2 + 1]['profile'])
        components[name] = {}
        for reload, label, filename in [(False, 'fresh', 'Result.json'), (True, 'reload', 'ReloadResult.json')]:
            path = profile / filename
            current = DOCS / (name + ('-Reload.json' if reload else '-FullSet.json'))
            report = read(path)
            assert report == read(current)
            assert report['passed'] and report['runtimeErrors'] == 0 and not report['gpuLost'], name
            assert report['productionAssemblySha256'] == digest, name
            assert report['mapActivity']['ticksAdvanced'] > 0 and report['mapActivity']['framesAdvanced'] > 0
            node = key + 'Reload' if reload else ('clinicGuest' if name == 'ClinicGuest' else key)
            assert report[node]['passed'] and not report[node]['errors']
            if reload:
                assert report['unresolvedReferences'] == 0 and report['isolatedFixtureSave'] and not report['userSaveTested']
            else:
                assert report['fullUserModSet'] and not report['originalComponentActive']
                assert report['settingsUi']['passed'] and report['localization']['passed']
                assert [report['localization'][k] for k in ('keyed', 'scalarFields', 'grammarLines')] == [2834, 691, 90]
                assert report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks'] == 252
            components[name][label] = report[node]['checks']
            evidence.extend([dict(path=str(path.relative_to(PROJECT)), sha256=sha(path)),
                             dict(path=str(current.relative_to(PROJECT)), sha256=sha(current))])
    assert components['Clinic']['fresh'] >= 32 and components['Clinic']['reload'] >= 11
    model = read(DOCS / 'MedicalAllocationTests.json')
    assert model['passed'] and model['cases'] == 20 and model['checks'] >= 219
    assert model['sourceSha256'] == sha(PROJECT / 'Source/MedicalAllocation.cs')
    assert model['fixtureSha256'] == sha(PROJECT / 'Tests/Data/ClinicReceiptAllocationCases.json')
    russian, duplicates = entries([PROJECT / 'Languages/Russian'])
    assert not duplicates and len(russian) == 3538
    utility = read(DOCS / 'UtilityVerification.json')
    assert utility['passed'] and utility['resourceHashesVerified'] == 4456
    result = dict(passed=True, productionAssemblySha256=digest, successfulIsolatedSteps=6,
                  runtimeChecks=sum(sum(v.values()) for v in components.values()), components=components,
                  standaloneAllocationCases=model['cases'], standaloneAllocationChecks=model['checks'],
                  medicalWorkAttributionImplemented=True, medicalBuyoutCreditImplemented=True,
                  consumableServiceCreditPercent=50, laborOnlyServiceCreditPercent=100,
                  countNote='Standalone numerical assertions are separate from live game assertions.',
                  scope='Native tending and medical bill-completion callbacks, whole-service consumable classification, two workers, physical partial receipts, half-coin and frozen-plan Scribe persistence, repeated discharge, clinical guest and existing work-credit regressions.',
                  fullSurgeryJobWithIngredientsTested=False, allMalformedSavesTested=False,
                  fullRegressionOnThisAssembly=False, userSaveTested=False, visualLayoutInspected=False,
                  liveGame16Verified=False, russianEntries=len(russian),
                  sourceSha256={str(p.relative_to(PROJECT)): sha(p) for p in sorted((PROJECT / 'Source').rglob('*.cs'))},
                  evidence=evidence)
    (DOCS / 'ClinicAttributionStageVerification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k not in ('sourceSha256', 'evidence')}, ensure_ascii=False))


if __name__ == '__main__':
    main()
