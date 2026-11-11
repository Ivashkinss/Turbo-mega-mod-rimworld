"""Import current Sidearms keys and translate the current 1.6 definition names."""
from pathlib import Path
from lxml import etree as E

PROJECT = Path(__file__).resolve().parents[1]

def save(items, path):
    root = E.Element('LanguageData')
    for key, text in items.items():
        E.SubElement(root, key).text = '\n'.join(line.rstrip() for line in text.split('\n')) if text is not None else None
    path.parent.mkdir(parents=True, exist_ok=True)
    E.indent(root, space='  ')
    E.ElementTree(root).write(str(path), encoding='utf-8', xml_declaration=True, pretty_print=True)

def main():
    source = PROJECT.parent / 'Simple Sidearms (Ношение двух видов оружия)/v1.6'
    values = {n.tag: n.text for n in E.parse(str(source / 'Languages/Russian/Keyed/Russian.xml')).getroot() if isinstance(n.tag, str)}
    save(values, PROJECT / 'Languages/Russian/Keyed/SimpleSidearms.xml')
    jobs = {'EquipSecondary.reportString': 'экипирует TargetA как вспомогательное оружие.',
            'ReequipSecondary.reportString': 'снова экипирует TargetA как вспомогательное оружие.',
            'ReequipSecondaryCombat.reportString': 'снова экипирует TargetA как вспомогательное оружие.'}
    save(jobs, PROJECT / 'Languages/Russian/DefInjected/JobDef/SimpleSidearms.xml')
    concepts = {
        'Concept_SimpleSidearmsBasic': ('SS: Вспомогательное оружие', 'Simple Sidearms\\n\\nЧтобы экипировать оружие как вспомогательное, нажмите на него правой кнопкой мыши и выберите «Экипировать как вспомогательное».\\n\\nПо умолчанию колонисты выбирают тип оружия, соответствующий их лучшему навыку или увлечению. Стрелки также переходят на оружие ближнего боя при вступлении в рукопашную схватку. Это поведение можно изменить в настройках мода.'),
        'Concept_SidearmsDropping': ('SS: Выбрасывание оружия', 'Оружие на сером фоне находится в инвентаре колониста и готово к использованию. Щёлкните правой кнопкой мыши, чтобы выбросить его.'),
        'Concept_SidearmsMissing': ('SS: Утерянное оружие', 'Оружие на тёмно-фиолетовом фоне отсутствует в снаряжении колониста. Он попытается найти замену при первой возможности. Щёлкните правой кнопкой мыши, чтобы забыть об этом оружии.'),
        'Concept_SimpleSidearmsPreference': ('SS: Предпочтения в оружии', 'Каждому колонисту можно назначить предпочтительный тип оружия. Варианты:\\nИспользовать дальнобойное оружие\\nВыбирать оружие по навыку или увлечению\\nИспользовать оружие ближнего боя'),
        'Concept_SimpleSidearmsAdvancedRanged': ('SS: Управление дальнобойным оружием', 'Щёлкните левой кнопкой мыши по переносимому дальнобойному оружию, чтобы назначить его основным. Если колонист предпочитает дальнобойное оружие либо выбирает по навыку и лучше владеет стрельбой, он будет брать это оружие вне активного боя. Оно также используется на охоте.\\n\\nПовторный щелчок закрепляет оружие независимо от дистанции и урона.\\n\\nПравый щелчок снимает закрепление или назначение основным.'),
        'Concept_SimpleSidearmsAdvancedMelee': ('SS: Управление оружием ближнего боя', 'Щёлкните левой кнопкой мыши по переносимому оружию ближнего боя, чтобы сделать его предпочтительным. Колонист будет использовать его, если предпочитает ближний бой либо выбирает по навыку и лучше владеет ближним боем, а также при вступлении в рукопашную схватку.\\n\\nПовторный щелчок закрепляет постоянное использование оружия.\\n\\nПравый щелчок снимает закрепление или предпочтение.'),
        'Concept_SimpleSidearmsAdvancedDrafted': ('SS: Оружие призванного колониста', 'Щёлкните левой кнопкой мыши по переносимому оружию призванного колониста, чтобы закрепить его до конца боя.\\n\\nБольшинство изменений набора оружия доступны только после отмены призыва.'),
        'Concept_SimpleSidearmsLosingWeapons': ('SS: Потеря оружия', 'В зависимости от настроек Simple Sidearms колонисты могут бросать оружие в различных ситуациях — по умолчанию при атаках в ближнем бою. После боя они снова подберут его.\\n\\nЭто поведение можно изменить в настройках мода.'),
        'Concept_CEOverride': ('SS: Патч для CE', 'Загружен Combat Extended. Управление инвентарём Simple Sidearms отключено.'),
    }
    fields = {name + '.' + field: text for name, pair in concepts.items() for field, text in zip(('label', 'helpText'), pair)}
    save(fields, PROJECT / 'Languages/Russian/DefInjected/ConceptDef/SimpleSidearms.xml')
    print(len(values), 'Keyed,', len(jobs) + len(fields), 'definition fields')

if __name__ == '__main__':
    main()
