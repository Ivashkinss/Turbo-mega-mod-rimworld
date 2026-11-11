"""Import and complete Snap Out Russian; requires the original only for reimport."""
from pathlib import Path
from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
SOURCE = PROJECT.parent / 'Snap Out! (Успокоение срывающихся колонистов)/Languages/Russian'

def write(root, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    ET.indent(root, space='  ')
    ET.ElementTree(root).write(str(path), encoding='utf-8', xml_declaration=True, pretty_print=True)

def main():
    english = ET.parse(str(PROJECT / 'Components/SnapOut/Languages/English/Keyed/CalmDown.xml')).getroot()
    russian = ET.parse(str(SOURCE / 'Keyed/CalmDown.xml')).getroot()
    values = {node.tag: node.text for node in russian if isinstance(node.tag, str)}
    values.update({
        'SettingsCategoryLabel': 'Успокоение при срыве',
        'CapLabel': 'Предел влияния переговоров',
        'LaunchCounterSetting': 'Счётчик запусков',
        'AlwaysSucceedSetting': 'Всегда успешно (отладка)',
        'DefaultButton': 'Сбросить расширенные настройки',
        'CM18': 'Ты не один, мы все рядом.',
        'CM20': 'Можешь выговориться, я тебя выслушаю.',
    })
    result = ET.Element('LanguageData')
    for node in english:
        if isinstance(node.tag, str):
            assert node.tag in values, node.tag
            ET.SubElement(result, node.tag).text = values[node.tag]
    write(result, PROJECT / 'Languages/Russian/Keyed/SnapOut.xml')
    for kind, filename in [('JobDef','Jobs_SnapOut'), ('ThoughtDef','Thoughts_SnapOut'),
                            ('InteractionDef','Interactions_SnapOut'), ('WorkGiverDef','Jobs_CalmDown')]:
        root = ET.parse(str(SOURCE / 'DefInjected' / kind / (filename+'.xml')), ET.XMLParser(remove_comments=True)).getroot()
        if kind == 'JobDef':
            root.find('SnapOut_ActuallyRecoverJob.reportString').text = 'проводит время наедине.'
        write(root, PROJECT / 'Languages/Russian/DefInjected' / kind / 'SnapOut.xml')
    print('Imported Snap Out translations:', len(result), 'Keyed, 12 definition fields.')

if __name__ == '__main__':
    main()
