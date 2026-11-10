"""Recover the installed Russian dev-menu text without editing the source mod."""

import hashlib
import json
import re
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
SOURCE = PROJECT.parent / "Dev In Your Language/1.6/Languages"


def main():
    english = {}
    russian = {}
    sources = []
    repairs = 0
    for path in sorted((SOURCE / "English/Keyed").glob("*.xml")):
        for node in ET.parse(str(path)).getroot():
            if isinstance(node.tag, str):
                english[node.tag] = node.text or ""
    for path in sorted((SOURCE / "Russian/Keyed").glob("*.xml")):
        text = path.read_text(encoding="utf-8-sig")
        if path.name == "Actions.xml":
            # Match the actual broken element, not any valid PlaySound element.
            text, fixed = re.subn(
                r"(<DebugAction_PlayAnimation>[^<]*)</DebugAction_PlaySound>",
                r"\1</DebugAction_PlayAnimation>", text)
            repairs += fixed
        tree = ET.fromstring(text.encode("utf-8"))
        for node in tree:
            if not isinstance(node.tag, str):
                continue
            value = re.sub(r"\[cite:\s*\d+\]", "", node.text or "").strip()
            # Keep the existing Russian wording. Remove only an exact duplicate
            # of the English source: DIYL itself adds that as a grey annotation.
            original = english.get(node.tag, "\0")
            suffix = " " + original if original.startswith("(") and original.endswith(")") else " (" + original + ")"
            if value.endswith(suffix):
                value = value[:-len(suffix)].rstrip()
            russian[node.tag] = value
        sources.append({"source": path.relative_to(PROJECT.parent).as_posix(),
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    missing = sorted(set(english) - set(russian))
    if missing:
        raise ValueError("Missing original Russian keys: " + ", ".join(missing))
    root = ET.Element("LanguageData")
    for key, text in sorted(russian.items()):
        ET.SubElement(root, key).text = text
    output = PROJECT / "Languages/Russian/Keyed/DevMenu.xml"
    output.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(root).write(str(output), encoding="utf-8", xml_declaration=True, pretty_print=True)
    report = {"recoveredRussianKeys": len(russian), "englishReferenceKeys": len(english),
              "fixedClosingTags": repairs, "missingKeys": missing,
              "originalModChanged": False, "originalSettingsChanged": False,
              "sources": sources}
    (PROJECT / "Documentation/DevMenuRepair.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "sources"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
