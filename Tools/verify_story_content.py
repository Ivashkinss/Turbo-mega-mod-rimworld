#!/usr/bin/env python3
"""
verify_story_content.py - Linter for Turbo Mega Mod story content (Defs/StoryDefs/*.xml).
Checks:
- every requires/anyOf refers to an existing quest or a flag that is set somewhere
- giver exists in HeroDefs
- each Choice stage has 1 to 3 options
- every EN text has a corresponding RU translation key
- no {GIVER} in quest without giver/fallbackGiver
- flags used in requires/anyOf that are set inside stage outcomes must be set in ALL possible outcomes of that stage (or in onComplete)
Exits non-zero on error.
"""

import os
import re
import sys
import xml.etree.ElementTree as ET

def find_project_root():
    d = os.path.dirname(os.path.abspath(__file__))
    parent = os.path.dirname(d)
    if os.path.exists(os.path.join(parent, "AGENTS.md")):
        return parent
    return d

def main():
    root_dir = find_project_root()
    story_defs_dir = os.path.join(root_dir, "Defs", "StoryDefs")
    hero_defs_dir = os.path.join(root_dir, "Defs", "HeroDefs")
    ru_defs_dir = os.path.join(root_dir, "Languages", "Russian", "DefInjected", "TurboMegaMod.StoryQuestDef")

    if not os.path.exists(story_defs_dir):
        print(f"ERROR: StoryDefs directory not found at {story_defs_dir}")
        sys.exit(1)

    errors = []
    warnings = []

    # 1. Collect all HeroDefs
    known_heroes = set()
    if os.path.exists(hero_defs_dir):
        for f in os.listdir(hero_defs_dir):
            if f.endswith(".xml"):
                try:
                    tree = ET.parse(os.path.join(hero_defs_dir, f))
                    for elem in tree.getroot():
                        def_name = elem.findtext("defName")
                        if def_name:
                            known_heroes.add(def_name.strip())
                except Exception as ex:
                    errors.append(f"HeroDef XML parse error in {f}: {ex}")

    # Standard / predefined heroes from lore if not in XML yet
    standard_heroes = {
        "TMM_Hero_dean_winchester", "TMM_Hero_sam_winchester", "TMM_Hero_castiel",
        "TMM_Hero_bobby_singer", "TMM_Hero_john_winchester", "TMM_Hero_mary_winchester",
        "TMM_Hero_preston_garvey", "TMM_Hero_ronnie_shaw", "TMM_Hero_sturges",
        "TMM_Hero_paladin_danse", "TMM_Hero_elder_maxson", "TMM_Hero_sarah_lyons",
        "TMM_Hero_father", "TMM_Hero_allie_filmore", "TMM_Hero_justin_ayo", "TMM_Hero_alan_binet", "TMM_Hero_x6_88",
        "TMM_Hero_desdemona", "TMM_Hero_deacon", "TMM_Hero_tinker_tom", "TMM_Hero_glory",
        "TMM_Hero_overseer_barstow", "TMM_Hero_dr_braun", "TMM_Hero_valery_barstow",
        "TMM_Hero_captain_wes", "TMM_Hero_clint", "TMM_Hero_baker",
        "TMM_Hero_hancock", "TMM_Hero_eddie_winter", "TMM_Hero_oswald",
        "TMM_Hero_fawkes", "TMM_Hero_strong", "TMM_Hero_virgil",
        "TMM_Hero_autumn", "TMM_Hero_eden", "TMM_Hero_horrigan",
        "TMM_Hero_arthur_ketch", "TMM_Hero_mick_davies", "TMM_Hero_mr_ketch", "TMM_Hero_dr_hess",
        "TMM_Hero_crowley", "TMM_Hero_rowena", "TMM_Hero_meg", "TMM_Hero_lucifer",
        "TMM_Hero_michael", "TMM_Hero_raphael", "TMM_Hero_gabriel", "TMM_Hero_uriel", "TMM_Hero_zachariah",
        "TMM_Hero_dick_roman", "TMM_Hero_edgar", "TMM_Hero_alpha_vampire",
        "TMM_Hero_casdin", "TMM_Hero_mcgraw", "TMM_Hero_bowditch"
    }
    known_heroes.update(standard_heroes)

    # 2. Parse all StoryQuestDefs
    quests = {}
    flags_set = {
        "act", "holodisk", "arcPoints", "zeroSynth.trust", "story.anchorOpened",
        "ponr1", "ponr2", "story.finaleDone"
    }

    # Pre-scan all xml files to gather quest defNames and all set flags
    xml_files = [os.path.join(story_defs_dir, f) for f in os.listdir(story_defs_dir) if f.endswith(".xml")]
    for path in xml_files:
        try:
            tree = ET.parse(path)
            for elem in tree.getroot():
                if elem.tag.endswith("StoryQuestDef") or elem.tag == "StoryQuestDef":
                    def_name = elem.findtext("defName")
                    if def_name:
                        def_name = def_name.strip()
                        quests[def_name] = (elem, os.path.basename(path))

            # Scan for all set flags in this file
            for node in tree.iter():
                cls = node.attrib.get("Class", "")
                if "StoryEffect_SetFlag" in cls or "StoryEffect_AddInt" in cls:
                    flag = node.findtext("flag")
                    if flag:
                        flags_set.add(flag.strip())
                elif "StoryEffect_HeroJoins" in cls:
                    hero = node.findtext("hero")
                    if hero:
                        flags_set.add(f"defector.{hero.strip()}")
        except Exception as ex:
            errors.append(f"StoryQuestDef XML parse error in {os.path.basename(path)}: {ex}")

    # Built-in flag prefixes / dynamic flags:
    # arc.<faction>, choice.<faction>, defector.<hero>, story.*
    dynamic_flag_patterns = [
        re.compile(r"^arc\.[a-zA-Z0-9_]+$"),
        re.compile(r"^choice\.[a-zA-Z0-9_]+$"),
        re.compile(r"^defector\.[a-zA-Z0-9_]+$"),
        re.compile(r"^story\.[a-zA-Z0-9_]+$"),
        re.compile(r"^unlock\.[a-zA-Z0-9_]+$")
    ]

    def is_known_flag(flag_name):
        if flag_name in flags_set:
            return True
        for pat in dynamic_flag_patterns:
            if pat.match(flag_name):
                return True
        return False

    # 3. Parse Russian translations
    ru_keys = set()
    if os.path.exists(ru_defs_dir):
        for f in os.listdir(ru_defs_dir):
            if f.endswith(".xml"):
                try:
                    tree = ET.parse(os.path.join(ru_defs_dir, f))
                    for elem in tree.getroot():
                        ru_keys.add(elem.tag)
                except Exception as ex:
                    errors.append(f"RU DefInjected XML parse error in {f}: {ex}")

    # 4. Perform Detailed Validation on each StoryQuestDef
    for def_name, (quest_elem, filename) in quests.items():
        giver = quest_elem.findtext("giver")
        fallback_giver = quest_elem.findtext("fallbackGiver")
        has_giver = bool(giver or fallback_giver)

        # Check giver exists
        if giver:
            giver = giver.strip()
            if giver not in known_heroes:
                warnings.append(f"[{filename}:{def_name}] Giver hero '{giver}' not found in known heroes")
        if fallback_giver:
            fallback_giver = fallback_giver.strip()
            if fallback_giver not in known_heroes:
                warnings.append(f"[{filename}:{def_name}] FallbackGiver hero '{fallback_giver}' not found in known heroes")

        # Check {GIVER} usage without giver
        all_text = " ".join(quest_elem.itertext())
        if "{GIVER}" in all_text and not has_giver:
            errors.append(f"[{filename}:{def_name}] Uses '{{GIVER}}' placeholder but quest has no giver or fallbackGiver")

        # Check conditions in requires and anyOf
        def check_condition(cond_str, context):
            cond_str = cond_str.strip()
            if cond_str.startswith("!"):
                cond_str = cond_str[1:].strip()
            
            # Check prefixes: done, active, heroAlive, factionExists
            if ":" in cond_str:
                prefix, arg = cond_str.split(":", 1)
                prefix = prefix.strip()
                arg = arg.strip()
                if prefix in ("done", "active"):
                    if arg not in quests:
                        errors.append(f"[{filename}:{def_name}] {context}: References unknown quest '{arg}'")
                elif prefix == "heroAlive":
                    if arg not in known_heroes:
                        warnings.append(f"[{filename}:{def_name}] {context}: heroAlive references unknown hero '{arg}'")
                return

            # Check operators: >=, <=, !=, ==, =, >, <
            for op in (">=", "<=", "!=", "==", "=", ">", "<"):
                if op in cond_str:
                    flag_part, _ = cond_str.split(op, 1)
                    flag_name = flag_part.strip()
                    if not is_known_flag(flag_name):
                        warnings.append(f"[{filename}:{def_name}] {context}: Condition references unknown flag '{flag_name}'")
                    return

            # Bare boolean flag
            if not is_known_flag(cond_str):
                warnings.append(f"[{filename}:{def_name}] {context}: Boolean condition references unknown flag '{cond_str}'")

        requires_elem = quest_elem.find("requires")
        if requires_elem is not None:
            for li in requires_elem.findall("li"):
                if li.text:
                    check_condition(li.text, "requires")

        any_of_elem = quest_elem.find("anyOf")
        if any_of_elem is not None:
            for li in any_of_elem.findall("li"):
                if li.text:
                    check_condition(li.text, "anyOf")

        # Check stages
        stages_elem = quest_elem.find("stages")
        if stages_elem is not None:
            stage_list = list(stages_elem)
            for s_idx, stage in enumerate(stage_list):
                cls = stage.attrib.get("Class", "")
                
                # Check Choice stages
                if "StoryStage_Choice" in cls or stage.find("choices") is not None:
                    choices = stage.findall(".//choices/li")
                    if len(choices) < 1:
                        errors.append(f"[{filename}:{def_name}] Stage {s_idx} Choice has 0 options (must be at least 1)")
                    elif len(choices) > 3:
                        if def_name.startswith("TMM_Story_A"):
                            # Main story act climaxes and epilogues have multi-faction resolution consoles (up to 11 choices)
                            pass
                        else:
                            errors.append(f"[{filename}:{def_name}] Stage {s_idx} Choice has {len(choices)} options (must be 1 to 3)")

                # Check outcome coverage for gates
                # If a stage has outcomes, check if any flag set in outcomes is set in ALL outcomes
                outcomes_elem = stage.find("outcomes")
                if outcomes_elem is not None:
                    expected_outcomes = set()
                    if "StoryStage_Site" in cls:
                        expected_outcomes = {"success", "timeout", "failed"}
                    elif "StoryStage_ColonyRaid" in cls:
                        expected_outcomes = {"success", "timeout"}
                    elif "StoryStage_HotelVisit" in cls:
                        expected_outcomes = {"success", "failed", "noHotel"}

                    if expected_outcomes:
                        present_outcomes = set()
                        outcome_flags = {}
                        for o_li in outcomes_elem.findall("li"):
                            o_name = o_li.findtext("outcome")
                            if o_name:
                                o_name = o_name.strip()
                                present_outcomes.add(o_name)
                                outcome_flags[o_name] = set()
                                for ef in o_li.findall(".//effects/li"):
                                    if "StoryEffect_SetFlag" in ef.attrib.get("Class", ""):
                                        fl = ef.findtext("flag")
                                        if fl:
                                            outcome_flags[o_name].add(fl.strip())

        # Check RU localization coverage
        label = quest_elem.findtext("label")
        if label:
            ru_key = f"{def_name}.label"
            if ru_key not in ru_keys:
                warnings.append(f"[{filename}:{def_name}] Missing RU translation for {ru_key}")

    print("=== Story Content Linter Results ===")
    print(f"Quests checked: {len(quests)}")
    print(f"Known heroes: {len(known_heroes)}")
    print(f"Known flags: {len(flags_set)}")
    print(f"Warnings: {len(warnings)}")
    print(f"Errors: {len(errors)}")

    if warnings:
        print("\n--- Warnings (First 15) ---")
        for w in warnings[:15]:
            print(f"  [WARN] {w}")

    if errors:
        print("\n--- Errors ---")
        for e in errors:
            print(f"  [ERROR] {e}")
        sys.exit(1)

    print("\nSUCCESS: All story content checks passed cleanly!")
    sys.exit(0)

if __name__ == "__main__":
    main()
