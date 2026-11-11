"""Generate reviewed VEF keys while preserving the earlier faction-dialog file."""
import json
from pathlib import Path
from lxml import etree as ET
from audit_localization import entries
from verify_combat_localization import check_text
from combat_trade_sources import collect, STRING_FIELDS, INCLUDED_IDS

PROJECT = Path(__file__).resolve().parents[1]


def definition_fields():
    extra = {'verb', 'gerund'} - STRING_FIELDS
    dlc = {'ludeon.rimworld.' + name for name in ('royalty', 'ideology', 'biotech', 'anomaly', 'odyssey')} - INCLUDED_IDS
    STRING_FIELDS.update(extra)
    INCLUDED_IDS.update(dlc)
    try:
        return collect(PROJECT / 'Components/VanillaExpandedFramework')[0]
    finally:
        STRING_FIELDS.difference_update(extra)
        INCLUDED_IDS.difference_update(dlc)


def main():
    source, _ = entries([PROJECT / 'Components/VanillaExpandedFramework/Languages/English'])
    translations = json.loads((PROJECT / 'Translations/VanillaExpandedFramework.json').read_text(encoding='utf-8'))
    russian, duplicates = entries([PROJECT / 'Languages/Russian'])
    assert not duplicates, duplicates
    assert set(translations) <= {key[1] for key in source}, 'Unknown VEF translation keys'
    for key, item in source.items():
        value = translations.get(key[1], russian.get(key, {}).get('text'))
        assert value is not None, key
        check_text(item['text'].replace('\\n', '\n').strip(), value.replace('\\n', '\n').strip(), str(key))
    root = ET.Element('LanguageData')
    for key, text in translations.items():
        ET.SubElement(root, key).text = text
    ET.indent(root, space='  ')
    ET.ElementTree(root).write(str(PROJECT / 'Languages/Russian/Keyed/VanillaExpandedFramework.xml'), encoding='utf-8', xml_declaration=True, pretty_print=True)
    definitions = definition_fields()
    mapping = json.loads((PROJECT / 'Translations/VanillaExpandedFramework.Defs.json').read_text(encoding='utf-8'))
    output = {}
    for (kind, key), text in definitions.items():
        assert text in mapping, (kind, key, text)
        value = mapping[text]
        check_text(text, value, key)
        for token in ('TargetA', 'TargetB', 'TargetC'):
            assert text.count(token) == value.count(token), (key, token)
        output.setdefault(kind, {})[key] = value
    for kind, fields in output.items():
        root = ET.Element('LanguageData')
        for key, value in sorted(fields.items()):
            ET.SubElement(root, key).text = value
        ET.indent(root, space='  ')
        path = PROJECT / 'Languages/Russian/DefInjected' / kind / 'VanillaExpandedFramework.xml'
        path.parent.mkdir(parents=True, exist_ok=True)
        ET.ElementTree(root).write(str(path), encoding='utf-8', xml_declaration=True, pretty_print=True)
    print(json.dumps({'sourceKeys': len(source), 'newKeys': len(translations), 'earlierKeysPreserved': len(source) - len(translations), 'definitionFields': len(definitions)}))


if __name__ == '__main__':
    main()
