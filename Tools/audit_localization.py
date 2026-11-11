"""Static localization audit for the selected RimWorld 1.6 components.

Only writes to this project's Documentation directory. It does not alter mods.
Run with the bundled Python runtime (lxml is required).
"""

import json
import re
from collections import Counter, defaultdict
from pathlib import Path

from lxml import etree as ET
from assembly_strings import user_strings

PROJECT = Path(__file__).resolve().parents[1]
MODS = PROJECT.parent
DOCS = PROJECT / "Documentation"
FIELDS = {
    "label", "description", "labelShort", "labelNoun", "labelPlural", "jobString",
    "reportString", "gerund", "verb", "deathMessage", "recoveryMessage", "pawnLabel",
    "title", "beginLetter", "endMessage", "message", "skillLabel", "customLabel", "noun",
}
CYRILLIC = re.compile(r"[А-Яа-яЁё]")
LATIN = re.compile(r"[A-Za-z]{2,}")
TOKEN = re.compile(r"(?<!\{)\{([A-Za-z_][A-Za-z_0-9.]*|\d+)(?:[:,][^{}]*)?\}(?!\})")
VERSION = re.compile(r"v?(\d+)\.(\d+)$")
ERRORS = []


def read_xml(path):
    try:
        return ET.parse(str(path), ET.XMLParser(remove_comments=True)).getroot()
    except ET.XMLSyntaxError as error:
        ERRORS.append({"file": str(path.relative_to(MODS)), "error": str(error)})
        return None


def relative(path):
    return path.relative_to(MODS).as_posix()


def enabled(attributes, ids):
    # Matches Verse.LoadFolder.ShouldLoad: positive-any, positive-all,
    # and negative-any. MayRequire is the XML def requirement (all).
    for attr, operation in (("IfModActive", any), ("IfModActiveAll", all),
                            ("IfModNotActive", lambda values: not any(values)), ("MayRequire", all)):
        if attr in attributes:
            if not operation(x.strip().lower() in ids for x in attributes[attr].split(",")):
                return False
    return True


def load_roots(folder, ids):
    manifest = folder / "LoadFolders.xml"
    notes = []
    if manifest.is_file():
        tree = read_xml(manifest)
        choices = [] if tree is None else [
            (tuple(map(int, VERSION.fullmatch(x.tag).groups())), x)
            for x in tree if isinstance(x.tag, str) and VERSION.fullmatch(x.tag)
        ]
        choices = [entry for entry in choices if entry[0] <= (1, 6)]
        if choices:
            version, node = max(choices, key=lambda entry: entry[0])
            if version != (1, 6):
                notes.append(f"В LoadFolders.xml нет v1.6; для аудита использован fallback v{version[0]}.{version[1]}. Требуется проверка загрузки в игре.")
            roots = [folder / (entry.text or "").strip().strip("/\\")
                     for entry in node.findall("li") if enabled(entry.attrib, ids)]
            return list(dict.fromkeys(roots)), notes
    versions = [(tuple(map(int, VERSION.fullmatch(x.name).groups())), x)
                for x in folder.iterdir() if x.is_dir() and VERSION.fullmatch(x.name)]
    versions = [entry for entry in versions if entry[0] <= (1, 6)]
    roots = [folder]
    if versions:
        version, path = max(versions, key=lambda entry: entry[0])
        roots.append(path)
        if version != (1, 6):
            notes.append(f"Нет папки 1.6; для статического аудита взята {path.name}. Требуется проверка выбора версии игрой.")
    return roots, notes


def languages(roots, prefix):
    return [path for root in roots if (root / "Languages").is_dir()
            for path in sorted((root / "Languages").iterdir())
            if path.is_dir() and path.name.lower().startswith(prefix.lower())]


def entries(directories):
    values = {}
    duplicates = []
    for directory in directories:
        for path in sorted(directory.rglob("*.xml")):
            parts = path.relative_to(directory).parts
            if len(parts) < 2 or parts[0] not in {"Keyed", "DefInjected"}:
                continue
            namespace = "Keyed" if parts[0] == "Keyed" else "/".join(parts[:2])
            root = read_xml(path)
            if root is None:
                continue
            for node in root:
                if not isinstance(node.tag, str):
                    continue
                key = (namespace, node.tag)
                item = {"namespace": namespace, "key": node.tag,
                        "text": "".join(node.itertext()).strip(), "file": relative(path), "line": node.sourceline}
                if key in values:
                    duplicates.append({"namespace": namespace, "key": node.tag,
                                       "sameText": values[key]["text"] == item["text"],
                                       "first": values[key], "second": item})
                values[key] = item
    return values, duplicates


def def_fields(node, path=""):
    list_index = 0
    for child in node:
        if not isinstance(child.tag, str):
            continue
        segment = str(list_index) if child.tag == "li" else child.tag
        if child.tag == "li":
            list_index += 1
        current = f"{path}.{segment}" if path else segment
        if len(child):
            yield from def_fields(child, current)
        elif child.tag in FIELDS and (child.text or "").strip():
            yield current, child.text.strip(), child.sourceline


def audit_mod(mod, ids):
    folder = MODS / mod["folder"]
    roots, notes = load_roots(folder, ids)
    en, en_dup = entries(languages(roots, "English"))
    ru, ru_dup = entries(languages(roots, "Russian"))
    required = dict(en)
    direct_ru = []
    defs = []
    abstract = {}
    for root in roots:
        if not (root / "Defs").is_dir():
            continue
        for path in sorted((root / "Defs").rglob("*.xml")):
            tree = read_xml(path)
            if tree is None:
                continue
            for definition in tree:
                if not isinstance(definition.tag, str) or not enabled(definition.attrib, ids):
                    continue
                fields = {name: {"text": text, "file": relative(path), "line": line}
                          for name, text, line in def_fields(definition)}
                if definition.get("Name"):
                    abstract[definition.get("Name")] = (definition, fields)
                if definition.findtext("defName"):
                    defs.append((definition, fields))
    for definition, own_fields in defs:
        chain = []
        parent = definition.get("ParentName")
        seen = set()
        while parent in abstract and parent not in seen:
            seen.add(parent)
            ancestor, fields = abstract[parent]
            chain.append(fields)
            parent = ancestor.get("ParentName")
        merged = {}
        for fields in reversed(chain):
            merged.update(fields)
        merged.update(own_fields)
        for field, value in merged.items():
            key = (f"DefInjected/{definition.tag}", f"{definition.findtext('defName')}.{field}")
            item = {"namespace": key[0], "key": key[1], **value}
            if CYRILLIC.search(value["text"]):
                direct_ru.append(item)
            elif LATIN.search(value["text"]):
                required.setdefault(key, item)
    internal = [value for value in required.values() if value['text'].startswith('dummy_')]
    required = {key: value for key, value in required.items() if not value['text'].startswith('dummy_')}
    patch_candidates = []
    for root in roots:
        for path in (root / "Patches").rglob("*.xml"):
            tree = read_xml(path)
            if tree is None:
                continue
            for definition in tree.xpath(".//*[defName]"):
                for field, text, line in def_fields(definition):
                    if LATIN.search(text) and not CYRILLIC.search(text):
                        patch_candidates.append({"namespace": f"DefInjected/{definition.tag}",
                                                 "key": f"{definition.findtext('defName')}.{field}",
                                                 "text": text, "file": relative(path), "line": line})
    missing, identical, mismatch, empty = [], [], [], []
    for key, original in sorted(required.items()):
        translated = ru.get(key)
        if translated is None:
            missing.append(original)
        elif not translated["text"]:
            empty.append({"original": original, "translation": translated})
        elif translated["text"] == original["text"] and LATIN.search(original["text"]):
            identical.append({"original": original, "translation": translated})
        elif Counter(TOKEN.findall(original["text"])) != Counter(TOKEN.findall(translated["text"])):
            mismatch.append({"original": original, "translation": translated})
    assemblies = [relative(path) for root in roots if (root / "Assemblies").is_dir()
                  for path in sorted((root / "Assemblies").rglob("*.dll"))]
    embedded = []
    for assembly in assemblies:
        try:
            strings = user_strings(MODS / assembly)
            russian = [text for text in strings if CYRILLIC.search(text)]
            english_ui = [text for text in strings if len(text) < 350 and len(text.split()) >= 3
                          and LATIN.search(text) and not CYRILLIC.search(text)
                          and not text.startswith(('http', 'System.', 'UnityEngine.', 'Verse.'))]
            embedded.append({'file': assembly, 'stringCount': len(strings),
                             'cyrillicStringCount': len(russian),
                             'cyrillicSamples': russian[:5], 'englishCandidates': english_ui[:20]})
        except (ValueError, UnicodeError, IndexError) as error:
            embedded.append({'file': assembly, 'error': str(error)})
    polluted = [value for value in ru.values() if re.search(r'\[cite[:_]|\[citation', value['text'])]
    english_tails = [value for value in ru.values() if CYRILLIC.search(value['text'])
                     and re.search(r'\([A-Za-z][^()]* [A-Za-z][^()]*\)', value['text'])]
    if assemblies:
        notes.append("Текст внутри DLL, динамические ключи и программно встроенные переводы требуют отдельной проверки; XML-аудит их не покрывает.")
    if not required and not direct_ru:
        notes.append("Нет обнаруженных текстов для перевода в выбранных XML; это не подтверждение полной русификации.")
    return {"name": mod["name"], "packageId": mod["packageId"], "folder": mod["folder"],
            "roots": [relative(root) for root in roots], "notes": notes,
            "requiredCount": len(required), "russianEntryCount": len(ru),
            "directRussianDefFields": direct_ru, "missing": missing,
            "identicalEnglishCandidates": identical, "placeholderMismatches": mismatch,
            "emptyTranslations": empty, "duplicateRussianKeys": ru_dup,
            "duplicateEnglishKeys": en_dup, "assemblies": assemblies,
            "internalPlaceholderFields": internal, "pollutedRussianEntries": polluted,
            "englishTailCandidates": english_tails, "assemblyStringInspection": embedded,
            "conditionalPatchTextCandidates": patch_candidates}


def main():
    inventory = json.loads((DOCS / "inventory.json").read_text(encoding="utf-8"))
    mods = [mod for mod in inventory["mods"] if mod["include"]]
    ids = {mod["packageId"].lower() for mod in mods}
    ids.add("ludeon.rimworld")
    # Optional DLC resources are not assumed active: list these audit assumptions explicitly.
    results = [audit_mod(mod, ids) for mod in mods]
    report = {"targetVersion": "1.6", "assumptions": [
        "Состав проекта: 29 выбранных компонентов и Core; DLC и внешние интеграции не предполагаются активными.",
        "Сверены Keyed, DefInjected и распространённые переводимые поля Defs; это статический аудит, не игровой translation report.",
        "Идентичные английские строки — кандидаты на проверку, не всегда ошибки (названия, сокращения).",
        "Часть абстрактных Defs и нестандартных текстовых полей, XML-патчи, Strings, DLL и динамические ключи требуют дальнейшей проверки.",
        "Прямой русский текст в Defs учитывается, даже если папки Russian нет.",
        "Повторяющиеся русские ключи требуют устранения; фактическое разрешение конфликтов игрой не предполагается.",
        "Показано отсутствие собственного RU у компонента; общий перевод из Core или другого мода может покрывать отдельные совпадающие ключи.",
        "Строки извлечены из #US DLL без исполнения кода; английские кандидаты могут быть логами и не доказывают отсутствие программного перевода.",
    ], "xmlErrors": ERRORS, "mods": results}
    (DOCS / "LocalizationAudit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    rows = ["# Аудит русификации исходных компонентов — RimWorld 1.6", "",
            "Отчёт описывает установленные оригиналы до изменений в ColonyProject.", "",
            "| Мод | Текстов XML для сверки | Нет своего RU | Совпадает с EN¹ | Параметры² | Повторы RU |",
            "| --- | ---: | ---: | ---: | ---: | ---: |"]
    for mod in results:
        rows.append(f"| {mod['name']} | {mod['requiredCount']} | {len(mod['missing']) + len(mod['emptyTranslations'])} | "
                    f"{len(mod['identicalEnglishCandidates'])} | {len(mod['placeholderMismatches'])} | {len(mod['duplicateRussianKeys'])} |")
    rows += ["", "¹ Совпадение с английским требует ручной проверки, например имя автора переводить не нужно.",
             "² Различия подстановок {0}, {name} требуют проверки формата.", "", "## Ограничения", ""]
    rows += [f"- {note}" for note in report["assumptions"]]
    for mod in results:
        if mod["missing"] or mod["duplicateRussianKeys"] or mod["placeholderMismatches"] or mod["notes"]:
            rows += ["", f"## {mod['name']}", ""]
            rows += [f"- {note}" for note in mod["notes"]]
            if mod['pollutedRussianEntries']:
                rows.append(f"- Посторонние маркеры [cite: …] в {len(mod['pollutedRussianEntries'])} русских строках.")
            if mod['englishTailCandidates']:
                rows.append(f"- Русские строки с английскими хвостами: {len(mod['englishTailCandidates'])} кандидатов на ручную проверку.")
            if mod['conditionalPatchTextCandidates']:
                rows.append(f"- Текстов в потенциально условных XML-патчах: {len(mod['conditionalPatchTextCandidates'])}; вынесены отдельно и не прибавлены к пропускам.")
            cyrillic = sum(x.get('cyrillicStringCount', 0) for x in mod['assemblyStringInspection'])
            if cyrillic:
                rows.append(f"- В DLL найдено строк с кириллицей: {cyrillic}; встроенный перевод нужно проверить отдельно в интерфейсе.")
            rows += [f"- Пример пропуска: `{entry['key']}` — {entry['text'].replace(chr(10), ' ')[:170]}"
                     for entry in mod["missing"][:3]]
    rows += ["", "## Ошибки XML", ""]
    rows += [f"- `{error['file']}`: {error['error']}" for error in ERRORS]
    rows += ["", "Все найденные ключи, исходные строки, файлы и номера строк находятся в LocalizationAudit.json.", ""]
    (DOCS / "LOCALIZATION-AUDIT.md").write_text("\n".join(rows), encoding="utf-8")
    print(json.dumps({"mods": len(results), "missingXmlEntries": sum(len(x['missing']) for x in results),
                      "xmlErrors": len(ERRORS)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
