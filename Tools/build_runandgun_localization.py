"""Import RunAndGun Russian and add the missing Combat Extended dialog keys."""
from pathlib import Path
from lxml import etree as E

PROJECT=Path(__file__).resolve().parents[1]

def main():
    src=PROJECT.parent/'RunAndGun (Стрельба на ходу)/Languages/Russian/Keyed/RunAndGun_Keys.xml'
    english=E.parse(str(PROJECT/'Components/RunAndGun/Languages/English/Keyed/RunAndGun_Keys.xml')).getroot()
    values={n.tag:n.text for n in E.parse(str(src)).getroot() if isinstance(n.tag,str)}
    values.update({
        'RG_Dialog_CE_Title':'Настройка RunAndGun для Combat Extended',
        'RG_Dialog_CE_Description':'Похоже, вы впервые запускаете эту версию RunAndGun вместе с Combat Extended. Эти моды совместимы, но стрельба на ходу подходит не для каждого оружия. \\nРекомендуется разрешить Combat Extended настроить список запрещённого оружия. При этом ранее заданный список запретов RunAndGun будет заменён; остальные настройки сохранятся.',
        'RG_Dialog_CE_ConfirmButton':'Разрешить',
        'RG_Dialog_CE_DisallowButton':'Не разрешать',
    })
    root=E.Element('LanguageData')
    values['RG_Dialog_CE_Description']=values['RG_Dialog_CE_Description'].replace('\\n','\n').replace(' \n','\n')
    for n in english:
        if isinstance(n.tag,str):
            assert n.tag in values,n.tag
            E.SubElement(root,n.tag).text=values[n.tag]
    E.indent(root,space='  ')
    E.ElementTree(root).write(str(PROJECT/'Languages/Russian/Keyed/RunAndGun.xml'),encoding='utf-8',xml_declaration=True,pretty_print=True)
    print('RunAndGun:',len(root),'Russian keys')

if __name__=='__main__':main()
