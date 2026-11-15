"""Verify affected delivery services on one production binary."""
import hashlib
import json
from pathlib import Path
from audit_localization import entries

PROJECT = Path(__file__).resolve().parents[1]
DOCS = PROJECT / 'Documentation'
COMPONENTS = [('SalesDelivery', 'salesDelivery'), ('Sales', 'sales'), ('Complex', 'complex'),
              ('Recruit', 'recruit'), ('Restaurant', 'restaurant'), ('Company', 'company')]


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    progress = read(PROJECT / 'TestRuns/DeliveryRevisionProgress.json')
    expected = [step for component, _ in COMPONENTS for step in (component, component + ' reload')]
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert [s['name'] for s in progress['completed']] == expected
    digest = hashlib.sha256((PROJECT / 'Assemblies/TurboMegaMod.dll').read_bytes()).hexdigest()
    assert digest == progress['productionAssemblySha256']
    assert not (PROJECT / 'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    reports = {}
    checks = 0
    for name, key in COMPONENTS:
        fresh = read(DOCS / (name + '-FullSet.json'))
        reload = read(DOCS / (name + '-Reload.json'))
        for report in (fresh, reload):
            assert report['passed'] and report['runtimeErrors'] == 0 and not report['gpuLost'], name
            assert report['productionAssemblySha256'] == digest, name
            assert report['mapActivity']['framesAdvanced'] > 0 and report['mapActivity']['ticksAdvanced'] > 0
        assert fresh['fullUserModSet'] and not fresh['originalComponentActive']
        assert fresh['localization']['keyed'] == 2824 and fresh['localization']['scalarFields'] == 691 and fresh['localization']['grammarLines'] == 90
        assert fresh['settingsUi']['passed'] and fresh['odysseyGrammar']['passed'] and fresh['odysseyGrammar']['checks'] == 252
        assert reload['unresolvedReferences'] == 0 and reload['isolatedFixtureSave'] and not reload['userSaveTested']
        reports[name] = dict(fresh=fresh[key]['checks'], reload=reload[key + 'Reload']['checks'])
        checks += sum(reports[name].values())
    russian, duplicates = entries([PROJECT / 'Languages/Russian'])
    assert not duplicates and len(russian) == 3528
    utility = read(DOCS / 'UtilityVerification.json')
    assert utility['passed'] and utility['resourceHashesVerified'] == 4456
    result = dict(passed=True, productionAssemblySha256=digest, successfulIsolatedSteps=len(expected), runtimeChecks=checks,
                  components=reports, russianEntries=len(russian), privateWarehouseDeliveryImplemented=True,
                  wholesaleBuyersImplemented=False, fullRegressionOnThisAssembly=False,
                  baselinePublicRetailCommit='b20c7a7e9765346392ad4a2645236db5ef42cbc4',
                  scope='Staff-only stock delivery, real pickup and handover, cancellation, persistence, and affected service/job/recruitment regressions.',
                  userSaveTested=False, visualLayoutInspected=False, liveGame16Verified=False)
    (DOCS / 'DeliveryStageVerification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
