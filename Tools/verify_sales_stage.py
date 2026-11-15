"""Verify the sales stage without relabeling an earlier full regression."""
import hashlib
import json
from pathlib import Path

from audit_localization import entries

PROJECT = Path(__file__).resolve().parents[1]
DOCS = PROJECT / 'Documentation'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    progress = read(PROJECT / 'TestRuns/SalesRevisionProgress.json')
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    expected = ['Sales', 'Sales reload', 'Core libraries and storage']
    for name in ['Hospitality', 'Restaurant', 'ClinicGuest', 'Complex', 'Company']:
        expected.extend([name, name + ' reload'])
    assert [s['name'] for s in progress['completed']] == expected
    digest = hashlib.sha256((PROJECT / 'Assemblies/TurboMegaMod.dll').read_bytes()).hexdigest()
    assert digest == progress['productionAssemblySha256']
    assert not (PROJECT / 'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    reports = {}
    checks = 0
    reload_keys = dict(Sales='sales', Hospitality='hotel', Restaurant='restaurant', ClinicGuest='clinicguest', Complex='complex', Company='company')
    for name in ['Sales', 'Harmony', 'Hospitality', 'Restaurant', 'ClinicGuest', 'Complex', 'Company']:
        report = read(DOCS / (name + '-FullSet.json'))
        assert report['passed'] and report['runtimeErrors'] == 0 and not report['gpuLost'], name
        assert report['productionAssemblySha256'] == digest and report['fullUserModSet'], name
        assert report['localization']['keyed'] == 2823 and report['localization']['scalarFields'] == 682 and report['localization']['grammarLines'] == 90, name
        assert report['settingsUi']['passed'] and report['odysseyGrammar']['passed'] and report['odysseyGrammar']['checks'] == 252
        key = 'clinicGuest' if name == 'ClinicGuest' else name.lower()
        checks += report[key]['checks']
        reports[name] = dict(fresh=report[key]['checks'])
        if name == 'Harmony':
            assert all(v['passed'] for v in report['relatedRegressions'].values())
            checks += sum(v['runtime']['checks'] for v in report['relatedRegressions'].values())
        else:
            reload = read(DOCS / (name + '-Reload.json'))
            assert reload['passed'] and reload['runtimeErrors'] == 0 and reload['unresolvedReferences'] == 0 and not reload['gpuLost'], name
            assert reload['productionAssemblySha256'] == digest and reload['isolatedFixtureSave'] and not reload['userSaveTested'], name
            value = reload[reload_keys[name] + 'Reload']['checks']
            checks += value
            reports[name]['reload'] = value
    russian, duplicates = entries([PROJECT / 'Languages/Russian'])
    assert not duplicates and len(russian) == 3518
    utility = read(DOCS / 'UtilityVerification.json')
    assert utility['passed'] and utility['resourceHashesVerified'] == 4456
    result = dict(passed=True, productionAssemblySha256=digest, successfulIsolatedSteps=len(expected),
                  runtimeChecks=checks, components=reports, russianEntries=len(russian),
                  baselineFullRegressionCommit='04729c6e6d3f4ae359d6c2a3e094458c025b2745',
                  scope='Public retail, stored flags, real goods/currency, container withdrawals and affected service regressions.',
                  privateWarehouseDeliveryImplemented=False, wholesaleBuyersImplemented=False,
                  supplementalSalesReloadChecks=reports['Sales']['reload'],
                  supplementalScope='Additional live withdrawal from sbz_Crate, with exact stock and money conservation.',
                  fullRegressionOnThisAssembly=False, userSaveTested=False, visualLayoutInspected=False)
    (DOCS / 'SalesStageVerification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
