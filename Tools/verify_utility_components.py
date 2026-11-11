"""Verify provenance, mutually exclusive loading and utility translations."""
import hashlib
import json
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
            if path.suffix.lower() == ".xml":
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
            check_text(source["text"], russian[key]["text"], str(key))
            if key == ("Keyed", "Bubbles.OffsetDirections"):
                assert len(source["text"].split("|")) == len(russian[key]["text"].split("|")) == 4
        translated[name] = len(english)
    report = {"components": list(COMPONENTS), "resourceHashesVerified": count,
              "russianKeysByComponent": translated, "separateHarmonyCopies": 0,
              "standaloneAndOriginalActiveLoadGuardsVerified": True, "passed": True}
    report["almostThereBranchCombinationsChecked"] = 8
    report["vehicleFrameworkRuntimeVerified"] = False
    (PROJECT / "Documentation/UtilityVerification.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
