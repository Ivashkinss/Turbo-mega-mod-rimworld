"""Version-selected XML text inputs for the two localized components."""

import json
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
MODS = PROJECT.parent
STRING_FIELDS = {"label", "description", "labelNoun", "jobString", "reportString", "headerTip", "tooltip"}
TARGETS = {
    "MeleeAnimation": MODS / "Melee Animation (Анимация ближнего боя)",
    "DynamicTradeInterface": MODS / "Dynamic Trade Interface",
}
INCLUDED_IDS = {mod["packageId"].lower() for mod in json.loads(
    (PROJECT / "Documentation/inventory.json").read_text(encoding="utf-8"))["mods"] if mod["include"]}


def normalize(text):
    return (text or "").replace("\\n", "\n").strip()


def selected_files(folder, subfolder):
    # A version folder overrides the same relative path in the common folder.
    files = {}
    for root in (folder, folder / "1.6"):
        for path in sorted((root / subfolder).rglob("*.xml")):
            files[path.relative_to(root / subfolder).as_posix()] = path
    return list(files.values())


def scalar_fields(node, prefix=""):
    for child in node:
        if not isinstance(child.tag, str):
            continue
        if child.tag == "li":
            segment = str(sum(sibling.tag == "li" for sibling in child.itersiblings(preceding=True)))
        else:
            segment = child.tag
        path = f"{prefix}.{segment}" if prefix else segment
        if len(child):
            yield from scalar_fields(child, path)
        elif child.tag in STRING_FIELDS and normalize(child.text):
            yield path, normalize(child.text)


def collect(folder):
    parser = ET.XMLParser(remove_comments=True)
    sources = selected_files(folder, "Defs")
    definitions = []
    templates = {}
    scalars = {}
    lists = {}
    for path in sources:
        for definition in ET.parse(str(path), parser).getroot():
            if not isinstance(definition.tag, str):
                continue
            if definition.get("MayRequire") and not all(
                item.strip().lower() in INCLUDED_IDS for item in definition.get("MayRequire").split(",")
            ):
                continue
            if definition.get("Name"):
                templates[definition.get("Name")] = definition
            if definition.findtext("defName"):
                definitions.append(definition)
    for definition in definitions:
        name = definition.findtext("defName")
        chain = [definition]
        parent = definition.get("ParentName")
        seen = set()
        while parent in templates and parent not in seen:
            seen.add(parent)
            chain.append(templates[parent])
            parent = templates[parent].get("ParentName")
        fields = {}
        for ancestor in reversed(chain):
            fields.update(scalar_fields(ancestor))
        for field, text in fields.items():
            scalars[(definition.tag, f"{name}.{field}")] = text
        if definition.tag == "TipSetDef":
            for index, tip in enumerate(definition.findall("tips/li")):
                scalars[(definition.tag, f"{name}.tips.{index}")] = normalize(tip.text)
        if definition.tag == "RulePackDef":
            rules = definition.find("rulePack/rulesStrings")
            if rules is not None:
                lists[(definition.tag, f"{name}.rulePack.rulesStrings")] = [normalize(x.text) for x in rules]
    keyed = {}
    for path in selected_files(folder, "Languages/English/Keyed"):
        for node in ET.parse(str(path), parser).getroot():
            if isinstance(node.tag, str):
                keyed[node.tag] = normalize(node.text)
        sources.append(path)
    return scalars, lists, keyed, sources
