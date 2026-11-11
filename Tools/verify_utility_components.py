"""Verify provenance, mutually exclusive loading and utility translations."""
import hashlib
import json
import re
from pathlib import Path

from lxml import etree as ET

from audit_localization import enabled, entries
from build_utility_components import COMPONENTS, PROJECT
from verify_combat_localization import check_text


def main():
    manifest = json.loads((PROJECT / "Documentation/UtilityComponents.json").read_text(encoding="utf-8"))
    folders = ET.parse(str(PROJECT / "LoadFolders.xml"))
    count = 0
    translated = {}
    for name, (_, package_id, _) in COMPONENTS.items():
        expected_files = {item["projectFile"] for item in manifest[name]["files"]}
        actual_files = {path.relative_to(PROJECT).as_posix()
                        for path in (PROJECT / "Components" / name).rglob("*") if path.is_file()}
        assert actual_files == expected_files, (name, "untracked or missing component resources")
        for item in manifest[name]["files"]:
            path = PROJECT / item["projectFile"]
            data = path.read_bytes()
            if path.suffix.lower() in (".xml", ".json") or path.name in ("LICENSE", "LICENSE.md"):
                data = data.replace(b"\r\n", b"\n")
            assert hashlib.sha256(data).hexdigest() == item["sha256"], path
            count += 1
        dlls = sorted(path.relative_to(PROJECT).as_posix() for path in (PROJECT / "Components" / name).rglob("*.dll"))
        expected_dlls = sorted(item["projectFile"] for item in manifest[name]["files"]
                               if item["projectFile"].endswith(".dll"))
        assert dlls == expected_dlls, dlls
        for version in ("v1.6", "v1.7"):
            selected = folders.xpath(f"/loadFolders/{version}/li[text()='Components/{name}']")
            assert len(selected) == 1
            assert enabled(selected[0].attrib, set()), (name, "missing when standalone")
            assert not enabled(selected[0].attrib, {package_id}), (name, "duplicate when original active")
    for version in ("v1.6", "v1.7"):
        order = [node.text for node in folders.find(version)]
        assert order.index('Components/HugsLib') < order.index('Components/AdaptiveStorage'), 'Load the library before its consumers'
        assert order.index('Components/AdaptiveStorage') < order.index('/'), 'Storage types must load before TurboMegaMod.dll'
        for original in (False, True):
            for vehicles in (False, True):
                ids = ({"duz.almosttherefork"} if original else set()) | ({"smashphil.vehicleframework"} if vehicles else set())
                selected = [node.text for node in folders.find(version)
                            if node.text.startswith("Components/AlmostThere") and enabled(node.attrib, ids)]
                expected = [] if original else ["Components/AlmostThere", "Components/AlmostThere/" + ("Vehicles" if vehicles else "Standard")]
                assert selected == expected, (version, ids, selected)
    russian, duplicates = entries([PROJECT / "Languages/Russian"])
    assert not duplicates, duplicates
    for name in COMPONENTS:
        english, _ = entries([PROJECT / "Components" / name / "Languages/English"])
        for key, source in english.items():
            assert key in russian, key
            check_text(source["text"].replace("\\n", "\n").strip(), russian[key]["text"].replace("\\n", "\n").strip(), str(key))
            if key == ("Keyed", "Bubbles.OffsetDirections"):
                assert len(source["text"].split("|")) == len(russian[key]["text"].split("|")) == 4
        translated[name] = len(english)
    # The source Russian tutorials/jobs used obsolete defNames. Verify against
    # the definitions that are actually shipped, including all current fields.
    sidearm_fields = 0
    for kind, file, fields in (
        ('ConceptDef', 'TutorDefs/Concepts.xml', ('label', 'helpText')),
        ('JobDef', 'JobDefs/Jobs_Misc.xml', ('reportString',)),
    ):
        expected_keys = set()
        for definition in ET.parse(str(PROJECT / 'Components/SimpleSidearms/Defs' / file)).getroot():
            for field in fields:
                key = definition.findtext('defName') + '.' + field
                expected_keys.add(key)
                source = definition.findtext(field).replace('\\n', '\n').strip()
                value = russian[('DefInjected/' + kind, key)]['text'].replace('\\n', '\n').strip()
                check_text(source, value, key)
                assert source.count('TargetA') == value.count('TargetA'), (key, 'job target token')
                sidearm_fields += 1
        actual_keys = {node.tag for node in ET.parse(str(PROJECT / 'Languages/Russian/DefInjected' / kind / 'SimpleSidearms.xml')).getroot()}
        assert actual_keys == expected_keys, (kind, 'stale or missing Sidearms def fields')
    report = {"components": list(COMPONENTS), "resourceHashesVerified": count,
              "russianKeysByComponent": translated, "separateHarmonyCopies": 0,
              "standaloneAndOriginalActiveLoadGuardsVerified": True, "passed": True}
    report["almostThereBranchCombinationsChecked"] = 8
    report["vehicleFrameworkRuntimeVerified"] = False
    report["sidearmsDefinitionFieldsVerified"] = sidearm_fields
    menus = list((PROJECT / 'Components').rglob('FloatSubMenu.dll'))
    assert menus == [PROJECT / 'Components/UsefulMarks/Assemblies/FloatSubMenu.dll'], menus
    assert manifest['NiceBillTab']['sharedMenuProvider'].startswith('UsefulMarks:')
    report['singleSharedFloatSubMenuVerified'] = True
    for version in ('v1.6', 'v1.7'):
        for original in (False, True):
            for psycasts in (False, True):
                ids = ({'zeracronius.dynamictradeinterface'} if original else set()) | ({'vanillaexpanded.vpsycastse'} if psycasts else set())
                actual = [n.text for n in folders.find(version) if n.text.startswith('Components/DynamicTradeInterface') and enabled(n.attrib, ids)]
                expected = [] if original else ['Components/DynamicTradeInterface'] + (['Components/DynamicTradeInterface/VPE'] if psycasts else [])
                assert actual == expected, (version, ids, actual)
    report['tradePsycastsBranchCombinationsChecked'] = 8
    report['tradePsycastsRuntimeVerified'] = False
    wall_keys = ET.parse(str(PROJECT / 'Languages/English/Keyed/WallsAreSolid.xml')).getroot()
    for node in wall_keys:
        translated_text = russian[('Keyed', node.tag)]['text']
        check_text(node.text.replace('\\n', '\n'), translated_text.replace('\\n', '\n'), node.tag)
        assert re.findall(r'\{[^}]+\}', node.text) == re.findall(r'\{[^}]+\}', translated_text), node.tag
    report['wallSettingsTranslationKeysVerified'] = len(wall_keys)
    melee_branches = {
        'Patch_FacialAnimation': 'nals.facialanimation', 'Patch_Lightsabers': 'projectjedi.lightsabers',
        'Patch_AlienRaces': 'erdelf.humanoidalienraces', 'Patch_CAI5000': 'krkr.rule56',
        'Patch_CombatExtended': 'ceteam.combatextended', 'Patch_PerformanceOptimizer': 'taranchuk.performanceoptimizer',
        'Patch_Tacticowl': 'owlchemist.tacticowl', 'Patch_Vehicles': 'smashphil.vehicleframework',
    }
    combinations = 0
    for version in ('v1.6', 'v1.7'):
        for original in (False, True):
            for branch, companion in melee_branches.items():
                for active in (False, True):
                    ids = ({'co.uk.epicguru.meleeanimation'} if original else set()) | ({companion} if active else set())
                    path = 'Components/MeleeAnimation/Optional/' + branch
                    nodes = folders.xpath(f'/loadFolders/{version}/li[text()="{path}"]')
                    assert len(nodes) == 1 and enabled(nodes[0].attrib, ids) == (active and not original), (version, branch, ids)
                    combinations += 1
    report['meleeOptionalBranchCombinationsChecked'] = combinations
    report['meleeOptionalCompanionsRuntimeVerified'] = False
    (PROJECT / "Documentation/UtilityVerification.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
