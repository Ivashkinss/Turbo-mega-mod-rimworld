"""Translate current Pick Up And Haul settings and definition fields."""
from pathlib import Path
from lxml import etree as E

PROJECT = Path(__file__).resolve().parents[1]

def save(values, path):
    root = E.Element('LanguageData')
    for key, text in values.items():
        E.SubElement(root, key).text = text
    path.parent.mkdir(parents=True, exist_ok=True)
    E.indent(root, space='  ')
    E.ElementTree(root).write(str(path), encoding='utf-8', xml_declaration=True, pretty_print=True)

def main():
    values = {
        'PUAH.allowCorpses': 'Разрешить переноску трупов в инвентаре',
        'PUAH.allowCorpsesTooltip': 'По умолчанию выключено: трупы переносятся только в руках.',
        'PUAH.allowAnimals': 'Разрешить приручённым вьючным животным переносить вещи в инвентаре',
        'PUAH.allowAnimalsTooltip': 'По умолчанию включено. Лучше всего работает с модами, добавляющими вьючное снаряжение другим животным, например VFME Caravan Packs.',
        'PUAH.allowMechanoids': 'Разрешить взломанным механоидам переносить вещи в инвентаре',
        'PUAH.allowMechanoidsTooltip': 'Требуется What The Hack или аналогичный мод. По умолчанию включено.',
        'PUAH.minimumFreeInventorySpace': 'Минимальная доля свободного места для переноски в инвентаре',
        'PUAH.minimumFreeInventorySpaceTooltip': 'Если свободного места меньше указанного, персонаж использует обычную переноску в руках. При 0% он всегда пытается собирать вещи в инвентарь, даже если помещается всего одна картофелина. При 100% требуется полное отсутствие одежды и снаряжения. По умолчанию 20%.',
    }
    save(values, PROJECT / 'Languages/Russian/Keyed/PickUpAndHaul.xml')
    save({'HaulToInventory.label': 'собирать вещи в инвентарь и переносить',
          'HaulToInventory.verb': 'перенести', 'HaulToInventory.gerund': 'переносит'},
         PROJECT / 'Languages/Russian/DefInjected/WorkGiverDef/PickUpAndHaul.xml')
    save({'HaulToInventory.reportString': 'собирает TargetA в инвентарь для переноски.',
          'UnloadYourHauledInventory.reportString': 'разгружает инвентарь.'},
         PROJECT / 'Languages/Russian/DefInjected/JobDef/PickUpAndHaul.xml')
    print('Pick Up And Haul: 8 Keyed, 5 definition fields')

if __name__ == '__main__':
    main()
