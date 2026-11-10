"""Build translated definitions and combat grammar from reviewed dictionaries."""

import hashlib
import json
from collections import defaultdict

from lxml import etree as ET

from combat_trade_sources import PROJECT, TARGETS, collect, normalize


def main():
    folder = TARGETS["MeleeAnimation"]
    scalars, lists, _, _ = collect(folder)
    mappings = json.loads((PROJECT / "Translations/MeleeAnimation.Defs.json").read_text(encoding="utf-8"))
    grammar = json.loads((PROJECT / "Translations/MeleeAnimation.Rules.json").read_text(encoding="utf-8"))
    output = defaultdict(dict)
    for (kind, key), source in scalars.items():
        if source not in mappings:
            raise ValueError(f"Untranslated field {kind}/{key}: {source!r}")
        output[kind][key] = mappings[source]
    for (kind, key), source_list in lists.items():
        translated = []
        for line in source_list:
            prefix, text = line.split("->", 1)
            if text not in grammar:
                raise ValueError(f"Untranslated rule {key}: {text!r}")
            translated.append(prefix + "->" + grammar[text])
        output[kind][key] = translated
    for kind, entries in output.items():
        root = ET.Element("LanguageData")
        for key, value in sorted(entries.items()):
            node = ET.SubElement(root, key)
            if isinstance(value, list):
                for line in value:
                    ET.SubElement(node, "li").text = line
            else:
                node.text = value
        path = PROJECT / "Languages/Russian/DefInjected" / kind / "MeleeAnimation.xml"
        path.parent.mkdir(parents=True, exist_ok=True)
        ET.ElementTree(root).write(str(path), encoding="utf-8", xml_declaration=True, pretty_print=True)
    sources = {}
    for target in TARGETS.values():
        for path in collect(target)[3]:
            sources[path.relative_to(PROJECT.parent).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    report = {"meleeScalarFieldsGenerated": len(scalars),
              "meleeRulePacksGenerated": len(lists),
              "meleeGrammarLinesTranslated": sum(len(value) for value in lists.values()),
              "sourceHashes": sources,
              "scope": "Russian XML overlay; original components and settings unchanged."}
    (PROJECT / "Documentation/CombatTradeSources.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "sourceHashes"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
