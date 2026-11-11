"""Import selected utility binaries/resources; never copy bundled Harmony."""
import hashlib
import json
import shutil
import argparse
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
COMPONENTS = {
    "CommonSense": ("Common Sense (Умные действия колонистов)", "avilmask.commonsense", {
        "1.6/Assemblies/CommonSense.dll": "Assemblies/CommonSense.dll",
        "Languages/English/Keyed/strings.xml": "Languages/English/Keyed/strings.xml",
        **{f"1.6/Defs/{name}.xml": f"Defs/{name}.xml" for name in ("RoomRoles", "Recipes_Meals", "JoyGivers", "Jobs_Misc")},
        **{f"1.6/Patches/{name}.xml": f"Patches/{name}.xml" for name in ("PackMeal", "FunPolice", "FoodOptimality", "CleanOnOpportunity", "CleanGizmo")},
        **{f"Textures/UI/Icons/{name}.png": f"Textures/UI/Icons/{name}.png" for name in ("Unload_Thing", "Unload_Thing_Cancel")},
    }),
    "SnapOut": ("Snap Out! (Успокоение срывающихся колонистов)", "weilbyte.snapout", {
        "1.6/Assemblies/SnapOut.dll": "Assemblies/SnapOut.dll",
        "Languages/English/Keyed/CalmDown.xml": "Languages/English/Keyed/CalmDown.xml",
        "1.6/Patches/CalmDownPatches.xml": "Patches/CalmDownPatches.xml",
        **{f"1.6/Defs/{folder}/{name}.xml": f"Defs/{folder}/{name}.xml" for folder, name in (
            ("JobDefs", "Jobs_SnapOut"), ("ThoughtDefs", "Thoughts_SnapOut"),
            ("InteractionDefs", "Interactions_SnapOut"), ("WorkGiverDefs", "WorkGivers_SnapOut"))},
    }),
    "DevInYourLanguage": ("Dev In Your Language", "latta.devl10n", {
        "1.6/Assemblies/DevInYourLanguage.dll": "Assemblies/DevInYourLanguage.dll",
        **{f"1.6/Languages/English/Keyed/{name}.xml": f"Languages/English/Keyed/{name}.xml"
           for name in ("Actions", "Actions_Incidents", "Menu", "Mod", "Settings", "TranslationOutput", "Windows")},
    }),
    "SmartSpeed": ("Smart Speed (4 скорость)", "sarg.smartspeed", {
        "1.6/Assemblies/SmartSpeed.dll": "Assemblies/SmartSpeed.dll",
        "Languages/English/Keyed/ACSmartSpeedStrings.xml": "Languages/English/Keyed/ACSmartSpeedStrings.xml",
        "Textures/UI/TimeControls/TimeSpeedButton_Ultrafast.png": "Textures/UI/TimeControls/TimeSpeedButton_Ultrafast.png",
        "Textures/UI/SmartSpeedLogo.png": "Textures/UI/SmartSpeedLogo.png",
    }),
    "SmarterConstruction": ("Smarter Construction (Умное строительство)", "dhultgren.smarterconstruction", {
        "v1.6/Assemblies/SmarterConstruction.dll": "Assemblies/SmarterConstruction.dll",
    }),
    "InteractionBubbles": ("Interaction Bubbles (Речевые пузыри)", "jaxe.bubbles", {
        "Assemblies/Bubbles.dll": "Assemblies/Bubbles.dll",
        "Languages/English/Keyed/Bubbles.xml": "Languages/English/Keyed/Bubbles.xml",
        "Textures/Bubbles/Icon.png": "Textures/Bubbles/Icon.png",
        "Textures/Bubbles/Inner.png": "Textures/Bubbles/Inner.png",
        "Textures/Bubbles/Outer.png": "Textures/Bubbles/Outer.png",
    }),
    "RealisticRooms": ("Realistic Rooms Rewritten (Снижение требований к комнатам)", "lucifer.realisticrooms", {
        "1.4/Assemblies/RealisticRoomsRewritten.dll": "Assemblies/RealisticRoomsRewritten.dll",
        "Languages/English/Keyed/settings.xml": "Languages/English/Keyed/settings.xml",
        "Patches/Rooms/RoomStats.xml": "Patches/Rooms/RoomStats.xml",
        "Patches/ThingDefs_Misc/Filth_Various.xml": "Patches/ThingDefs_Misc/Filth_Various.xml",
    }),
    "AlmostThere": ("Almost There! (Караваны не спят)", "duz.almosttherefork", {
        "1.6/Assemblies/CaravanDontRest.dll": "Standard/Assemblies/CaravanDontRest.dll",
        "VF/1.6/Assemblies/CaravanDontRest.dll": "Vehicles/Assemblies/CaravanDontRest.dll",
        "Patches/CaravanDontRestPatch.xml": "Patches/CaravanDontRestPatch.xml",
        "VF/Patches/CaravanDontRestPatchVF.xml": "Vehicles/Patches/CaravanDontRestPatchVF.xml",
        "Languages/English/Keyed/GameplayCommands.xml": "Languages/English/Keyed/GameplayCommands.xml",
        "Languages/English/Keyed/AlmostThere_keys.xml": "Languages/English/Keyed/AlmostThere_keys.xml",
        "Textures/UI/ForceRest.png": "Textures/UI/ForceRest.png",
        "Textures/UI/DontRest.png": "Textures/UI/DontRest.png",
        "Textures/UI/AlmostThere.png": "Textures/UI/AlmostThere.png",
    }),
}


def clean_xml(path):
    text = path.read_text(encoding="utf-8-sig")
    path.write_text("\n".join(line.rstrip() for line in text.splitlines()) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--component", action="append", choices=COMPONENTS,
                        help="Import only this component and preserve other manifest entries; repeat as needed.")
    selected = parser.parse_args().component or list(COMPONENTS)
    for name in selected:
        folder, _, files = COMPONENTS[name]
        for relative in [*files, "About/About.xml"]:
            original = PROJECT.parent / folder / relative
            if not original.is_file():
                raise FileNotFoundError(f"Restore original resources before importing: {original}")
    manifest = PROJECT / "Documentation/UtilityComponents.json"
    report = json.loads(manifest.read_text(encoding="utf-8")) if manifest.is_file() else {}
    for name in selected:
        folder, package_id, files = COMPONENTS[name]
        source = PROJECT.parent / folder
        items = []
        for relative, destination in files.items():
            original = source / relative
            target = PROJECT / "Components" / name / destination
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(original, target)
            source_hash = hashlib.sha256(original.read_bytes()).hexdigest()
            if target.suffix.lower() == ".xml":
                clean_xml(target)
            data = target.read_bytes()
            if target.suffix.lower() == ".xml":
                data = data.replace(b"\r\n", b"\n")
            items.append({"source": original.relative_to(PROJECT.parent).as_posix(),
                          "projectFile": target.relative_to(PROJECT).as_posix(),
                          "sourceRawSha256": source_hash,
                          "sha256": hashlib.sha256(data).hexdigest()})
        about = PROJECT / "ThirdParty" / f"{name}.About.xml"
        shutil.copyfile(source / "About/About.xml", about)
        clean_xml(about)
        report[name] = {"packageId": package_id, "files": items, "bundledHarmonyCopied": False,
                        "textNormalization": "LF line endings and trailing line whitespace removed; XML values otherwise unchanged"}
    path = PROJECT / "LoadFolders.xml"
    tree = ET.parse(str(path), ET.XMLParser(remove_blank_text=True))
    for version in ("v1.6", "v1.7"):
        for name, (_, package_id, _) in COMPONENTS.items():
            if not tree.xpath(f"/{tree.getroot().tag}/{version}/li[text()='Components/{name}']"):
                ET.SubElement(tree.find(version), "li", IfModNotActive=package_id).text = f"Components/{name}"
        for branch, attributes in {
            "Standard": {"IfModNotActive": "duz.almosttherefork,smashphil.vehicleframework"},
            "Vehicles": {"IfModNotActive": "duz.almosttherefork", "IfModActive": "smashphil.vehicleframework"},
        }.items():
            folder = "Components/AlmostThere/" + branch
            if "AlmostThere" in report and not tree.xpath(f"/loadFolders/{version}/li[text()='{folder}']"):
                ET.SubElement(tree.find(version), "li", **attributes).text = folder
    ET.indent(tree, space="  ")
    tree.write(str(path), encoding="utf-8", xml_declaration=True, pretty_print=True)
    manifest.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({name: len(item["files"]) for name, item in report.items()}))


if __name__ == "__main__":
    main()
