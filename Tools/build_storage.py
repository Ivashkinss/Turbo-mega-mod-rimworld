"""Integrate the 1.6 storage resources, leaving installed originals unchanged."""

import hashlib
import json
import re
import shutil
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
MODS = PROJECT.parent
COMPONENTS = {
    "NeatStorage": MODS / "Neat Storage",
    "Fridge": MODS / "[sbz] Fridge (Холодильники)",
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save_xml(root, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(root).write(str(path), encoding="utf-8", xml_declaration=True, pretty_print=True)


def content(component):
    result = []
    for path in (PROJECT / "Components" / component / "Defs").rglob("*.xml"):
        result += [(node, path) for node in ET.parse(str(path)).getroot().findall("ThingDef")]
    return result


def effective(node, field, named, seen=None):
    found = node.findtext(field)
    if found is not None:
        return found
    seen = set() if seen is None else seen
    parent = node.get("ParentName")
    if parent in named and parent not in seen:
        return effective(named[parent], field, named, seen | {parent})
    raise ValueError(f"Missing inherited {field} for {node.findtext('defName')}")


def main():
    provenance = []
    for component, source in COMPONENTS.items():
        target = PROJECT / "Components" / component
        for sub, dest in (("1.6/Defs", "Defs"), ("1.6/Patches", "Patches"), ("Textures", "Textures")):
            for original in sorted((source / sub).rglob("*")):
                if not original.is_file():
                    continue
                output = target / dest / original.relative_to(source / sub)
                # Both mods define the same ThingCategoryDef. Keep exactly one copy.
                if component == "Fridge" and original.relative_to(source).as_posix() == "1.6/Defs/ThingCategoryDef/ThingCategory.xml":
                    provenance.append({"source": original.relative_to(MODS).as_posix(),
                                       "sourceSha256": digest(original), "action": "identical_category_deduplicated"})
                    continue
                output.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(original, output)
                provenance.append({"source": original.relative_to(MODS).as_posix(), "sourceSha256": digest(original),
                                   "projectFile": output.relative_to(PROJECT).as_posix()})
        about = source / "About/About.xml"
        destination = PROJECT / "ThirdParty" / f"{component}.About.xml"
        destination.parent.mkdir(exist_ok=True)
        shutil.copyfile(about, destination)
        provenance.append({"source": about.relative_to(MODS).as_posix(), "sourceSha256": digest(about),
                           "projectFile": destination.relative_to(PROJECT).as_posix()})

    changes = []
    for path in (PROJECT / "Components/NeatStorage/Defs").rglob("*.xml"):
        tree = ET.parse(str(path))
        for capacity in tree.findall("ThingDef/building/maxItemsInCell"):
            capacity.text = str(int(capacity.text) * 2)
        for description in tree.findall("ThingDef/description"):
            description.text = re.sub(r"(up to )(\d+)", lambda m: m[1] + str(int(m[2]) * 2), description.text)
        # Keep the horizontal spread of linear item rows close to the original
        # when twice as many stacks are present. Circle layouts have a fixed radius.
        for cell in tree.xpath("//itemGraphics/columns/*/rows/*[stackBehaviour='Stack']"):
            factor = cell.find("stackOffsetFactor")
            if factor is None:
                factor = ET.SubElement(cell, "stackOffsetFactor")
                factor.text = "1"
            factor.text = str(float(factor.text) / 2)
        save_xml(tree.getroot(), path)

    definitions = content("NeatStorage")
    named = {node.get("Name"): node for node, _ in definitions if node.get("Name")}
    for node, path in definitions:
        name = node.findtext("defName")
        if not name:
            continue
        per_cell = int(effective(node, "building/maxItemsInCell", named))
        size = [int(value) for value in re.findall(r"\d+", effective(node, "size", named))]
        total = per_cell * size[0] * size[1]
        changes.append({"defName": name, "size": size, "originalStacksPerCell": per_cell // 2,
                        "stacksPerCell": per_cell, "originalTotalStacks": total // 2, "totalStacks": total})

    path = PROJECT / "Components/Fridge/Defs/ThingDefs_Buildings/Buildings_Fridge.xml"
    tree = ET.parse(str(path))
    cells = tree.xpath("/Defs/AdaptiveStorage.GraphicsDef[defName='sbz_LargeFridgeASF']/itemGraphics/columns/*/rows/*")
    if len(cells) != 2:
        raise ValueError("Expected exactly two item cells in the large fridge")
    for index, cell in enumerate(cells):
        # Runtime worker lays out up to 15 icons as a centered 5 x 3 grid.
        # Fewer stored stacks get larger icons and centered partial rows.
        cell.find("stackOffsetNorth").text = "(0,0,0)"
        cell.find("drawOffsetNorth").text = "(0.5,0,0)" if index == 0 else "(-0.5,0,0.5)"
        ET.SubElement(cell, "workerClass").text = "TurboMegaMod.FridgeItemGraphicWorker"
        ET.SubElement(cell, "maxDrawSize").text = "(0.95,0.5)"
        ET.SubElement(cell, "drawShadow").text = "false"
    save_xml(tree.getroot(), path)

    (PROJECT / "Documentation/StorageChanges.json").write_text(json.dumps({
        "capacityMultiplier": 2,
        "capacityScope": "All 27 Neat Storage buildings. Fridge capacities are unchanged.",
        "storages": sorted(changes, key=lambda row: row["defName"]),
        "fridgeFix": {"defName": "sbz_LargeFridge", "graphicsDef": "sbz_LargeFridgeASF",
                      "capacity": 30, "stacksPerCell": 15,
                      "originalNorthStackStep": 0.275, "layout": "adaptive centered grid, up to 5 columns x 3 rows per cell",
                      "maxItemDrawSizeSparse": [0.95, 0.5], "maxItemDrawSizeFull": [0.43, 0.34],
                      "inGameVerified": False,
                      "previousRevisionUserFeedback": "No overflow, but icons too small and fridge looked underfilled."},
        "sources": provenance,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Integrated {len(changes)} storages and 7 fridges; source files unchanged.")


if __name__ == "__main__":
    main()
