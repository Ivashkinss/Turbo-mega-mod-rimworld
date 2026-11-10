"""Verify provenance, mutually exclusive loading and complete Smart Speed RU."""
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
    for name, (_, package_id, _) in COMPONENTS.items():
        for item in manifest[name]["files"]:
            path = PROJECT / item["projectFile"]
            data = path.read_bytes()
            if path.suffix.lower() == ".xml":
                data = data.replace(b"\r\n", b"\n")
            assert hashlib.sha256(data).hexdigest() == item["sha256"], path
            count += 1
        dlls = list((PROJECT / "Components" / name / "Assemblies").glob("*.dll"))
        assert [dll.name for dll in dlls] == [name + ".dll"], dlls
        for version in ("v1.6", "v1.7"):
            selected = folders.xpath(f"/loadFolders/{version}/li[text()='Components/{name}']")
            assert len(selected) == 1
            assert enabled(selected[0].attrib, set()), (name, "missing when standalone")
            assert not enabled(selected[0].attrib, {package_id}), (name, "duplicate when original active")
    english, _ = entries([PROJECT / "Components/SmartSpeed/Languages/English"])
    russian, duplicates = entries([PROJECT / "Languages/Russian"])
    assert not duplicates, duplicates
    for key, source in english.items():
        assert key in russian, key
        check_text(source["text"], russian[key]["text"], str(key))
    report = {"components": list(COMPONENTS), "resourceHashesVerified": count,
              "smartSpeedRussianKeys": len(english), "separateHarmonyCopies": 0,
              "standaloneAndOriginalActiveLoadGuardsVerified": True, "passed": True}
    (PROJECT / "Documentation/UtilityVerification.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
