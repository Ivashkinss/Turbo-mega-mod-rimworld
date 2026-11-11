"""Import selected utility binaries/resources; never copy bundled Harmony."""
import hashlib
import json
import shutil
import argparse
from pathlib import Path

from lxml import etree as ET

PROJECT = Path(__file__).resolve().parents[1]
COMPONENTS = {
    "NiceBillTab": ("Nice Bill Tab (Фильтр рецептов)", "andromeda.nicebilltab", {
        "1.6/Assemblies/NiceBillTab.dll": "Assemblies/NiceBillTab.dll",
        "1.6/Assemblies/FloatSubMenu.dll": "Assemblies/FloatSubMenu.dll",
        "Languages/English/Keyed/Common.xml": "Languages/English/Keyed/Common.xml",
        "Textures/NiceBillTab/Ammo1.png": "Textures/NiceBillTab/Ammo1.png",
        "Textures/NiceBillTab/ApparelSlots.png": "Textures/NiceBillTab/ApparelSlots.png",
        "Textures/NiceBillTab/ArmorBlunt.png": "Textures/NiceBillTab/ArmorBlunt.png",
        "Textures/NiceBillTab/ArmorHeat.png": "Textures/NiceBillTab/ArmorHeat.png",
        "Textures/NiceBillTab/ArmorPen.png": "Textures/NiceBillTab/ArmorPen.png",
        "Textures/NiceBillTab/ArmorSharp.png": "Textures/NiceBillTab/ArmorSharp.png",
        "Textures/NiceBillTab/ArrowEdge.png": "Textures/NiceBillTab/ArrowEdge.png",
        "Textures/NiceBillTab/AtkSpeed.png": "Textures/NiceBillTab/AtkSpeed.png",
        "Textures/NiceBillTab/Beauty.png": "Textures/NiceBillTab/Beauty.png",
        "Textures/NiceBillTab/BurnTile.png": "Textures/NiceBillTab/BurnTile.png",
        "Textures/NiceBillTab/Checkbox_Checked.png": "Textures/NiceBillTab/Checkbox_Checked.png",
        "Textures/NiceBillTab/Checkbox_Empty.png": "Textures/NiceBillTab/Checkbox_Empty.png",
        "Textures/NiceBillTab/Cold.png": "Textures/NiceBillTab/Cold.png",
        "Textures/NiceBillTab/CoverArms.png": "Textures/NiceBillTab/CoverArms.png",
        "Textures/NiceBillTab/CoverBody.png": "Textures/NiceBillTab/CoverBody.png",
        "Textures/NiceBillTab/CoverHead.png": "Textures/NiceBillTab/CoverHead.png",
        "Textures/NiceBillTab/CoverLegs.png": "Textures/NiceBillTab/CoverLegs.png",
        "Textures/NiceBillTab/Damage.png": "Textures/NiceBillTab/Damage.png",
        "Textures/NiceBillTab/Efficiency.png": "Textures/NiceBillTab/Efficiency.png",
        "Textures/NiceBillTab/EnergyBarrier.png": "Textures/NiceBillTab/EnergyBarrier.png",
        "Textures/NiceBillTab/EnergyShield.png": "Textures/NiceBillTab/EnergyShield.png",
        "Textures/NiceBillTab/EnergyShieldRecharge.png": "Textures/NiceBillTab/EnergyShieldRecharge.png",
        "Textures/NiceBillTab/FadeGradient.png": "Textures/NiceBillTab/FadeGradient.png",
        "Textures/NiceBillTab/Feature.png": "Textures/NiceBillTab/Feature.png",
        "Textures/NiceBillTab/Glow.png": "Textures/NiceBillTab/Glow.png",
        "Textures/NiceBillTab/HGradient.png": "Textures/NiceBillTab/HGradient.png",
        "Textures/NiceBillTab/Hourglass.png": "Textures/NiceBillTab/Hourglass.png",
        "Textures/NiceBillTab/Idea.png": "Textures/NiceBillTab/Idea.png",
        "Textures/NiceBillTab/Ingot.png": "Textures/NiceBillTab/Ingot.png",
        "Textures/NiceBillTab/Ingot2.png": "Textures/NiceBillTab/Ingot2.png",
        "Textures/NiceBillTab/Mag.png": "Textures/NiceBillTab/Mag.png",
        "Textures/NiceBillTab/Mass.png": "Textures/NiceBillTab/Mass.png",
        "Textures/NiceBillTab/MechPower.png": "Textures/NiceBillTab/MechPower.png",
        "Textures/NiceBillTab/Medical.png": "Textures/NiceBillTab/Medical.png",
        "Textures/NiceBillTab/Medicine.png": "Textures/NiceBillTab/Medicine.png",
        "Textures/NiceBillTab/Move.png": "Textures/NiceBillTab/Move.png",
        "Textures/NiceBillTab/NoIconRecipe.png": "Textures/NiceBillTab/NoIconRecipe.png",
        "Textures/NiceBillTab/Nutrition.png": "Textures/NiceBillTab/Nutrition.png",
        "Textures/NiceBillTab/Pause.png": "Textures/NiceBillTab/Pause.png",
        "Textures/NiceBillTab/Play.png": "Textures/NiceBillTab/Play.png",
        "Textures/NiceBillTab/Process.png": "Textures/NiceBillTab/Process.png",
        "Textures/NiceBillTab/ProcessMask.png": "Textures/NiceBillTab/ProcessMask.png",
        "Textures/NiceBillTab/Range.png": "Textures/NiceBillTab/Range.png",
        "Textures/NiceBillTab/Reload.png": "Textures/NiceBillTab/Reload.png",
        "Textures/NiceBillTab/Royal.png": "Textures/NiceBillTab/Royal.png",
        "Textures/NiceBillTab/Settings.png": "Textures/NiceBillTab/Settings.png",
        "Textures/NiceBillTab/ShieldFull.png": "Textures/NiceBillTab/ShieldFull.png",
        "Textures/NiceBillTab/StoppingPower.png": "Textures/NiceBillTab/StoppingPower.png",
        "Textures/NiceBillTab/Surgery.png": "Textures/NiceBillTab/Surgery.png",
        "Textures/NiceBillTab/Surgery2.png": "Textures/NiceBillTab/Surgery2.png",
        "Textures/NiceBillTab/VGradient.png": "Textures/NiceBillTab/VGradient.png",
        "Textures/NiceBillTab/Warm.png": "Textures/NiceBillTab/Warm.png",
    }),
    "PickUpAndHaul": ("Pick Up And Haul (Переноска нескольких предметов)", "mehni.pickupandhaul", {
        "1.6/Assemblies/PickUpAndHaul.dll": "Assemblies/PickUpAndHaul.dll",
        "1.6/Assemblies/IHoldMultipleThings.dll": "Assemblies/IHoldMultipleThings.dll",
        "Defs/JobDefs/WorkGiver.xml": "Defs/JobDefs/WorkGiver.xml",
        "Patches/PickUpAndHaul.xml": "Patches/PickUpAndHaul.xml",
        "Languages/English/Keyed/Translations.xml": "Languages/English/Keyed/Translations.xml",
        "Languages/English/DefInjected/JobDef/WorkGiver.xml": "Languages/English/DefInjected/JobDef/WorkGiver.xml",
        "Languages/English/DefInjected/WorkGiverDef/WorkGiver.xml": "Languages/English/DefInjected/WorkGiverDef/WorkGiver.xml",
        "LICENSE": "LICENSE",
    }),
    "SimpleSidearms": ("Simple Sidearms (Ношение двух видов оружия)", "petetimessix.simplesidearms", {
        "v1.6/Assemblies/SimpleSidearms.dll": "Assemblies/SimpleSidearms.dll",
        "v1.6/Defs/JobDefs/Jobs_Misc.xml": "Defs/JobDefs/Jobs_Misc.xml",
        "v1.6/Defs/ThinkTreeDefs/SubTrees_Misc.xml": "Defs/ThinkTreeDefs/SubTrees_Misc.xml",
        "v1.6/Defs/TutorDefs/Concepts.xml": "Defs/TutorDefs/Concepts.xml",
        "v1.6/Defs/WeaponTagMapDefs/SidearmWeaponTagMaps.xml": "Defs/WeaponTagMapDefs/SidearmWeaponTagMaps.xml",
        "v1.6/Languages/English/Keyed/English.xml": "Languages/English/Keyed/English.xml",
        "v1.6/Patches/pawnPatches.xml": "Patches/pawnPatches.xml",
        "v1.6/Patches/thinkTreePatches.xml": "Patches/thinkTreePatches.xml",
        "v1.6/Patches/weaponTagPatches.xml": "Patches/weaponTagPatches.xml",
        "v1.6/Textures/blocked_weapon.png": "Textures/blocked_weapon.png",
        "v1.6/Textures/drawPocket.png": "Textures/drawPocket.png",
        "v1.6/Textures/drawPocketMemory.png": "Textures/drawPocketMemory.png",
        "v1.6/Textures/drawPocketMemoryPrimary.png": "Textures/drawPocketMemoryPrimary.png",
        "v1.6/Textures/drawPocketPrimary.png": "Textures/drawPocketPrimary.png",
        "v1.6/Textures/drawPocketTemp.png": "Textures/drawPocketTemp.png",
        "v1.6/Textures/lockClosed.png": "Textures/lockClosed.png",
        "v1.6/Textures/lockOpen.png": "Textures/lockOpen.png",
        "v1.6/Textures/locklockOff.png": "Textures/locklockOff.png",
        "v1.6/Textures/locklockOn.png": "Textures/locklockOn.png",
        "v1.6/Textures/missingIcon.png": "Textures/missingIcon.png",
        "v1.6/Textures/preferMelee.png": "Textures/preferMelee.png",
        "v1.6/Textures/preferRanged.png": "Textures/preferRanged.png",
        "v1.6/Textures/preferSkilled.png": "Textures/preferSkilled.png",
        "v1.6/Textures/settingsWarningIcon.png": "Textures/settingsWarningIcon.png",
        "v1.6/Textures/sidearm_default_ranged.png": "Textures/sidearm_default_ranged.png",
        "v1.6/Textures/sidearm_forced_always.png": "Textures/sidearm_forced_always.png",
        "v1.6/Textures/sidearm_forced_drafted.png": "Textures/sidearm_forced_drafted.png",
        "v1.6/Textures/sidearm_preferred_melee.png": "Textures/sidearm_preferred_melee.png",
        "v1.6/Textures/speedBiasSliderNegative.png": "Textures/speedBiasSliderNegative.png",
        "v1.6/Textures/speedBiasSliderPositive.png": "Textures/speedBiasSliderPositive.png",
        "v1.6/Textures/tuts/autopickup.png": "Textures/tuts/autopickup.png",
        "v1.6/Textures/tuts/havePunch.png": "Textures/tuts/havePunch.png",
        "v1.6/Textures/tuts/rememberDropped.png": "Textures/tuts/rememberDropped.png",
        "v1.6/Textures/tuts/rememberPrimary.png": "Textures/tuts/rememberPrimary.png",
        "v1.6/Textures/unarmedIcon.png": "Textures/unarmedIcon.png",
        "v1.6/Textures/weaponType_dangerous.png": "Textures/weaponType_dangerous.png",
        "v1.6/Textures/weaponType_emp.png": "Textures/weaponType_emp.png",
        "v1.6/Textures/weaponType_manual.png": "Textures/weaponType_manual.png",
        "v1.6/Textures/weaponType_offhandcompat.png": "Textures/weaponType_offhandcompat.png",
        "v1.6/Textures/weaponType_shieldcompat.png": "Textures/weaponType_shieldcompat.png",
        "v1.6/Textures/weapon_held_mainhand.png": "Textures/weapon_held_mainhand.png",
        "v1.6/Textures/weapon_held_offhand.png": "Textures/weapon_held_offhand.png",
        "v1.6/Textures/weapon_held_twohand.png": "Textures/weapon_held_twohand.png",
    }),
    "RunAndGun": ("RunAndGun (Стрельба на ходу)", "roolo.runandgun.kotobike", {
        "1.6/Assemblies/RunAndGun.dll": "Assemblies/RunAndGun.dll",
        "Languages/English/Keyed/RunAndGun_Keys.xml": "Languages/English/Keyed/RunAndGun_Keys.xml",
        "Patches/RunAndGun.xml": "Patches/RunAndGun.xml",
        "Defs/UpdateFeatureDefs/UpdateFeatures.xml": "Defs/UpdateFeatureDefs/UpdateFeatures.xml",
        "News/UpdateFeatures.xml": "News/UpdateFeatures.xml",
        **{f"Textures/{name}.png": f"Textures/{name}.png" for name in ("square", "UI/Buttons/enable_RG", "UI/Buttons/disable_RG")},
    }),
    "CommonSense": ("Common Sense (Умные действия колонистов)", "avilmask.commonsense", {
        "1.6/Assemblies/CommonSense.dll": "Assemblies/CommonSense.dll",
        "Languages/English/Keyed/strings.xml": "Languages/English/Keyed/strings.xml",
        **{f"1.6/Defs/{name}.xml": f"Defs/{name}.xml" for name in ("RoomRoles", "Recipes_Meals", "JoyGivers", "Jobs_Misc")},
        **{f"1.6/Patches/{name}.xml": f"Patches/{name}.xml" for name in ("PackMeal", "FunPolice", "FoodOptimality", "CleanOnOpportunity", "CleanGizmo")},
        **{f"Textures/UI/Icons/{name}.png": f"Textures/UI/Icons/{name}.png" for name in ("Unload_Thing", "Unload_Thing_Cancel")},
    }),
    "SnapOut": ("Snap Out! (Успокоение срывающихся колонистов)", "weilbyte.snapout", {
        "1.6/Assemblies/SnapOut.dll": "Assemblies/SnapOut.dll",
        "Languages/English/Keyed/CalmDown.xml": "Languages/English/Keyed/CalmDown.xml",
        "1.6/Patches/CalmDownPatches.xml": "Patches/CalmDownPatches.xml",
        **{f"1.6/Defs/{folder}/{name}.xml": f"Defs/{folder}/{name}.xml" for folder, name in (
            ("JobDefs", "Jobs_SnapOut"), ("ThoughtDefs", "Thoughts_SnapOut"),
            ("InteractionDefs", "Interactions_SnapOut"), ("WorkGiverDefs", "WorkGivers_SnapOut"))},
    }),
    "DevInYourLanguage": ("Dev In Your Language", "latta.devl10n", {
        "1.6/Assemblies/DevInYourLanguage.dll": "Assemblies/DevInYourLanguage.dll",
        **{f"1.6/Languages/English/Keyed/{name}.xml": f"Languages/English/Keyed/{name}.xml"
           for name in ("Actions", "Actions_Incidents", "Menu", "Mod", "Settings", "TranslationOutput", "Windows")},
    }),
    "SmartSpeed": ("Smart Speed (4 скорость)", "sarg.smartspeed", {
        "1.6/Assemblies/SmartSpeed.dll": "Assemblies/SmartSpeed.dll",
        "Languages/English/Keyed/ACSmartSpeedStrings.xml": "Languages/English/Keyed/ACSmartSpeedStrings.xml",
        "Textures/UI/TimeControls/TimeSpeedButton_Ultrafast.png": "Textures/UI/TimeControls/TimeSpeedButton_Ultrafast.png",
        "Textures/UI/SmartSpeedLogo.png": "Textures/UI/SmartSpeedLogo.png",
    }),
    "SmarterConstruction": ("Smarter Construction (Умное строительство)", "dhultgren.smarterconstruction", {
        "v1.6/Assemblies/SmarterConstruction.dll": "Assemblies/SmarterConstruction.dll",
    }),
    "InteractionBubbles": ("Interaction Bubbles (Речевые пузыри)", "jaxe.bubbles", {
        "Assemblies/Bubbles.dll": "Assemblies/Bubbles.dll",
        "Languages/English/Keyed/Bubbles.xml": "Languages/English/Keyed/Bubbles.xml",
        "Textures/Bubbles/Icon.png": "Textures/Bubbles/Icon.png",
        "Textures/Bubbles/Inner.png": "Textures/Bubbles/Inner.png",
        "Textures/Bubbles/Outer.png": "Textures/Bubbles/Outer.png",
    }),
    "RealisticRooms": ("Realistic Rooms Rewritten (Снижение требований к комнатам)", "lucifer.realisticrooms", {
        "1.4/Assemblies/RealisticRoomsRewritten.dll": "Assemblies/RealisticRoomsRewritten.dll",
        "Languages/English/Keyed/settings.xml": "Languages/English/Keyed/settings.xml",
        "Patches/Rooms/RoomStats.xml": "Patches/Rooms/RoomStats.xml",
        "Patches/ThingDefs_Misc/Filth_Various.xml": "Patches/ThingDefs_Misc/Filth_Various.xml",
    }),
    "AlmostThere": ("Almost There! (Караваны не спят)", "duz.almosttherefork", {
        "1.6/Assemblies/CaravanDontRest.dll": "Standard/Assemblies/CaravanDontRest.dll",
        "VF/1.6/Assemblies/CaravanDontRest.dll": "Vehicles/Assemblies/CaravanDontRest.dll",
        "Patches/CaravanDontRestPatch.xml": "Patches/CaravanDontRestPatch.xml",
        "VF/Patches/CaravanDontRestPatchVF.xml": "Vehicles/Patches/CaravanDontRestPatchVF.xml",
        "Languages/English/Keyed/GameplayCommands.xml": "Languages/English/Keyed/GameplayCommands.xml",
        "Languages/English/Keyed/AlmostThere_keys.xml": "Languages/English/Keyed/AlmostThere_keys.xml",
        "Textures/UI/ForceRest.png": "Textures/UI/ForceRest.png",
        "Textures/UI/DontRest.png": "Textures/UI/DontRest.png",
        "Textures/UI/AlmostThere.png": "Textures/UI/AlmostThere.png",
    }),
}


def clean_text(path):
    text = path.read_text(encoding="utf-8-sig")
    path.write_text("\n".join(line.rstrip() for line in text.splitlines()) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--component", action="append", choices=COMPONENTS,
                        help="Import only this component and preserve other manifest entries; repeat as needed.")
    selected = parser.parse_args().component or list(COMPONENTS)
    for name in selected:
        folder, _, files = COMPONENTS[name]
        for relative in [*files, "About/About.xml"]:
            original = PROJECT.parent / folder / relative
            if not original.is_file():
                raise FileNotFoundError(f"Restore original resources before importing: {original}")
    manifest = PROJECT / "Documentation/UtilityComponents.json"
    report = json.loads(manifest.read_text(encoding="utf-8")) if manifest.is_file() else {}
    for name in selected:
        folder, package_id, files = COMPONENTS[name]
        source = PROJECT.parent / folder
        items = []
        for relative, destination in files.items():
            original = source / relative
            target = PROJECT / "Components" / name / destination
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(original, target)
            source_hash = hashlib.sha256(original.read_bytes()).hexdigest()
            if target.suffix.lower() == ".xml" or target.name == "LICENSE":
                clean_text(target)
            data = target.read_bytes()
            if target.suffix.lower() == ".xml" or target.name == "LICENSE":
                data = data.replace(b"\r\n", b"\n")
            items.append({"source": original.relative_to(PROJECT.parent).as_posix(),
                          "projectFile": target.relative_to(PROJECT).as_posix(),
                          "sourceRawSha256": source_hash,
                          "sha256": hashlib.sha256(data).hexdigest()})
        about = PROJECT / "ThirdParty" / f"{name}.About.xml"
        shutil.copyfile(source / "About/About.xml", about)
        clean_text(about)
        report[name] = {"packageId": package_id, "files": items, "bundledHarmonyCopied": False,
                        "textNormalization": "XML and LICENSE: LF line endings and trailing line whitespace removed; content otherwise unchanged"}
    path = PROJECT / "LoadFolders.xml"
    tree = ET.parse(str(path), ET.XMLParser(remove_blank_text=True))
    for version in ("v1.6", "v1.7"):
        for name, (_, package_id, _) in COMPONENTS.items():
            if not tree.xpath(f"/{tree.getroot().tag}/{version}/li[text()='Components/{name}']"):
                ET.SubElement(tree.find(version), "li", IfModNotActive=package_id).text = f"Components/{name}"
        for branch, attributes in {
            "Standard": {"IfModNotActive": "duz.almosttherefork,smashphil.vehicleframework"},
            "Vehicles": {"IfModNotActive": "duz.almosttherefork", "IfModActive": "smashphil.vehicleframework"},
        }.items():
            folder = "Components/AlmostThere/" + branch
            if "AlmostThere" in report and not tree.xpath(f"/loadFolders/{version}/li[text()='{folder}']"):
                ET.SubElement(tree.find(version), "li", **attributes).text = folder
    ET.indent(tree, space="  ")
    tree.write(str(path), encoding="utf-8", xml_declaration=True, pretty_print=True)
    manifest.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({name: len(item["files"]) for name, item in report.items()}))


if __name__ == "__main__":
    main()
