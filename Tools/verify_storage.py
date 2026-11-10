"""Check packaged resources and optionally installed originals, including inheritance."""

import hashlib
import json
import re
from collections import Counter
from pathlib import Path

from lxml import etree as ET

from audit_localization import TOKEN, entries, load_roots

PROJECT = Path(__file__).resolve().parents[1]
MODS = PROJECT.parent


def definitions(directory):
    return [node for path in directory.rglob("*.xml")
            for node in ET.parse(str(path), ET.XMLParser(remove_comments=True)).getroot().findall("ThingDef")]


def inherited(node, field, templates):
    found = node.findtext(field)
    if found is not None:
        return found
    parent = node.get("ParentName")
    if parent in templates:
        return inherited(templates[parent], field, templates)
    raise AssertionError(f"Unresolved field {field} on {node.findtext('defName')}")


def capacities(nodes):
    templates = {node.get("Name"): node for node in nodes if node.get("Name")}
    result = {}
    for node in nodes:
        name = node.findtext("defName")
        if name:
            size = [int(x) for x in re.findall(r"\d+", inherited(node, "size", templates))]
            per_cell = int(inherited(node, "building/maxItemsInCell", templates))
            result[name] = (per_cell, per_cell * size[0] * size[1])
    return result


def vector(text):
    return tuple(float(x) for x in text.strip("()").split(","))


def main():
    project_xml = [path for folder in ("About", "Components", "Languages", "ThirdParty")
                   for path in (PROJECT / folder).rglob("*.xml")] + [PROJECT / "LoadFolders.xml"]
    for path in project_xml:
        ET.parse(str(path))
    changes = json.loads((PROJECT / "Documentation/StorageChanges.json").read_text(encoding="utf-8"))
    baseline = json.loads((PROJECT / "Documentation/StorageBaseline.json").read_text(encoding="utf-8"))
    for name, expected in baseline["xmlSha256LF"].items():
        actual = (PROJECT / name).read_bytes().replace(b"\r\n", b"\n")
        assert hashlib.sha256(actual).hexdigest() == expected, name
    integrated = definitions(PROJECT / "Components/NeatStorage/Defs")
    new = capacities(integrated)
    old = {item["defName"]: (item["originalStacksPerCell"], item["originalTotalStacks"])
           for item in changes["storages"]}
    original_storage = MODS / "Neat Storage/1.6/Defs"
    if original_storage.is_dir():
        assert capacities(definitions(original_storage)) == old
    assert len(new) == 27 and old.keys() == new.keys()
    for name in old:
        assert new[name] == tuple(value * 2 for value in old[name]), (name, old[name], new[name])
    fridge_old = {name: tuple(value) for name, value in baseline["fridgeCapacities"].items()}
    original_fridge = MODS / "[sbz] Fridge (Холодильники)/1.6/Defs"
    if original_fridge.is_dir():
        assert capacities(definitions(original_fridge)) == fridge_old
    fridge_new = capacities(definitions(PROJECT / "Components/Fridge/Defs"))
    assert fridge_old == fridge_new and len(fridge_new) == 7

    languages, duplicates = entries([PROJECT / "Languages/Russian"])
    assert not duplicates, duplicates
    for name, (_, total) in new.items():
        assert ("DefInjected/ThingDef", name + ".label") in languages
        text = languages[("DefInjected/ThingDef", name + ".description")]["text"]
        assert re.search(rf"до {total}\b", text), (name, total, text)
    for name in fridge_new:
        assert ("DefInjected/ThingDef", name + ".label") in languages
        assert ("DefInjected/ThingDef", name + ".description") in languages
    for node in integrated:
        if node.findtext("defName"):
            description = node.findtext("description")
            match = re.search(r"up to (\d+)", description)
            assert match and int(match[1]) == new[node.findtext("defName")][1], description

    inventory = json.loads((PROJECT / "Documentation/inventory.json").read_text(encoding="utf-8"))
    asf = next(mod for mod in inventory["mods"] if mod["packageId"] == "adaptive.storage.framework")
    roots, _ = load_roots(MODS / asf["folder"], {"ludeon.rimworld", "adaptive.storage.framework", "brrainz.harmony"})
    english, _ = entries([root / "Languages/English" for root in roots if (root / "Languages/English").is_dir()])
    for key, original in english.items():
        assert key in languages, key
        assert Counter(TOKEN.findall(original["text"])) == Counter(TOKEN.findall(languages[key]["text"])), key
    style_path = MODS / asf["folder"] / "Defs/ContentLabelStyles.xml"
    for node in ET.parse(str(style_path)).getroot():
        for field in ("label", "description"):
            assert ("DefInjected/AdaptiveStorage.ContentLabelStyleDef", f"{node.findtext('defName')}.{field}") in languages

    roots = [PROJECT, PROJECT / "Components/NeatStorage", PROJECT / "Components/Fridge"]
    named_defs = []
    abstracts = []
    for root in roots:
        for path in (root / "Defs").rglob("*.xml"):
            for node in ET.parse(str(path), ET.XMLParser(remove_comments=True)).getroot():
                if node.findtext("defName"):
                    named_defs.append((node.tag, node.findtext("defName")))
                if node.get("Name"):
                    abstracts.append((node.tag, node.get("Name")))
                for texture in node.findall(".//texPath"):
                    if (texture.text or "").startswith("Things/Building/Storage/"):
                        assert any(list((resource / "Textures" / texture.text).parent.glob(
                            Path(texture.text).name + "*.png")) for resource in roots), texture.text
    assert len(named_defs) == len(set(named_defs)), "Duplicate defs"
    assert len(abstracts) == len(set(abstracts)), "Duplicate XML inheritance templates"
    assert named_defs.count(("ThingCategoryDef", "BuildingsNeatStorage")) == 1

    fridge_xml = PROJECT / "Components/Fridge/Defs/ThingDefs_Buildings/Buildings_Fridge.xml"
    original_xml = MODS / "[sbz] Fridge (Холодильники)/1.6/Defs/ThingDefs_Buildings/Buildings_Fridge.xml"
    tree = ET.parse(str(fridge_xml))
    original_tree = ET.parse(str(original_xml)) if original_xml.is_file() else None
    selector = "/Defs/AdaptiveStorage.GraphicsDef[defName='sbz_LargeFridgeASF']/itemGraphics/columns/*/rows/*"
    assert (PROJECT / "Assemblies/TurboMegaMod.dll").is_file(), "Compile the game assembly first"
    assert len(tree.xpath(selector)) == 2
    for column, cell in enumerate(tree.xpath(selector)):
        assert cell.findtext("workerClass") == "TurboMegaMod.FridgeItemGraphicWorker"
        offset = vector(cell.findtext("drawOffsetNorth"))
        assert offset == ((0.5, 0, 0) if column == 0 else (-0.5, 0, 0.5))
        if original_tree is not None:
            original = original_tree.xpath(selector)[column]
            for orientation in ("", "East", "West"):
                assert cell.findtext("drawOffset" + orientation) == original.findtext("drawOffset" + orientation)
    layout = json.loads((PROJECT / "Documentation/FridgeLayoutTests.json").read_text(encoding="utf-8-sig"))
    assert layout["passed"] and layout["cases"] == 720
    assert layout["sourceSha256"] == hashlib.sha256(
        (PROJECT / "Source/FridgeItemGraphicWorker.cs").read_bytes().replace(b"\r\n", b"\n")).hexdigest()
    english_dev, _ = entries([MODS / "Dev In Your Language/1.6/Languages/English"])
    assert len(english_dev) == 615
    for key, original in english_dev.items():
        assert key in languages, key
        translated = languages[key]["text"]
        assert Counter(TOKEN.findall(original["text"])) == Counter(TOKEN.findall(translated)), key
        assert "[cite:" not in translated, key
    dev_repair = json.loads((PROJECT / "Documentation/DevMenuRepair.json").read_text(encoding="utf-8"))
    for source in dev_repair["sources"]:
        assert hashlib.sha256((MODS / source["source"]).read_bytes()).hexdigest() == source["sha256"]

    verified_originals = 0
    verified_assets = 0
    for source in changes["sources"]:
        original = MODS / source["source"]
        if original.is_file():
            assert hashlib.sha256(original.read_bytes()).hexdigest() == source["sourceSha256"]
            verified_originals += 1
        packaged = source.get("projectFile")
        if packaged and packaged.endswith(".png"):
            assert hashlib.sha256((PROJECT / packaged).read_bytes()).hexdigest() == source["sourceSha256"]
            verified_assets += 1
    summary = {
        "xmlFilesParsed": len(project_xml),
        "storagesWithVerifiedDoubleCapacity": len(new),
        "fridgesWithUnchangedCapacity": len(fridge_new),
        "uniqueRussianEntries": len(languages),
        "originalSourceHashesVerified": verified_originals,
        "packagedXmlBaselineHashesVerified": len(baseline["xmlSha256LF"]),
        "packagedAssetHashesVerified": verified_assets,
        "storageBaselineCommit": baseline["baselineCommit"],
        "fridgeWorkerCasesVerified": layout["cases"],
        "recoveredDevMenuRussianKeys": len(english_dev),
        "duplicateDefs": 0,
        "duplicateRussianKeys": 0,
        "inGameVerified": False,
        "visualLimitations": "Production grid worker tested with doubles and compiled against game assemblies; actual rendering still requires an in-game check.",
    }
    (PROJECT / "Documentation/Verification.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False))


if __name__ == "__main__":
    main()
