"""Import selected utility binaries/resources; never copy bundled Harmony."""
import hashlib
import json
import shutil
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
COMPONENTS = {
    "SmartSpeed": ("Smart Speed (4 скорость)", "sarg.smartspeed", {
        "1.6/Assemblies/SmartSpeed.dll": "Assemblies/SmartSpeed.dll",
        "Languages/English/Keyed/ACSmartSpeedStrings.xml": "Languages/English/Keyed/ACSmartSpeedStrings.xml",
        "Textures/UI/TimeControls/TimeSpeedButton_Ultrafast.png": "Textures/UI/TimeControls/TimeSpeedButton_Ultrafast.png",
        "Textures/UI/SmartSpeedLogo.png": "Textures/UI/SmartSpeedLogo.png",
    }),
    "SmarterConstruction": ("Smarter Construction (Умное строительство)", "dhultgren.smarterconstruction", {
        "v1.6/Assemblies/SmarterConstruction.dll": "Assemblies/SmarterConstruction.dll",
    }),
}


def clean_xml(path):
    text = path.read_text(encoding="utf-8-sig")
    path.write_text("\n".join(line.rstrip() for line in text.splitlines()) + "\n", encoding="utf-8")


def main():
    report = {}
    for name, (folder, package_id, files) in COMPONENTS.items():
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
    ET.indent(tree, space="  ")
    tree.write(str(path), encoding="utf-8", xml_declaration=True, pretty_print=True)
    (PROJECT / "Documentation/UtilityComponents.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({name: len(item["files"]) for name, item in report.items()}))


if __name__ == "__main__":
    main()
