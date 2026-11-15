"""Publish a compact report only after the complete isolated service run passes."""
import hashlib
import json
from pathlib import Path

from verify_integration_complete import main as verify_integration

PROJECT = Path(__file__).resolve().parents[1]
EXPECTED_STEPS = ['Recruit', 'Recruit reload', 'Sales', 'Sales reload', 'SalesDelivery', 'SalesDelivery reload', 'Wholesale', 'Wholesale reload', 'Dossier', 'Dossier reload', 'Farming', 'Farming reload',
                  'Core libraries', 'Melee and hands', 'HugsLib and RunAndGun']
for component in ['Hospitality', 'Restaurant', 'Complex', 'Clinic', 'ClinicGuest',
                  'ComplexLayout', 'Company']:
    EXPECTED_STEPS.extend([component, component + ' reload'])


def main():
    progress = json.loads((PROJECT / 'TestRuns/ServiceRevisionProgress.json').read_text(encoding='utf-8-sig'))
    assert progress['passed'] and progress['current'] is None and progress['error'] is None
    assert [step['name'] for step in progress['completed']] == EXPECTED_STEPS
    digest = hashlib.sha256((PROJECT / 'Assemblies/TurboMegaMod.dll').read_bytes()).hexdigest()
    assert digest == progress['productionAssemblySha256'], 'DLL changed after regression'
    verify_integration()
    summary = dict(passed=True, runner='Tools/test_service_revision.ps1',
                   productionAssemblySha256=digest, startedUtc=progress['started'],
                   isolatedLaunches=len(EXPECTED_STEPS),
                   steps=[dict(name=step['name'], seconds=step['seconds']) for step in progress['completed']],
                   userSaveTested=False, testProbeRemoved=True)
    notes_path = PROJECT / 'Documentation/ServiceRevisionTestNotes.json'
    if notes_path.exists():
        notes = json.loads(notes_path.read_text(encoding='utf-8-sig'))
        if notes['productionAssemblySha256'] == digest:
            summary['failedAttemptsOutsideSuccessfulSteps'] = len(notes['failedAttemptsOutsideSuccessfulSteps'])
            summary['supplementalIsolatedLaunches'] = len(notes.get('supplementalChecks', []))
            summary['testNotes'] = notes_path.name
    path = PROJECT / 'Documentation/ServiceRevisionRegression.json'
    path.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(passed=True, isolatedLaunches=len(EXPECTED_STEPS),
                         productionAssemblySha256=digest)))


if __name__ == '__main__':
    main()
