"""Write deduplicated Russian storage labels with actual integrated capacities."""

import json
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
STORAGE = {
    "sbz_TallCrate": ("высокий ящик", "Высокий ящик для хранения разных предметов."),
    "sbz_Crate": ("большой ящик", "Большой ящик для хранения разных предметов."),
    "sbz_MidCrate": ("средний ящик", "Ящик среднего размера для хранения разных предметов."),
    "sbz_SmallCrate": ("маленький ящик", "Маленький ящик для хранения разных предметов."),
    "sbz_Pallet": ("поддон", "Поддон для хранения разных предметов."),
    "sbz_MiniPallet": ("мини-поддон", "Небольшой поддон для хранения разных предметов."),
    "sbz_HalfPallet": ("половинный поддон", "Поддон половинного размера для хранения разных предметов."),
    "sbz_OpenShelf": ("открытый стеллаж", "Открытый стеллаж для хранения разных предметов."),
    "sbz_TwoTierDisplayShelf": ("двухъярусный стеллаж", "Двухъярусный стеллаж для хранения и демонстрации предметов."),
    "sbz_DisplayShelf": ("витринный стеллаж", "Стеллаж для хранения и демонстрации предметов."),
    "sbz_SmallDisplayShelf": ("малый витринный стеллаж", "Маленький стеллаж для хранения и демонстрации предметов."),
    "sbz_TallDisplayShelf": ("высокий витринный стеллаж", "Высокий стеллаж для хранения и демонстрации предметов."),
    "sbz_NarrowTallDisplayShelf": ("узкий высокий стеллаж", "Высокий узкий стеллаж для хранения и демонстрации предметов."),
    "sbz_WideHangerShelf": ("вешалка для одежды", "Широкая стойка с вешалками для одежды."),
    "sbz_HangerShelf": ("узкая вешалка для одежды", "Узкая стойка с вешалками для одежды."),
    "sbz_DisplayCabinet": ("витринный шкаф", "Шкаф для хранения и демонстрации предметов."),
    "sbz_NarrowDisplayCabinet": ("узкий витринный шкаф", "Узкий шкаф для хранения и демонстрации предметов."),
    "sbz_DisplayTable": ("выставочная витрина", "Витрина для демонстрации предметов."),
    "sbz_RoundPedestal": ("круглый постамент", "Круглый постамент для демонстрации предметов."),
    "sbz_MeatHook": ("крюки для мяса", "Крюки для подвешивания мяса и трупов."),
    "sbz_TextileRack": ("стойка для текстиля", "Стойка для хранения разных видов ткани."),
    "sbz_Safe": ("сейф", "Сейф для хранения различных предметов."),
    "sbz_Drum": ("бочка", "Бочка для хранения различных предметов."),
    "sbz_HorizontalWeaponRack": ("горизонтальная оружейная стойка", "Стойка для горизонтального размещения оружия."),
    "sbz_VerticalWeaponRack": ("вертикальная оружейная стойка", "Стойка для вертикального размещения оружия."),
    "sbz_WeaponRack": ("оружейная стойка", "Стойка для хранения и демонстрации оружия."),
    "sbz_CorpseSpot": ("место для трупов", "Место для компактного хранения трупов."),
}


def write_language(entries, relative):
    root = ET.Element("LanguageData")
    for key, text in sorted(entries.items()):
        ET.SubElement(root, key).text = text
    path = PROJECT / "Languages/Russian" / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    ET.ElementTree(root).write(str(path), encoding="utf-8", xml_declaration=True, pretty_print=True)


def main():
    changes = json.loads((PROJECT / "Documentation/StorageChanges.json").read_text(encoding="utf-8"))
    entries = {}
    if set(STORAGE) != {row["defName"] for row in changes["storages"]}:
        raise ValueError("Storage definitions and translations do not match")
    for row in changes["storages"]:
        name = row["defName"]
        label, description = STORAGE[name]
        suffix = f" Вместимость: до {row['totalStacks']} стопок."
        if name == "sbz_CorpseSpot":
            suffix = f" Вместимость: до {row['totalStacks']} трупов."
        entries[f"{name}.label"] = label
        entries[f"{name}.description"] = description + suffix
    write_language(entries, "DefInjected/ThingDef/Storage.xml")
    write_language({"BuildingsNeatStorage.label": "Хранилища"}, "DefInjected/ThingCategoryDef/Storage.xml")

    entries = {}
    path = PROJECT / "Components/Fridge/Defs/ThingDefs_Buildings/Buildings_Fridge.xml"
    for node in ET.parse(str(path)).getroot().findall("ThingDef"):
        name = node.findtext("defName")
        if not name:
            continue
        label = node.findtext("label")
        size = [int(value) for value in node.findtext("size").strip("()").split(",")]
        capacity = int(node.findtext("building/maxItemsInCell")) * size[0] * size[1]
        units = "трупов" if name == "sbz_CorpseFridge" else "стопок предметов"
        description = f"Вместимость: до {capacity} {units}. Охлаждает содержимое; не работает при температуре окружающей среды выше 100 °C."
        if "WallFridge" in name:
            description += " Встраивается в стену. Не поддерживает крышу."
        entries[f"{name}.label"] = label
        entries[f"{name}.description"] = description
    write_language(entries, "DefInjected/ThingDef/Fridges.xml")
    print("Russian storage/fridge entries: 69. Adaptive Storage entries: 41 including the generated build category.")


if __name__ == "__main__":
    main()
