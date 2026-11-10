"""Verify the actual installed combat/trade XML and all generated RU overlays."""

import json
import re
from collections import Counter

from lxml import etree as ET

from audit_localization import entries
from combat_trade_sources import PROJECT, TARGETS, collect, normalize

FORMAT = re.compile(r"(?<!\{)\{[^{}]+\}(?!\})")
RULE_TOKEN = re.compile(r"\[[^\[\]]+\]")
TAG = re.compile(r"</?(?:b|i|color|size)(?:=[^>]+)?>")


def check_text(source, translated, label, grammar=False):
    assert Counter(FORMAT.findall(source)) == Counter(FORMAT.findall(translated)), (label, "format parameters")
    if grammar:
        assert set(RULE_TOKEN.findall(source)) == set(RULE_TOKEN.findall(translated)), (label, "grammar tokens")
    assert TAG.findall(source) == TAG.findall(translated), (label, "rich text tags")
    # Tag fragments in the runtime allowlist are joined by the original code;
    # complete XML entries must have balanced, properly nested rich text.
    stack = []
    for tag in TAG.findall(translated):
        name = re.match(r"</?(\w+)", tag)[1]
        if tag.startswith("</"):
            assert stack and stack.pop() == name, (label, "unbalanced markup")
        else:
            stack.append(name)
    assert not stack, (label, "unclosed markup")
    assert source.count("\n") == translated.count("\n"), (label, "line breaks")


def main():
    russian, duplicates = entries([PROJECT / "Languages/Russian"])
    assert not duplicates, duplicates
    report = {}
    for name, folder in TARGETS.items():
        scalars, lists, keyed, sources = collect(folder)
        for key, source in keyed.items():
            check_text(source, normalize(russian[("Keyed", key)]["text"]), key)
        for (kind, key), source in scalars.items():
            check_text(source, normalize(russian[("DefInjected/" + kind, key)]["text"]), key)
        for (kind, key), source in lists.items():
            path = PROJECT / "Languages/Russian/DefInjected" / kind / "MeleeAnimation.xml"
            translated = ET.parse(str(path)).getroot().find(key)
            assert translated is not None and len(translated) == len(source), key
            for original, item in zip(source, translated, strict=True):
                prefix, text = original.split("->", 1)
                ru_prefix, ru_text = normalize(item.text).split("->", 1)
                assert prefix == ru_prefix, (key, "rule name/weight changed")
                check_text(text, ru_text, key, grammar=True)
        report[name] = {"keyed": len(keyed), "scalarFields": len(scalars),
                        "rulePacks": len(lists), "grammarLines": sum(map(len, lists.values())), "errors": []}
    runtime = json.loads((PROJECT / "Translations/RuntimeUI.json").read_text(encoding="utf-8"))
    for source, translated in runtime["literals"].items():
        assert Counter(FORMAT.findall(source)) == Counter(FORMAT.findall(translated)), source
        assert TAG.findall(source) == TAG.findall(translated), source
        assert source.count("\n") == translated.count("\n"), source
    report["runtimeLiteralStrings"] = len(runtime["literals"])
    report["runtimeEnumLabels"] = len(runtime["corpseOffsetModes"])
    report["duplicateRussianKeys"] = len(duplicates)
    report["checks"] = ["format parameters including format specifiers", "grammar token names",
                        "rule names and weights", "rich text tags and nesting", "line breaks"]
    (PROJECT / "Documentation/LocalizationCheckpoint.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
