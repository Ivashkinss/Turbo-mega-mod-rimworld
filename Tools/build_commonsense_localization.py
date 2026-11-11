"""Import Common Sense Russian text, complete missing keys and definition fields."""
from pathlib import Path
from lxml import etree as E

PROJECT = Path(__file__).resolve().parents[1]

def save(root, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    E.indent(root, space='  ')
    E.ElementTree(root).write(str(path), encoding='utf-8', xml_declaration=True, pretty_print=True)

def main():
    source = PROJECT.parent / 'Common Sense (Умные действия колонистов)/Languages/Russian/Keyed/strings.xml'
    english = E.parse(str(PROJECT / 'Components/CommonSense/Languages/English/Keyed/strings.xml')).getroot()
    values = {n.tag:n.text for n in E.parse(str(source)).getroot() if isinstance(n.tag,str)}
    values.update({
        'manual_unload_label':'Показывать в инвентаре кнопку отметки вещей для переноса на склад',
        'manual_unload_note':'Если подходящего склада нет, персонаж положит вещь рядом с собой.',
        'meditation_economy_label':'Персонажи медитируют только столько, сколько необходимо',
    })
    root=E.Element('LanguageData')
    for n in english:
        if isinstance(n.tag,str):
            assert n.tag in values,n.tag
            E.SubElement(root,n.tag).text=values[n.tag]
    save(root,PROJECT/'Languages/Russian/Keyed/CommonSense.xml')
    fields = {
        'JobDef': {'UnloadMarkedItems.reportString':'выгружает инвентарь.', 'SocialRelaxCommonSense.reportString':'общается и отдыхает.'},
        'RoomRoleDef': {'CooledStoreroom.label':'холодильная камера'},
        'RecipeDef': {'avCSDummyRecipeForNutrientPaste.label':'питательная паста', 'avCSDummyRecipeForNutrientPaste.description':'Приготовление питательной пасты.', 'avCSDummyRecipeForNutrientPaste.jobString':'готовит питательную пасту.'},
    }
    for kind,items in fields.items():
        tree=E.Element('LanguageData')
        for key,text in items.items(): E.SubElement(tree,key).text=text
        save(tree,PROJECT/'Languages/Russian/DefInjected'/kind/'CommonSense.xml')
    print('Common Sense:',len(root),'Keyed,',sum(map(len,fields.values())),'definition fields')

if __name__=='__main__':main()
