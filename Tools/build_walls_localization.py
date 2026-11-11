"""Add XML Extensions translation keys without rewriting any gameplay settings."""
from pathlib import Path
from lxml import etree as E

PROJECT = Path(__file__).resolve().parents[1]
RUSSIAN = {
    (0, 'text'): 'ИЗМЕНЕНИЯ ТРЕБУЮТ ПЕРЕЗАПУСКА',
    (2, 'text'): 'Урон зданиям умножается на указанные коэффициенты. \\nНепроходимые сооружения — это прежде всего стены.',
    (3, 'label'): 'Режущие атаки в ближнем бою: {SharpMeleePassable} [по умолчанию: 0.4]',
    (4, 'label'): 'Режущие атаки по непроходимым сооружениям: {SharpMeleeImpassable} [по умолчанию: 0.1]',
    (5, 'label'): 'Дробящий урон: {BluntPassable} [по умолчанию: 0.5]',
    (6, 'label'): 'Дробящий урон непроходимым сооружениям: {BluntImpassable} [по умолчанию: 0.3]',
    (7, 'label'): 'Режущие атаки на расстоянии: {SharpRangedPassable} [по умолчанию: 0.2]',
    (8, 'label'): 'Режущие атаки на расстоянии по непроходимым сооружениям: {SharpRangedImpassable} [по умолчанию: 0.2]',
    (9, 'label'): 'Взрывы: {BombPassable} [по умолчанию: 0.8]',
    (10, 'label'): 'Взрывы по непроходимым сооружениям: {BombImpassable} [по умолчанию: 1]',
    (11, 'text'): 'Служебные имена типов урона в каждой категории:',
    (12, 'text'): 'Режущий урон в ближнем бою: Cut, Poke, Stab, Scratch, Bite',
    (13, 'text'): 'Дробящий урон: Blunt, Crush',
    (14, 'text'): 'Режущий урон на расстоянии: RangedStab, Bullet, Arrow',
    (15, 'text'): 'Взрыв: Bomb',
    (16, 'text'): 'Только у Bomb оба коэффициента уже были заданы: 2 для проходимых и 4 для непроходимых сооружений.',
    (18, 'label'): 'Отменить изменения для турелей, ульев, обломков и зданий мехкластеров',
    (18, 'tooltip'): 'При отключении для разрушения таких объектов чаще потребуется штурмовой топор, зато ваши турели станут прочнее.',
    (19, 'label'): 'Природный камень не получает урон от дальнобойного оружия',
    (19, 'tooltip'): 'Умножать урон природным каменным стенам от дальнобойных атак на ноль.',
}

def save(root, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    E.indent(root, space='  ')
    E.ElementTree(root).write(str(path), encoding='utf-8', xml_declaration=True, pretty_print=True)

def main():
    menu = E.parse(str(PROJECT / 'Components/WallsAreSolid/Defs/WallsAreSolid_Settings.xml')).getroot()[0]
    base = 'Defs/XmlExtensions.SettingsMenuDef[defName="Victor.WallsAreSolid.Settings"]'
    patch, english, russian = E.Element('Patch'), E.Element('LanguageData'), E.Element('LanguageData')
    records = [(base, 'tKey', 'TMM.Walls.Menu', menu.findtext('label'), 'Прочные стены')]
    for (index, field), translated in RUSSIAN.items():
        original = menu.find('settings')[index].findtext(field)
        assert original is not None, (index, field)
        records.append((f'{base}/settings/li[{index + 1}]', 'tKeyTip' if field == 'tooltip' else 'tKey', f'TMM.Walls.{index}.{field}', original, translated))
    for xpath, tag, key, source, translated in records:
        op = E.SubElement(patch, 'Operation', Class='PatchOperationAdd')
        E.SubElement(op, 'xpath').text = xpath
        E.SubElement(E.SubElement(op, 'value'), tag).text = key
        E.SubElement(english, key).text = source
        E.SubElement(russian, key).text = translated
    save(patch, PROJECT / 'Patches/WallsLocalization.xml')
    save(english, PROJECT / 'Languages/English/Keyed/WallsAreSolid.xml')
    save(russian, PROJECT / 'Languages/Russian/Keyed/WallsAreSolid.xml')
    for kind, key, text in [('ToolCapacityDef', 'BluntCrushing.label', 'разрушительный удар'), ('DamageDef', 'BluntCrushing.label', 'разрушительный удар')]:
        root = E.Element('LanguageData'); E.SubElement(root, key).text = text
        save(root, PROJECT / 'Languages/Russian/DefInjected' / kind / 'WallsAreSolid.xml')
    print(len(records), 'XML Extensions keys, 2 definition fields')

if __name__ == '__main__':
    main()
