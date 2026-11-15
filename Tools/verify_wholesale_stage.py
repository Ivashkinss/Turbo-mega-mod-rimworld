"""Verify targeted wholesale evidence without mixing production binaries.

TestRuns contains local isolated fixtures, never user saves. Durable component
reports and this summary are committed; local profiles are deliberately ignored.
"""
import hashlib
import json
from pathlib import Path
from audit_localization import entries

PROJECT = Path(__file__).resolve().parents[1]
DOCS = PROJECT / 'Documentation'
COMPONENTS = [('Wholesale', 'wholesale'), ('Sales', 'sales'),
              ('SalesDelivery', 'salesDelivery'), ('Hospitality', 'hospitality'),
              ('Complex', 'complex')]
SUPPLEMENT = PROJECT / 'TestRuns/Wholesale-FullSet-20261115-180841'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_report(report, key, assembly, reload):
    assert report['passed'] and report[key]['passed'] and not report[key]['errors']
    assert report['runtimeErrors'] == 0 and not report['gpuLost']
    assert report['productionAssemblySha256'] == assembly
    assert report['mapActivity']['framesAdvanced'] > 0 and report['mapActivity']['ticksAdvanced'] > 0
    if reload:
        assert report['unresolvedReferences'] == 0 and report['isolatedFixtureSave'] and not report['userSaveTested']
    else:
        assert report['fullUserModSet'] and not report['originalComponentActive']
        assert report['localization']['passed']
        assert [report['localization'][k] for k in ('keyed', 'scalarFields', 'grammarLines')] == [2834, 691, 90]
        assert report['settingsUi']['passed'] and report['odysseyGrammar']['passed']
        assert report['odysseyGrammar']['checks'] == 252
    return report[key]['checks']


def main():
    binary = PROJECT / 'Assemblies/TurboMegaMod.dll'
    assembly = digest(binary)
    assert all(p.stat().st_mtime <= binary.stat().st_mtime for p in (PROJECT / 'Source').rglob('*.cs')), 'Source changed after build'
    assert not (PROJECT / 'Assemblies/TurboMegaMod.TestProbe.dll').exists()
    progress = read(PROJECT / 'TestRuns/WholesaleRevisionProgress.json')
    expected = [step for component, _ in COMPONENTS for step in (component, component + ' reload')]
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert progress['productionAssemblySha256'] == assembly
    assert [s['name'] for s in progress['completed']] == expected
    primary, latest, evidence = {}, {}, []
    for index, (name, key) in enumerate(COMPONENTS):
        profile = Path(progress['completed'][index * 2]['profile'])
        assert profile == Path(progress['completed'][index * 2 + 1]['profile'])
        assert profile.resolve().is_relative_to((PROJECT / 'TestRuns').resolve())
        primary[name], latest[name] = {}, {}
        for is_reload, label, filename in ((False, 'fresh', 'Result.json'), (True, 'reload', 'ReloadResult.json')):
            node = ('hotelReload' if name == 'Hospitality' else key + 'Reload') if is_reload else key
            path = profile / filename
            primary[name][label] = check_report(read(path), node, assembly, is_reload)
            current = DOCS / (name + ('-Reload.json' if is_reload else '-FullSet.json'))
            latest[name][label] = check_report(read(current), node, assembly, is_reload)
            evidence.append(dict(path=str(path.relative_to(PROJECT)), sha256=digest(path)))
            evidence.append(dict(path=str(current.relative_to(PROJECT)), sha256=digest(current)))
            if name != 'Wholesale':
                assert read(path) == read(current)
    supplemental = {}
    for is_reload, label, filename in ((False, 'fresh', 'Result.json'), (True, 'reload', 'ReloadResult.json')):
        path = SUPPLEMENT / filename
        supplemental[label] = check_report(read(path), 'wholesale' + ('Reload' if is_reload else ''), assembly, is_reload)
        assert supplemental[label] == latest['Wholesale'][label]
        evidence.append(dict(path=str(path.relative_to(PROJECT)), sha256=digest(path)))
    assert primary['Wholesale']['fresh'] >= 27 and primary['Wholesale']['reload'] >= 27
    assert supplemental['fresh'] >= 54 and supplemental['reload'] >= 31
    russian, duplicates = entries([PROJECT / 'Languages/Russian'])
    assert not duplicates and len(russian) == 3538
    utility = read(DOCS / 'UtilityVerification.json')
    assert utility['passed'] and utility['resourceHashesVerified'] == 4456
    planner = read(DOCS / 'WholesalePackingTests.json')
    assert planner['passed'] and planner['linkedIntoGameAssembly'] and not planner['runtimeWholesaleTestedByThisRunner']
    for field, path in [('plannerSha256', 'Source/Wholesale/PackingPlanner.cs'),
                        ('testSha256', 'Tests/WholesalePackingTests.cs'),
                        ('fixtureAdapterSha256', 'Tests/WholesalePackingCaseProbe.cs'),
                        ('fixtureSha256', 'Tests/Data/WholesalePackingCases.json')]:
        assert planner[field] == digest(PROJECT / path)
    source_hashes = {str(p.relative_to(PROJECT)): digest(p) for p in sorted((PROJECT / 'Source').rglob('*.cs'))}
    result = dict(passed=True, productionAssemblySha256=assembly,
                  successfulPrimarySteps=10, primaryRuntimeChecks=sum(sum(v.values()) for v in primary.values()),
                  primaryComponents=primary, supplementalSteps=2, supplementalWholesaleChecks=supplemental,
                  latestComponentReports=latest, latestReportRuntimeChecks=sum(sum(v.values()) for v in latest.values()),
                  countNote='Supplemental checks overlap the primary wholesale scenario. Do not add these totals as distinct coverage.',
                  standalonePlannerChecks=planner['checks'], standaloneFixtureCases=planner['fixtureCases'],
                  standaloneFixtureAssertions=planner['fixtureAssertions'], runtimeDelayProjections=24,
                  russianEntries=len(russian), resourceHashesVerified=4456,
                  wholesaleBuyersImplemented=True, privateWarehouseDeliveryImplemented=True,
                  scope='Real group silver and cargo, partial handover rollback, escrow, loose deep-saved goods and quality, staff delivery, native approach/departure, deadline, scheduling and affected retail/hotel/staff regressions.',
                  fullRegressionOnThisAssembly=False, baselineFullRegressionCommit='04729c6e6d3f4ae359d6c2a3e094458c025b2745',
                  userSaveTested=False, liveGame16Verified=False, visualLayoutInspected=False,
                  allFutureFactionsVerified=False, sourceSha256=source_hashes, evidence=evidence)
    (DOCS / 'WholesaleStageVerification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k not in ('sourceSha256', 'evidence')}, ensure_ascii=False))


if __name__ == '__main__':
    main()
