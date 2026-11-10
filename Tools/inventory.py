"""Read installed mods; write project inventory without modifying originals."""

import json
from pathlib import Path
import xml.etree.ElementTree as ET

PROJECT = Path(__file__).resolve().parents[1]
MODS = PROJECT.parent
EXCLUDED = {
    "brrainz.cameraplus": "Camera+ — исключено и удалено по просьбе пользователя 11 ноября 2026.",
    "sjaandi.realtiltmod": "Освещение и тени — исключено пользователем.",
    "vanillaexpanded.vtexe": "HD-текстуры — исключено пользователем.",
    "icc.eov.flak": "Ретекстуры брони — исключено пользователем.",
}
KEPT_EXPLICITLY = {"co.uk.epicguru.meleeanimation", "mlie.showmeyourhands"}


def items(element, path):
    return [node.text.strip() for node in element.findall(path) if node.text]


def inspect(folder):
    about = ET.parse(folder / "About" / "About.xml").getroot()
    package_id = about.findtext("packageId", "").strip()
    key = package_id.lower()
    files = sorted(path for path in folder.rglob("*") if path.is_file())
    # Language directories, including regional naming variants, across all versions.
    russian = sorted({
        path.relative_to(folder).as_posix()
        for path in folder.rglob("*")
        if path.is_dir() and path.parent.name.lower() == "languages"
        and path.name.lower().startswith("russian")
    })
    loadfolders = folder / "LoadFolders.xml"
    declared_roots = None
    if loadfolders.exists():
        version = ET.parse(loadfolders).getroot().find("v1.6")
        declared_roots = [
            {"path": (entry.text or "").strip(), "conditions": dict(entry.attrib)}
            for entry in version.findall("li")
        ] if version is not None else []
    source_files = [
        path.relative_to(folder).as_posix() for path in files
        if path.suffix.lower() == ".cs"
        and not {"packages", "obj", "bin"}.intersection(
            part.lower() for part in path.relative_to(folder).parts
        )
    ]
    integrated = {
        "sbz.neatstorage": PROJECT / "Components/NeatStorage/Defs",
        "sbz.neatstoragefridge": PROJECT / "Components/Fridge/Defs",
    }
    integrated_here = key in integrated and integrated[key].is_dir()
    translated_here = integrated_here or (
        key == "adaptive.storage.framework"
        and (PROJECT / "Languages/Russian/Keyed/AdaptiveStorage.xml").is_file()
    )
    return {
        "folder": folder.name,
        "name": about.findtext("name", "").strip(),
        "packageId": package_id,
        "author": about.findtext("author", about.findtext("authors", "")).strip(),
        "include": key not in EXCLUDED,
        "decision": EXCLUDED.get(key, "Оставить анимации — явное решение пользователя."
                                 if key in KEPT_EXPLICITLY else "Включить в объединение."),
        "supportedVersions": items(about, "supportedVersions/li"),
        "dependencies": items(about, "modDependencies/li/packageId"),
        "dependenciesByVersion": {
            version.tag: items(version, "li/packageId")
            for version in about.findall("modDependenciesByVersion/*")
        },
        "loadAfter": items(about, "loadAfter/li"),
        "loadBefore": items(about, "loadBefore/li"),
        "declaredLoadFolders16": declared_roots,
        "russianDirectoriesAllVersions": russian,
        "sourceFilesAllVersions": source_files,
        "topLevelLicenseFiles": [
            path.name for path in files if path.parent == folder
            and ("license" in path.name.lower() or "copying" in path.name.lower())
        ],
        "integrationStatus": ("integrated_not_game_tested" if integrated_here else "pending")
                             if key not in EXCLUDED else "excluded",
        "translationStatus": ("project_xml_translated_not_game_tested" if translated_here
                              else "see_LocalizationAudit") if key not in EXCLUDED else "excluded",
    }


def main():
    records = [inspect(folder) for folder in sorted(MODS.iterdir())
               if folder != PROJECT and (folder / "About" / "About.xml").is_file()]
    included = [record for record in records if record["include"]]
    included_ids = {record["packageId"].lower() for record in included}
    missing = []
    for record in included:
        dependencies = record["dependencies"] + record["dependenciesByVersion"].get("v1.6", [])
        for dependency in dependencies:
            if dependency.lower() not in included_ids and not dependency.lower().startswith("ludeon."):
                missing.append({"mod": record["packageId"], "dependency": dependency})
    output = PROJECT / "Documentation"
    output.mkdir(exist_ok=True)
    (output / "inventory.json").write_text(json.dumps({
        "gameVersion": (MODS.parent / "Version.txt").read_text(encoding="utf-8-sig").strip(),
        "targetVersion": "1.6",
        "installedCount": len(records),
        "includedCount": len(included),
        "excludedCount": len(records) - len(included),
        "missingRequiredPackagesFromIncludedSet": missing,
        "notes": [
            "Это состав проекта, не готовый объединённый мод.",
            "Наличие папки Russian не доказывает полноту перевода для 1.6.",
            "Наличие исходников старых версий не доказывает наличие исходников 1.6.",
            "Оригинальные моды и настройки игры не изменены.",
        ],
        "mods": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    rows = ["# Состав проекта", "", "Цель: один самостоятельный мод для RimWorld 1.6 с русской локализацией.",
            "", "| Исходный мод | Решение | Русские папки во всех версиях |",
            "| --- | --- | --- |"]
    for record in records:
        rows.append(f"| {record['name']} | {'Включить' if record['include'] else 'Исключить'} | "
                    f"{len(record['russianDirectoriesAllVersions'])} |")
    rows += ["", "Перечень папок перевода — предварительная инвентаризация, не проверка полноты.",
             "", f"Включено: {len(included)}. Исключено: {len(records) - len(included)}.",
             f"Отсутствующих обязательных зависимостей по метаданным: {len(missing)}.",
             "Порядок загрузки, совместимость кода и запуск игры ещё не проверены.", ""]
    (output / "MODS.md").write_text("\n".join(rows), encoding="utf-8")
    print(json.dumps({"installed": len(records), "included": len(included),
                      "excluded": len(records) - len(included), "missingDependencies": missing},
                     ensure_ascii=False))


if __name__ == "__main__":
    main()
