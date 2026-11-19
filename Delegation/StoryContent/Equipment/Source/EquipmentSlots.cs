using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF EXTENSIONS & ENUMS
    // =========================================================================

    public enum EquipmentModKind
    {
        Weapon,
        Apparel
    }

    public sealed class BackpackExtension : DefModExtension
    {
        public float bonusCaravanKg = 25f;
    }

    // =========================================================================
    // UTILITIES: STORY FLAGS, WORKBENCHES, SIMPLE SIDEARMS REFLECTION
    // =========================================================================

    public static class EquipmentUtil
    {
        private static bool s_sidearmsChecked;
        private static Type s_sidearmsCompType;
        private static MethodInfo s_sidearmsGetMemoryMethod;
        private static PropertyInfo s_sidearmsRememberedProp;
        private static FieldInfo s_sidearmsRememberedField;

        private static bool s_storyChecked;
        private static Type s_sparkStoryType;
        private static PropertyInfo s_storyInstanceProp;
        private static MethodInfo s_storyGetFlagMethod;
        private static FieldInfo s_storyFlagsField;

        public static bool HasStoryFlag(string flagName)
        {
            if (flagName.NullOrEmpty()) return true;

            if (!s_storyChecked)
            {
                s_storyChecked = true;
                s_sparkStoryType = AccessTools.TypeByName("TurboMegaMod.SparkStory");
                if (s_sparkStoryType != null)
                {
                    s_storyInstanceProp = AccessTools.Property(s_sparkStoryType, "Instance");
                    s_storyGetFlagMethod = AccessTools.Method(s_sparkStoryType, "GetFlag", new Type[] { typeof(string) }); // CHECK API
                    s_storyFlagsField = AccessTools.Field(s_sparkStoryType, "flags"); // CHECK API
                }
            }

            if (s_sparkStoryType == null || s_storyInstanceProp == null) return true;

            try
            {
                object inst = s_storyInstanceProp.GetValue(null, null);
                if (inst == null) return false;

                if (s_storyGetFlagMethod != null)
                {
                    object val = s_storyGetFlagMethod.Invoke(inst, new object[] { flagName });
                    string str = val != null ? val.ToString() : null;
                    return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) || str == "1";
                }

                if (s_storyFlagsField != null)
                {
                    IDictionary dict = s_storyFlagsField.GetValue(inst) as IDictionary;
                    if (dict != null && dict.Contains(flagName))
                    {
                        object val = dict[flagName];
                        string str = val != null ? val.ToString() : null;
                        return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) || str == "1";
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static bool IsWeaponModEligible(ThingDef def)
        {
            if (def == null) return false;
            if (def.equipmentType != EquipmentType.Primary) return false;
            return def.IsRangedWeapon || def.IsMeleeWeapon;
        }

        public static int DefaultWeaponSlotCount(ThingDef def)
        {
            if (def == null) return 0;
            if (def.defName == "TMM_Legendary_FirstBlade" ||
                def.defName == "TMM_Legendary_ArchangelBlade" ||
                def.defName == "TMM_Legendary_TheColt")
            {
                return 0;
            }
            if (def.defName != null && def.defName.StartsWith("TMM_Legendary_", StringComparison.Ordinal))
            {
                return 1;
            }
            return 2;
        }

        public static bool IsApparelModEligible(ThingDef def)
        {
            if (def == null || !def.IsApparel || def.apparel == null || def.apparel.layers == null) return false;
            if (IsPowerArmorDef(def)) return false;

            ApparelLayerDef shell = ApparelLayerDefOf.Shell;
            if (shell == null || !def.apparel.layers.Contains(shell)) return false;

            if (def.apparel.bodyPartGroups != null && def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
            {
                return true;
            }
            return false;
        }

        public static bool IsPowerArmorDef(ThingDef def)
        {
            if (def == null || !def.IsApparel || def.apparel == null) return false;
            if (def.apparel.layers != null)
            {
                for (int i = 0; i < def.apparel.layers.Count; i++)
                {
                    if (def.apparel.layers[i] != null && def.apparel.layers[i].defName == "TMM_PowerArmor") return true;
                }
            }

            string n = def.defName;
            if (n.NullOrEmpty()) return false;
            if (n == "TMM_Legendary_ProtectorArmor") return true;
            if (n.StartsWith("TMM_", StringComparison.Ordinal) &&
                (n.IndexOf("PowerArmor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("T60", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("T45", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("X01", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
            return false;
        }

        public static Building FindNearestWorkbench(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Map.listerBuildings == null) return null;

            List<Building> buildings = pawn.Map.listerBuildings.allBuildingsColonist;
            Building bestPreferred = null;
            float bestPreferredDist = float.MaxValue;
            Building bestAny = null;
            float bestAnyDist = float.MaxValue;

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.Destroyed || !(b is Building_WorkTable)) continue;
                if (!pawn.CanReach(b, PathEndMode.InteractionCell, Danger.Deadly)) continue;

                float d = pawn.Position.DistanceToSquared(b.Position);
                string dn = b.def != null ? b.def.defName : string.Empty;
                bool preferred = dn == "TableMachining" || dn == "ElectricSmithy" || dn == "FueledSmithy" ||
                                 dn == "ElectricTailoringBench" || dn == "HandTailoringBench";
                if (preferred && d < bestPreferredDist)
                {
                    bestPreferredDist = d;
                    bestPreferred = b;
                }
                else if (d < bestAnyDist)
                {
                    bestAnyDist = d;
                    bestAny = b;
                }
            }

            return bestPreferred ?? bestAny;
        }

        /// <summary>
        /// Reads backup/sidearm weapons via Simple Sidearms reflection if embedded in the mod,
        /// falling back cleanly to weapon Things inside pawn.inventory.innerContainer without errors.
        /// </summary>
        public static List<ThingWithComps> GetSidearms(Pawn pawn)
        {
            List<ThingWithComps> result = new List<ThingWithComps>();
            if (pawn == null) return result;

            HashSet<ThingWithComps> seen = new HashSet<ThingWithComps>();
            ThingWithComps primary = pawn.equipment != null ? pawn.equipment.Primary : null;

            // 1. Always collect actual weapon items carried in pawn.inventory
            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                ThingOwner<Thing> inv = pawn.inventory.innerContainer;
                for (int i = 0; i < inv.Count; i++)
                {
                    ThingWithComps twc = inv[i] as ThingWithComps;
                    if (twc != null && twc != primary && twc.def != null && (twc.def.IsRangedWeapon || twc.def.IsMeleeWeapon))
                    {
                        if (seen.Add(twc)) result.Add(twc);
                    }
                }
            }

            // 2. Reflection hook into embedded Simple Sidearms (if present) to verify remembered sidearms
            if (!s_sidearmsChecked)
            {
                s_sidearmsChecked = true;
                s_sidearmsCompType = AccessTools.TypeByName("SimpleSidearms.rimworld.CompSidearmMemory") ??
                                     AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Memory.CompSidearmMemory"); // CHECK API
                if (s_sidearmsCompType != null)
                {
                    s_sidearmsGetMemoryMethod = AccessTools.Method(s_sidearmsCompType, "GetMemoryCompForPawn", new Type[] { typeof(Pawn), typeof(bool) }); // CHECK API
                    s_sidearmsRememberedProp = AccessTools.Property(s_sidearmsCompType, "RememberedWeapons"); // CHECK API
                    s_sidearmsRememberedField = AccessTools.Field(s_sidearmsCompType, "rememberedWeapons"); // CHECK API
                }
            }

            if (s_sidearmsCompType != null)
            {
                try
                {
                    object memComp = null;
                    if (s_sidearmsGetMemoryMethod != null)
                    {
                        memComp = s_sidearmsGetMemoryMethod.Invoke(null, new object[] { pawn, false });
                    }
                    else if (pawn.AllComps != null)
                    {
                        for (int i = 0; i < pawn.AllComps.Count; i++)
                        {
                            if (s_sidearmsCompType.IsInstanceOfType(pawn.AllComps[i]))
                            {
                                memComp = pawn.AllComps[i];
                                break;
                            }
                        }
                    }

                    if (memComp != null)
                    {
                        IEnumerable remembered = null;
                        if (s_sidearmsRememberedProp != null) remembered = s_sidearmsRememberedProp.GetValue(memComp, null) as IEnumerable;
                        else if (s_sidearmsRememberedField != null) remembered = s_sidearmsRememberedField.GetValue(memComp) as IEnumerable;

                        // Simple Sidearms remembers ThingDefStuffDefPair; weapons themselves live in inventory
                        // which we already added above, so no duplicate entries are created.
                        if (remembered != null)
                        {
                            // Touch enumerator safely to verify reflection bridge without throwing
                            IEnumerator en = remembered.GetEnumerator();
                            if (en != null) en.MoveNext();
                        }
                    }
                }
                catch
                {
                    // Silent fallback to inventory-based sidearms list
                }
            }

            return result;
        }

        public static List<Thing> FindAvailableModItems(Pawn pawn, EquipmentModKind kind)
        {
            List<Thing> results = new List<Thing>();
            if (pawn == null) return results;

            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                ThingOwner<Thing> inv = pawn.inventory.innerContainer;
                for (int i = 0; i < inv.Count; i++)
                {
                    Thing t = inv[i];
                    if (IsMatchingModItem(t, kind)) results.Add(t);
                }
            }

            if (pawn.Map != null && pawn.Map.listerThings != null)
            {
                List<Thing> all = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
                for (int i = 0; i < all.Count; i++)
                {
                    Thing t = all[i];
                    if (t == null || t.Destroyed || t.IsForbidden(Faction.OfPlayer)) continue;
                    if (!IsMatchingModItem(t, kind)) continue;
                    if (pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        results.Add(t);
                    }
                }
            }

            return results;
        }

        private static bool IsMatchingModItem(Thing t, EquipmentModKind kind)
        {
            if (t == null || t.Destroyed) return false;
            CompEquipmentMod mod = t.TryGetComp<CompEquipmentMod>();
            if (mod == null || mod.Props.modKind != kind) return false;
            if (mod.Props.maxCharges > 0 && mod.Charges <= 0) return false;
            return true;
        }

        public static Thing FindColtBulletThing(Pawn pawn, string ammoDefName)
        {
            if (pawn == null || ammoDefName.NullOrEmpty()) return null;
            ThingDef ammoDef = DefDatabase<ThingDef>.GetNamedSilentFail(ammoDefName);
            if (ammoDef == null) return null;

            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing t = pawn.inventory.innerContainer[i];
                    if (t != null && !t.Destroyed && t.def == ammoDef && t.stackCount > 0) return t;
                }
            }

            if (pawn.Map != null && pawn.Map.listerThings != null)
            {
                List<Thing> list = pawn.Map.listerThings.ThingsOfDef(ammoDef);
                Thing best = null;
                float bestDist = float.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t == null || t.Destroyed || t.stackCount <= 0 || t.IsForbidden(Faction.OfPlayer)) continue;
                    if (!pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly)) continue;
                    float d = pawn.Position.DistanceToSquared(t.Position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = t;
                    }
                }
                return best;
            }

            return null;
        }
    }

    // =========================================================================
    // MODULE ITEM COMP (STORES CHARGES ON THE MODULE ITEM ITSELF)
    // =========================================================================

    public sealed class CompProperties_EquipmentMod : CompProperties
    {
        public EquipmentModKind modKind = EquipmentModKind.Weapon;
        public string extraDamageDef;
        public float extraDamageAmount = 5f;
        public float extraArmorPenetration = 0.40f;
        public int maxCharges = 0; // 0 = permanent / unlimited
        public float armorSharpOffset = 0f;
        public float armorBluntOffset = 0f;
        public float armorHeatOffset = 0f;
        public string requiredStoryFlag;

        public CompProperties_EquipmentMod()
        {
            compClass = typeof(CompEquipmentMod);
        }
    }

    public sealed class CompEquipmentMod : ThingComp
    {
        private int charges = -1;

        public CompProperties_EquipmentMod Props
        {
            get { return (CompProperties_EquipmentMod)props; }
        }

        public int Charges
        {
            get
            {
                if (Props.maxCharges <= 0) return 0;
                if (charges < 0) charges = Props.maxCharges;
                return charges;
            }
            set
            {
                if (Props.maxCharges <= 0) charges = 0;
                else charges = Mathf.Clamp(value, 0, Props.maxCharges);
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = Props.maxCharges;
        }

        public override string TransformLabel(string label)
        {
            if (Props.maxCharges > 0)
            {
                return string.Format("{0} [{1}/{2}]", label, Charges, Props.maxCharges);
            }
            return label;
        }

        public override string CompInspectStringExtra()
        {
            if (Props.modKind == EquipmentModKind.Weapon)
            {
                string chargePart = Props.maxCharges > 0
                    ? string.Format(" ({0}/{1})", Charges, Props.maxCharges)
                    : " (permanent)";
                return string.Format("Weapon module: +{0:0.#} {1}{2}",
                    Props.extraDamageAmount, Props.extraDamageDef ?? "damage", chargePart);
            }
            return string.Format("Apparel module: +{0:0}% sharp armor", Props.armorSharpOffset * 100f);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", -1);
            if (Props.maxCharges > 0 && charges < 0) charges = Props.maxCharges;
        }
    }

    // =========================================================================
    // SERIALIZABLE INSTALLED MOD RECORD
    // =========================================================================

    public sealed class InstalledModEntry : IExposable
    {
        public string modDefName;
        public int charges;
        public int maxCharges;
        public int hitPoints = 100;

        public ThingDef ModDef
        {
            get { return !modDefName.NullOrEmpty() ? DefDatabase<ThingDef>.GetNamedSilentFail(modDefName) : null; }
        }

        public CompProperties_EquipmentMod ModProps
        {
            get
            {
                ThingDef d = ModDef;
                return d != null ? d.GetCompProperties<CompProperties_EquipmentMod>() : null;
            }
        }

        public bool IsDepleted
        {
            get { return maxCharges > 0 && charges <= 0; }
        }

        public bool CanTrigger
        {
            get { return maxCharges <= 0 || charges > 0; }
        }

        public void ConsumeCharge()
        {
            if (maxCharges > 0 && charges > 0)
            {
                charges--;
            }
        }

        public string DisplayLabel()
        {
            ThingDef d = ModDef;
            string name = d != null ? d.LabelCap.ToString() : (modDefName ?? "Mod");
            if (maxCharges > 0)
            {
                return string.Format("{0} [{1}/{2}]", name, charges, maxCharges);
            }
            return name;
        }

        public Thing CreateReturnedThing()
        {
            ThingDef d = ModDef;
            if (d == null) return null;
            Thing t = ThingMaker.MakeThing(d);
            if (t == null) return null;
            if (d.useHitPoints && hitPoints > 0)
            {
                t.HitPoints = Mathf.Clamp(hitPoints, 1, t.MaxHitPoints);
            }
            CompEquipmentMod comp = t.TryGetComp<CompEquipmentMod>();
            if (comp != null && maxCharges > 0)
            {
                comp.Charges = charges;
            }
            return t;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref modDefName, "modDefName", null);
            Scribe_Values.Look(ref charges, "charges", 0);
            Scribe_Values.Look(ref maxCharges, "maxCharges", 0);
            Scribe_Values.Look(ref hitPoints, "hitPoints", 100);
        }
    }

    // =========================================================================
    // WEAPON MOD SLOTS COMP (2 SLOTS ON STANDARD WEAPONS, 0-1 ON LEGENDARIES)
    // =========================================================================

    public sealed class CompProperties_WeaponMods : CompProperties
    {
        public int maxSlots = 2;

        public CompProperties_WeaponMods()
        {
            compClass = typeof(CompWeaponMods);
        }
    }

    public sealed class CompWeaponMods : ThingComp
    {
        private List<InstalledModEntry> installedMods = new List<InstalledModEntry>();

        public CompProperties_WeaponMods Props
        {
            get { return (CompProperties_WeaponMods)props; }
        }

        public List<InstalledModEntry> InstalledMods
        {
            get
            {
                if (installedMods == null) installedMods = new List<InstalledModEntry>();
                return installedMods;
            }
        }

        public int MaxSlots
        {
            get { return Props != null ? Props.maxSlots : 0; }
        }

        public bool HasFreeSlot
        {
            get { return InstalledMods.Count < MaxSlots; }
        }

        public bool CanInstall(Thing modThing, out string failReason)
        {
            failReason = null;
            if (MaxSlots <= 0)
            {
                failReason = "No module slots available";
                return false;
            }
            if (!HasFreeSlot)
            {
                failReason = "All module slots are occupied";
                return false;
            }
            if (modThing == null || modThing.Destroyed)
            {
                failReason = "Invalid module";
                return false;
            }
            CompEquipmentMod mod = modThing.TryGetComp<CompEquipmentMod>();
            if (mod == null || mod.Props.modKind != EquipmentModKind.Weapon)
            {
                failReason = "Not a weapon module";
                return false;
            }
            if (mod.Props.maxCharges > 0 && mod.Charges <= 0)
            {
                failReason = "Module tank is empty";
                return false;
            }
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                if (InstalledMods[i].modDefName == modThing.def.defName)
                {
                    failReason = "Duplicate module already installed";
                    return false;
                }
            }
            return true;
        }

        public bool InstallMod(Thing modThing)
        {
            string reason;
            if (!CanInstall(modThing, out reason)) return false;

            CompEquipmentMod modComp = modThing.TryGetComp<CompEquipmentMod>();
            InstalledModEntry entry = new InstalledModEntry();
            entry.modDefName = modThing.def.defName;
            entry.maxCharges = modComp.Props.maxCharges;
            entry.charges = modComp.Charges;
            entry.hitPoints = modThing.HitPoints;
            InstalledMods.Add(entry);

            if (modThing.stackCount > 1)
            {
                Thing split = modThing.SplitOff(1);
                if (split != null && !split.Destroyed) split.Destroy(DestroyMode.Vanish);
            }
            else if (!modThing.Destroyed)
            {
                modThing.Destroy(DestroyMode.Vanish);
            }

            return true;
        }

        public Thing RemoveMod(int index, Map map, IntVec3 dropPos)
        {
            if (index < 0 || index >= InstalledMods.Count) return null;
            InstalledModEntry entry = InstalledMods[index];
            InstalledMods.RemoveAt(index);

            Thing returned = entry.CreateReturnedThing();
            if (returned != null && map != null && dropPos.InBounds(map))
            {
                GenPlace.TryPlaceThing(returned, dropPos, map, ThingPlaceMode.Near);
            }
            return returned;
        }

        public override string CompInspectStringExtra()
        {
            if (MaxSlots <= 0) return null;
            if (InstalledMods.Count == 0)
            {
                return string.Format("TMM_WeaponModsEmpty".Translate(), MaxSlots);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("TMM_WeaponModsPrefix".Translate());
            sb.Append(" ");
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(InstalledMods[i].DisplayLabel());
            }
            return sb.ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref installedMods, "installedMods", LookMode.Deep);
            if (installedMods == null) installedMods = new List<InstalledModEntry>();
        }
    }

    // =========================================================================
    // APPAREL MOD SLOTS COMP (1 SLOT ON COATS / JACKETS / DUSTERS)
    // =========================================================================

    public sealed class CompProperties_ApparelMods : CompProperties
    {
        public int maxSlots = 1;

        public CompProperties_ApparelMods()
        {
            compClass = typeof(CompApparelMods);
        }
    }

    public sealed class CompApparelMods : ThingComp
    {
        private List<InstalledModEntry> installedMods = new List<InstalledModEntry>();

        public CompProperties_ApparelMods Props
        {
            get { return (CompProperties_ApparelMods)props; }
        }

        public List<InstalledModEntry> InstalledMods
        {
            get
            {
                if (installedMods == null) installedMods = new List<InstalledModEntry>();
                return installedMods;
            }
        }

        public int MaxSlots
        {
            get { return Props != null ? Props.maxSlots : 0; }
        }

        public bool HasFreeSlot
        {
            get { return InstalledMods.Count < MaxSlots; }
        }

        public bool CanInstall(Thing modThing, out string failReason)
        {
            failReason = null;
            if (MaxSlots <= 0 || !HasFreeSlot)
            {
                failReason = "No apparel lining slots free";
                return false;
            }
            if (modThing == null || modThing.Destroyed)
            {
                failReason = "Invalid module";
                return false;
            }
            CompEquipmentMod mod = modThing.TryGetComp<CompEquipmentMod>();
            if (mod == null || mod.Props.modKind != EquipmentModKind.Apparel)
            {
                failReason = "Not an apparel module";
                return false;
            }
            return true;
        }

        public bool InstallMod(Thing modThing)
        {
            string reason;
            if (!CanInstall(modThing, out reason)) return false;

            CompEquipmentMod modComp = modThing.TryGetComp<CompEquipmentMod>();
            InstalledModEntry entry = new InstalledModEntry();
            entry.modDefName = modThing.def.defName;
            entry.maxCharges = modComp.Props.maxCharges;
            entry.charges = modComp.Charges;
            entry.hitPoints = modThing.HitPoints;
            InstalledMods.Add(entry);

            if (modThing.stackCount > 1)
            {
                Thing split = modThing.SplitOff(1);
                if (split != null && !split.Destroyed) split.Destroy(DestroyMode.Vanish);
            }
            else if (!modThing.Destroyed)
            {
                modThing.Destroy(DestroyMode.Vanish);
            }

            return true;
        }

        public Thing RemoveMod(int index, Map map, IntVec3 dropPos)
        {
            if (index < 0 || index >= InstalledMods.Count) return null;
            InstalledModEntry entry = InstalledMods[index];
            InstalledMods.RemoveAt(index);

            Thing returned = entry.CreateReturnedThing();
            if (returned != null && map != null && dropPos.InBounds(map))
            {
                GenPlace.TryPlaceThing(returned, dropPos, map, ThingPlaceMode.Near);
            }
            return returned;
        }

        public float GetArmorOffset(StatDef stat)
        {
            if (stat == null || InstalledMods.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                CompProperties_EquipmentMod mp = InstalledMods[i].ModProps;
                if (mp == null) continue;
                if (stat == StatDefOf.ArmorRating_Sharp) sum += mp.armorSharpOffset;
                else if (stat == StatDefOf.ArmorRating_Blunt) sum += mp.armorBluntOffset;
                else if (stat == StatDefOf.ArmorRating_Heat) sum += mp.armorHeatOffset;
            }
            return sum;
        }

        public override string CompInspectStringExtra()
        {
            if (InstalledMods.Count == 0) return null;
            StringBuilder sb = new StringBuilder();
            sb.Append("TMM_ApparelModsPrefix".Translate());
            sb.Append(" ");
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(InstalledMods[i].DisplayLabel());
            }
            return sb.ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref installedMods, "installedMods", LookMode.Deep);
            if (installedMods == null) installedMods = new List<InstalledModEntry>();
        }
    }

    // =========================================================================
    // STAT PART FOR APPAREL MODS (REFLECTS +0.25 SHARP ARMOR EVERYWHERE)
    // =========================================================================

    public sealed class StatPart_ApparelMods : StatPart
    {
        private readonly StatDef targetStat;

        public StatPart_ApparelMods(StatDef stat)
        {
            targetStat = stat;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || req.Thing == null) return;
            CompApparelMods comp = req.Thing.TryGetComp<CompApparelMods>();
            if (comp == null) return;
            float bonus = comp.GetArmorOffset(targetStat);
            if (bonus > 0f)
            {
                val += bonus;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing || req.Thing == null) return null;
            CompApparelMods comp = req.Thing.TryGetComp<CompApparelMods>();
            if (comp == null) return null;
            float bonus = comp.GetArmorOffset(targetStat);
            if (bonus <= 0f) return null;
            return string.Format("TMM_ApparelModStatLine".Translate(), bonus.ToStringPercent());
        }
    }

    // =========================================================================
    // COLT AMMO COMP (13 ROUNDS, RELOADABLE WITH TMM_Ammo_ColtSilverBullet)
    // Attached to TMM_Legendary_TheColt via XML patch & DefsLoaded injector
    // =========================================================================

    public sealed class CompProperties_ColtAmmo : CompProperties
    {
        public int maxCharges = 13;
        public string ammoDefName = "TMM_Ammo_ColtSilverBullet";

        public CompProperties_ColtAmmo()
        {
            compClass = typeof(CompColtAmmo);
        }
    }

    public sealed class CompColtAmmo : ThingComp
    {
        private int charges = -1;

        public CompProperties_ColtAmmo Props
        {
            get { return (CompProperties_ColtAmmo)props; }
        }

        public int MaxCharges
        {
            get { return Props != null ? Props.maxCharges : 13; }
        }

        public int Charges
        {
            get
            {
                int synced;
                if (TryReadCompLimitedCharges(out synced))
                {
                    charges = synced;
                    return synced;
                }
                if (charges < 0) charges = MaxCharges;
                return charges;
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = MaxCharges;
        }

        public bool ConsumeRound()
        {
            if (Charges <= 0) return false;
            charges = Charges - 1;
            TryWriteCompLimitedCharges(charges);
            return true;
        }

        public int ReloadRounds(int amount)
        {
            if (amount <= 0) return 0;
            int cur = Charges;
            int missing = MaxCharges - cur;
            int add = Mathf.Min(missing, amount);
            if (add <= 0) return 0;
            charges = cur + add;
            TryWriteCompLimitedCharges(charges);
            return add;
        }

        public override string CompInspectStringExtra()
        {
            // Avoid duplicating inspect text if CompLimitedCharges is already mounted
            if (HasCompLimitedCharges()) return null;
            return string.Format("TMM_ColtAmmoInspect".Translate(), Charges, MaxCharges);
        }

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra() // CHECK API
        {
            Pawn holder = parent.ParentHolder is Pawn_EquipmentTracker
                ? ((Pawn_EquipmentTracker)parent.ParentHolder).pawn : null;
            if (holder == null || !holder.IsColonistPlayerControlled) yield break;

            if (Charges < MaxCharges)
            {
                Command_Action cmd = new Command_Action();
                cmd.defaultLabel = string.Format("TMM_ReloadColtGizmo".Translate(), Charges, MaxCharges);
                cmd.defaultDesc = "TMM_ReloadColtDesc".Translate();
                cmd.icon = TexCommand.RearmTrap; // CHECK API
                cmd.action = delegate
                {
                    Thing bullets = EquipmentUtil.FindColtBulletThing(holder, Props.ammoDefName);
                    if (bullets == null)
                    {
                        Messages.Message("TMM_NoColtBullets".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_ReloadColt");
                    if (jd != null)
                    {
                        Job job = JobMaker.MakeJob(jd, parent, bullets);
                        job.count = MaxCharges - Charges;
                        holder.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };
                yield return cmd;
            }
        }

        public bool HasCompLimitedCharges()
        {
            return FindCompLimitedCharges() != null;
        }

        private ThingComp FindCompLimitedCharges()
        {
            if (parent == null || parent.AllComps == null) return null;
            for (int i = 0; i < parent.AllComps.Count; i++)
            {
                ThingComp c = parent.AllComps[i];
                if (c != null && c.GetType().Name == "CompLimitedCharges") return c;
            }
            return null;
        }

        private bool TryReadCompLimitedCharges(out int value)
        {
            value = 0;
            ThingComp lc = FindCompLimitedCharges();
            if (lc == null) return false;
            PropertyInfo prop = AccessTools.Property(lc.GetType(), "Charges");
            if (prop != null)
            {
                object v = prop.GetValue(lc, null);
                if (v is int)
                {
                    value = (int)v;
                    return true;
                }
            }
            return false;
        }

        private void TryWriteCompLimitedCharges(int newCharges)
        {
            ThingComp lc = FindCompLimitedCharges();
            if (lc == null) return;
            FieldInfo field = AccessTools.Field(lc.GetType(), "charges");
            if (field != null)
            {
                field.SetValue(lc, newCharges);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", -1);
            if (charges < 0) charges = MaxCharges;
        }
    }

    // =========================================================================
    // JOB DRIVERS (INSTALL MOD, REMOVE MOD, RELOAD COLT)
    // =========================================================================

    public sealed class JobDriver_InstallEquipmentMod : JobDriver
    {
        public const int DefaultWorkTicks = 2500; // 1 in-game hour

        private Thing TargetEquipment { get { return job.targetA.Thing; } }
        private Thing ModThing { get { return job.targetB.Thing; } }
        private Building Workbench { get { return job.targetC.Thing as Building; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (ModThing != null && ModThing.Spawned)
            {
                if (!pawn.Reserve(job.targetB, job, 1, 1, null, errorOnFailed)) return false;
            }
            if (Workbench != null && Workbench.Spawned)
            {
                if (!pawn.Reserve(job.targetC, job, 1, -1, null, errorOnFailed)) return false;
            }
            if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return TargetEquipment == null || TargetEquipment.Destroyed || ModThing == null || ModThing.Destroyed;
            });

            // 1. Fetch module if lying on map
            if (ModThing != null && ModThing.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            }

            // 2. Walk to workbench (or to equipment if on ground and no workbench)
            if (Workbench != null && Workbench.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            }
            else if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }

            // 3. Work at bench for 1 in-game hour
            Toil work = Toils_General.Wait(DefaultWorkTicks);
            work.WithProgressBarToilDelay(Workbench != null ? TargetIndex.C : TargetIndex.A);
            work.WithEffect(EffecterDefOf.ConstructMetal, Workbench != null ? TargetIndex.C : TargetIndex.A); // CHECK API
            yield return work;

            // 4. Perform installation
            Toil install = new Toil();
            install.initAction = delegate
            {
                Thing eq = TargetEquipment;
                Thing mod = ModThing;
                if (eq == null || mod == null || eq.Destroyed || mod.Destroyed) return;

                CompWeaponMods wm = eq.TryGetComp<CompWeaponMods>();
                if (wm != null)
                {
                    wm.InstallMod(mod);
                    return;
                }
                CompApparelMods am = eq.TryGetComp<CompApparelMods>();
                if (am != null)
                {
                    am.InstallMod(mod);
                }
            };
            install.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return install;
        }
    }

    public sealed class JobDriver_RemoveEquipmentMod : JobDriver
    {
        public const int DefaultWorkTicks = 2500; // 1 in-game hour

        private Thing TargetEquipment { get { return job.targetA.Thing; } }
        private Building Workbench { get { return job.targetC.Thing as Building; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Workbench != null && Workbench.Spawned)
            {
                if (!pawn.Reserve(job.targetC, job, 1, -1, null, errorOnFailed)) return false;
            }
            if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return TargetEquipment == null || TargetEquipment.Destroyed;
            });

            if (Workbench != null && Workbench.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            }
            else if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }

            Toil work = Toils_General.Wait(DefaultWorkTicks);
            work.WithProgressBarToilDelay(Workbench != null ? TargetIndex.C : TargetIndex.A);
            yield return work;

            Toil remove = new Toil();
            remove.initAction = delegate
            {
                Thing eq = TargetEquipment;
                if (eq == null || eq.Destroyed) return;

                int idx = Mathf.Max(0, job.count);
                CompWeaponMods wm = eq.TryGetComp<CompWeaponMods>();
                if (wm != null)
                {
                    wm.RemoveMod(idx, pawn.Map, pawn.Position);
                    return;
                }
                CompApparelMods am = eq.TryGetComp<CompApparelMods>();
                if (am != null)
                {
                    am.RemoveMod(idx, pawn.Map, pawn.Position);
                }
            };
            remove.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return remove;
        }
    }

    public sealed class JobDriver_ReloadColt : JobDriver
    {
        private Thing Colt { get { return job.targetA.Thing; } }
        private Thing Ammo { get { return job.targetB.Thing; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Ammo != null && Ammo.Spawned)
            {
                if (!pawn.Reserve(job.targetB, job, 1, Mathf.Max(1, job.count), null, errorOnFailed)) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return Colt == null || Colt.Destroyed || Ammo == null || Ammo.Destroyed || Ammo.stackCount <= 0;
            });

            if (Ammo != null && Ammo.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            }

            Toil wait = Toils_General.Wait(120);
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

            Toil reload = new Toil();
            reload.initAction = delegate
            {
                CompColtAmmo ammoComp = Colt != null ? Colt.TryGetComp<CompColtAmmo>() : null;
                if (ammoComp == null || Ammo == null || Ammo.Destroyed) return;

                int need = ammoComp.MaxCharges - ammoComp.Charges;
                int take = Mathf.Min(need, Ammo.stackCount);
                if (take <= 0) return;

                int loaded = ammoComp.ReloadRounds(take);
                if (loaded > 0)
                {
                    if (Ammo.stackCount > loaded)
                    {
                        Thing split = Ammo.SplitOff(loaded);
                        if (split != null && !split.Destroyed) split.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Ammo.Destroy(DestroyMode.Vanish);
                    }
                }
            };
            reload.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return reload;
        }
    }

    // =========================================================================
    // WARCRAFT-STYLE EQUIPMENT ITAB (ADDITIVE — DOES NOT REPLACE VANILLA GEAR)
    // =========================================================================

    public sealed class ITab_TMM_Equipment : ITab
    {
        private static readonly Color EmptySlotBg = new Color(0.18f, 0.19f, 0.21f, 0.90f);
        private static readonly Color FilledSlotBg = new Color(0.24f, 0.26f, 0.30f, 0.95f);
        private static readonly Color SlotBorderColor = new Color(0.45f, 0.48f, 0.52f, 1.0f);
        private static readonly Color MiniModBg = new Color(0.15f, 0.16f, 0.18f, 0.95f);

        public ITab_TMM_Equipment()
        {
            size = new Vector2(540f, 560f);
            labelKey = "TMM_TabEquipment";
            tutorTag = "TMM_Equipment";
        }

        public override bool IsVisible
        {
            get
            {
                Pawn p = SelPawn;
                return p != null && p.RaceProps != null && p.RaceProps.Humanlike;
            }
        }

        protected override void FillTab()
        {
            Pawn p = SelPawn;
            if (p == null) return;
            DrawEquipmentTab(new Rect(0f, 0f, size.x, size.y).ContractedBy(10f), p);
        }

        /// <summary>
        /// Direct entry point used by InGameEquipmentProbe during a real EventType.Repaint pass.
        /// </summary>
        internal void DebugDrawInOnGUI(Rect rect, Pawn pawn)
        {
            if (pawn == null) return;
            GUI.BeginGroup(rect);
            try
            {
                DrawEquipmentTab(new Rect(0f, 0f, rect.width, rect.height).ContractedBy(10f), pawn);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawEquipmentTab(Rect rect, Pawn pawn)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 30f);
            Widgets.Label(titleRect, string.Format("{0} — {1}", pawn.LabelShortCap, "TMM_TabEquipment".Translate()));
            Text.Font = GameFont.Small;

            float topY = rect.y + 34f;
            float slotSize = 62f;
            float rowStep = 70f;

            // Classify worn apparel into paperdoll slots
            Apparel head = FindSlotApparel(pawn, "head");
            Apparel eyes = FindSlotApparel(pawn, "eyes");
            Apparel skinTorso = FindSlotApparel(pawn, "skin");
            Apparel midTorso = FindSlotApparel(pawn, "middle");
            Apparel shellTorso = FindSlotApparel(pawn, "shell");
            Apparel legs = FindSlotApparel(pawn, "legs");
            Apparel belt = FindSlotApparel(pawn, "belt");
            Apparel backpack = FindSlotApparel(pawn, "backpack");
            Apparel powerArmor = FindSlotApparel(pawn, "powerarmor");

            // LEFT COLUMN: Head, Eyes, Torso (Skin, Middle, Shell + Mod)
            float leftX = rect.x + 8f;
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 0f, slotSize, slotSize), head, "TMM_Slot_Head".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 1f, slotSize, slotSize), eyes, "TMM_Slot_Eyes".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 2f, slotSize, slotSize), skinTorso, "TMM_Slot_Skin".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 3f, slotSize, slotSize), midTorso, "TMM_Slot_Middle".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 4f, slotSize, slotSize), shellTorso, "TMM_Slot_Shell".Translate(), pawn, true);

            // Apparel Mod mini-slot next to Shell
            DrawApparelModSlot(new Rect(leftX + slotSize + 8f, topY + rowStep * 4f + 15f, 32f, 32f), shellTorso, pawn);

            // CENTER PAPERDOLL PORTRAIT & STATS
            float centerW = 210f;
            float centerX = rect.x + (rect.width - centerW) * 0.5f;
            Rect portraitBox = new Rect(centerX + 35f, topY + 12f, 140f, 175f);
            Widgets.DrawBoxSolid(portraitBox, new Color(0.12f, 0.13f, 0.15f, 0.9f));
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(portraitBox, 1);
            GUI.color = Color.white;

            RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(portraitBox.width, portraitBox.height), Rot4.South, default(Vector3), 1.15f); // CHECK API
            if (portrait != null)
            {
                GUI.DrawTexture(portraitBox.ContractedBy(4f), portrait, ScaleMode.ScaleToFit);
            }

            // Stats summary box under portrait
            Rect statsBox = new Rect(centerX, topY + 200f, centerW, 130f);
            Widgets.DrawBoxSolid(statsBox, new Color(0.14f, 0.15f, 0.17f, 0.85f));
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(statsBox, 1);
            GUI.color = Color.white;

            Rect st = statsBox.ContractedBy(8f);
            Text.Font = GameFont.Tiny;
            float carryStat = pawn.GetStatValue(StatDefOf.CarryingCapacity);
            float caravanCap = MassUtility.Capacity(pawn); // CHECK API
            float moveSpd = pawn.GetStatValue(StatDefOf.MoveSpeed);
            float bestSharp = ComputeBestArmor(pawn, StatDefOf.ArmorRating_Sharp);
            float bestBlunt = ComputeBestArmor(pawn, StatDefOf.ArmorRating_Blunt);

            Widgets.Label(new Rect(st.x, st.y, st.width, 20f), string.Format("{0}: {1:0} / {2:0.#} kg", "TMM_Stat_Carry".Translate(), carryStat, caravanCap));
            Widgets.Label(new Rect(st.x, st.y + 20f, st.width, 20f), string.Format("{0}: {1:0.00} c/s", "TMM_Stat_Move".Translate(), moveSpd));
            Widgets.Label(new Rect(st.x, st.y + 42f, st.width, 20f), string.Format("{0}: {1}", "TMM_Stat_Sharp".Translate(), bestSharp.ToStringPercent()));
            Widgets.Label(new Rect(st.x, st.y + 62f, st.width, 20f), string.Format("{0}: {1}", "TMM_Stat_Blunt".Translate(), bestBlunt.ToStringPercent()));
            Text.Font = GameFont.Small;

            // RIGHT COLUMN: Legs, Belt, Backpack, Power Armor
            float rightX = rect.xMax - slotSize - 8f;
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 0f, slotSize, slotSize), legs, "TMM_Slot_Legs".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 1f, slotSize, slotSize), belt, "TMM_Slot_Belt".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 2f, slotSize, slotSize), backpack, "TMM_Slot_Backpack".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 3f, slotSize, slotSize), powerArmor, "TMM_Slot_PowerArmor".Translate(), pawn, true);

            // BOTTOM WEAPONS & MOD SLOTS PANEL
            float weaponsY = topY + rowStep * 5f + 6f;
            Rect weaponsBox = new Rect(rect.x, weaponsY, rect.width, rect.yMax - weaponsY);
            Widgets.DrawBoxSolid(weaponsBox, new Color(0.13f, 0.14f, 0.16f, 0.90f));
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(weaponsBox, 1);
            GUI.color = Color.white;

            Rect wp = weaponsBox.ContractedBy(8f);
            ThingWithComps primary = pawn.equipment != null ? pawn.equipment.Primary : null;

            // Primary weapon + up to 2 mod slots
            DrawEquipmentSlot(new Rect(wp.x, wp.y, slotSize, slotSize), primary, "TMM_Slot_Primary".Translate(), pawn, false);
            DrawWeaponModSlotsRow(new Rect(wp.x, wp.y + slotSize + 6f, 110f, 30f), primary, pawn);

            // Sidearms (Simple Sidearms reflection + inventory weapons)
            float sideX = wp.x + 130f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(sideX, wp.y, wp.width - 130f, 18f), "TMM_Slot_Sidearms".Translate());
            Text.Font = GameFont.Small;

            List<ThingWithComps> sidearms = EquipmentUtil.GetSidearms(pawn);
            int maxShow = 4;
            float sideSlot = 48f;
            for (int i = 0; i < maxShow; i++)
            {
                ThingWithComps sw = i < sidearms.Count ? sidearms[i] : null;
                Rect sRect = new Rect(sideX + i * (sideSlot + 34f), wp.y + 20f, sideSlot, sideSlot);
                DrawEquipmentSlot(sRect, sw, "TMM_Slot_Backup".Translate(), pawn, false);
                if (sw != null)
                {
                    DrawWeaponModSlotsRow(new Rect(sRect.x, sRect.yMax + 4f, sideSlot + 24f, 24f), sw, pawn);
                }
            }
        }

        private void DrawEquipmentSlot(Rect rect, Thing item, string emptyLabel, Pawn pawn, bool isApparel)
        {
            Widgets.DrawBoxSolid(rect, item != null ? FilledSlotBg : EmptySlotBg);
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;

            if (item == null)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.68f, 0.70f, 0.72f, 0.9f);
                Widgets.Label(rect.ContractedBy(3f), emptyLabel);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                return;
            }

            Rect iconRect = rect.ContractedBy(6f);
            Widgets.ThingIcon(iconRect, item);

            // Quality badge top-left
            QualityCategory qc;
            if (item.TryGetQuality(out qc))
            {
                Text.Font = GameFont.Tiny;
                Rect qRect = new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, 16f);
                string qShort = qc.GetLabelShort();
                if (qShort.Length > 3) qShort = qShort.Substring(0, 3);
                Widgets.Label(qRect, qShort);
                Text.Font = GameFont.Small;
            }

            // Colt charge badge top-right
            CompColtAmmo coltAmmo = item.TryGetComp<CompColtAmmo>();
            if (coltAmmo != null)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, 16f), coltAmmo.Charges.ToString());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            // Durability bar along bottom
            if (item.def.useHitPoints && item.MaxHitPoints > 0)
            {
                float pct = Mathf.Clamp01((float)item.HitPoints / (float)item.MaxHitPoints);
                Rect barBg = new Rect(rect.x + 3f, rect.yMax - 6f, rect.width - 6f, 3f);
                Widgets.DrawBoxSolid(barBg, new Color(0.08f, 0.08f, 0.08f, 0.9f));
                Color barCol = pct > 0.5f ? new Color(0.3f, 0.85f, 0.35f) : (pct > 0.25f ? Color.yellow : Color.red);
                Widgets.DrawBoxSolid(new Rect(barBg.x, barBg.y, barBg.width * pct, barBg.height), barCol);
            }

            // Tooltip with stats & mods
            TooltipHandler.TipRegion(rect, BuildItemTooltip(item));

            // Right-click menu: Info / Unequip / Install Mod / Reload Colt
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(rect))
            {
                OpenSlotContextMenu(item, pawn, isApparel);
                Event.current.Use();
            }
        }

        private void DrawWeaponModSlotsRow(Rect area, ThingWithComps weapon, Pawn pawn)
        {
            if (weapon == null) return;
            CompWeaponMods comp = weapon.GetComp<CompWeaponMods>();
            if (comp == null || comp.MaxSlots <= 0) return;

            float miniSize = Mathf.Min(28f, area.height);
            for (int i = 0; i < comp.MaxSlots; i++)
            {
                Rect mRect = new Rect(area.x + i * (miniSize + 4f), area.y, miniSize, miniSize);
                InstalledModEntry entry = i < comp.InstalledMods.Count ? comp.InstalledMods[i] : null;
                DrawModMiniSlot(mRect, weapon, entry, i, EquipmentModKind.Weapon, pawn);
            }
        }

        private void DrawApparelModSlot(Rect mRect, Apparel apparel, Pawn pawn)
        {
            if (apparel == null) return;
            CompApparelMods comp = apparel.GetComp<CompApparelMods>();
            if (comp == null || comp.MaxSlots <= 0) return;

            InstalledModEntry entry = comp.InstalledMods.Count > 0 ? comp.InstalledMods[0] : null;
            DrawModMiniSlot(mRect, apparel, entry, 0, EquipmentModKind.Apparel, pawn);
        }

        private void DrawModMiniSlot(Rect rect, Thing hostItem, InstalledModEntry entry, int slotIndex, EquipmentModKind kind, Pawn pawn)
        {
            Widgets.DrawBoxSolid(rect, MiniModBg);
            GUI.color = entry != null ? new Color(0.35f, 0.75f, 0.95f) : SlotBorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;

            if (entry != null && entry.ModDef != null)
            {
                Widgets.DefIcon(rect.ContractedBy(3f), entry.ModDef);
                if (entry.maxCharges > 0)
                {
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.LowerRight;
                    Widgets.Label(rect.ContractedBy(1f), entry.charges.ToString());
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }
                TooltipHandler.TipRegion(rect, entry.DisplayLabel());
            }
            else
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.6f, 0.62f, 0.65f);
                Widgets.Label(rect, "+");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                TooltipHandler.TipRegion(rect, "TMM_EmptyModSlotTip".Translate());
            }

            if (Event.current.type == EventType.MouseDown && (Event.current.button == 0 || Event.current.button == 1) && Mouse.IsOver(rect))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (entry != null)
                {
                    if (entry.ModDef != null)
                    {
                        opts.Add(new FloatMenuOption("TMM_Menu_Info".Translate(), delegate
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(entry.ModDef));
                        }));
                    }
                    if (pawn != null && pawn.IsColonistPlayerControlled)
                    {
                        int capturedIndex = slotIndex;
                        opts.Add(new FloatMenuOption("TMM_Menu_RemoveMod".Translate(), delegate
                        {
                            QueueRemoveModJob(pawn, hostItem, capturedIndex);
                        }));
                    }
                }
                else if (pawn != null && pawn.IsColonistPlayerControlled)
                {
                    AddInstallModOptions(opts, pawn, hostItem, kind);
                }

                if (opts.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(opts));
                }
                Event.current.Use();
            }
        }

        private void OpenSlotContextMenu(Thing item, Pawn pawn, bool isApparel)
        {
            List<FloatMenuOption> opts = new List<FloatMenuOption>();

            opts.Add(new FloatMenuOption("TMM_Menu_Info".Translate(), delegate
            {
                Find.WindowStack.Add(new Dialog_InfoCard(item));
            }));

            if (pawn != null && pawn.IsColonistPlayerControlled)
            {
                opts.Add(new FloatMenuOption("TMM_Menu_Unequip".Translate(), delegate
                {
                    if (isApparel && item is Apparel && pawn.apparel != null)
                    {
                        Apparel dropped;
                        pawn.apparel.TryDrop((Apparel)item, out dropped, pawn.Position, true);
                    }
                    else if (!isApparel && item is ThingWithComps)
                    {
                        ThingWithComps twc = (ThingWithComps)item;
                        if (pawn.equipment != null && pawn.equipment.Primary == twc)
                        {
                            ThingWithComps dropped;
                            pawn.equipment.TryDropEquipment(twc, out dropped, pawn.Position, true);
                        }
                        else if (pawn.inventory != null && pawn.inventory.innerContainer.Contains(twc))
                        {
                            Thing dropped;
                            pawn.inventory.innerContainer.TryDrop(twc, pawn.Position, pawn.Map, ThingPlaceMode.Near, out dropped);
                        }
                    }
                }));

                CompWeaponMods wm = item.TryGetComp<CompWeaponMods>();
                if (wm != null && wm.HasFreeSlot)
                {
                    AddInstallModOptions(opts, pawn, item, EquipmentModKind.Weapon);
                }

                CompApparelMods am = item.TryGetComp<CompApparelMods>();
                if (am != null && am.HasFreeSlot)
                {
                    AddInstallModOptions(opts, pawn, item, EquipmentModKind.Apparel);
                }

                CompColtAmmo coltAmmo = item.TryGetComp<CompColtAmmo>();
                if (coltAmmo != null && coltAmmo.Charges < coltAmmo.MaxCharges)
                {
                    opts.Add(new FloatMenuOption(string.Format("TMM_ReloadColtGizmo".Translate(), coltAmmo.Charges, coltAmmo.MaxCharges), delegate
                    {
                        Thing bullets = EquipmentUtil.FindColtBulletThing(pawn, coltAmmo.Props.ammoDefName);
                        if (bullets == null)
                        {
                            Messages.Message("TMM_NoColtBullets".Translate(), MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_ReloadColt");
                        if (jd != null)
                        {
                            Job j = JobMaker.MakeJob(jd, item, bullets);
                            j.count = coltAmmo.MaxCharges - coltAmmo.Charges;
                            pawn.jobs.TryTakeOrderedJob(j, JobTag.Misc);
                        }
                    }));
                }
            }

            if (opts.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static void AddInstallModOptions(List<FloatMenuOption> opts, Pawn pawn, Thing hostItem, EquipmentModKind kind)
        {
            List<Thing> mods = EquipmentUtil.FindAvailableModItems(pawn, kind);
            if (mods.Count == 0)
            {
                opts.Add(new FloatMenuOption("TMM_NoModsAvailable".Translate(), null));
                return;
            }

            HashSet<string> shownDefs = new HashSet<string>();
            for (int i = 0; i < mods.Count; i++)
            {
                Thing modThing = mods[i];
                if (!shownDefs.Add(modThing.def.defName)) continue;

                string fail;
                bool can = false;
                CompWeaponMods wm = hostItem.TryGetComp<CompWeaponMods>();
                CompApparelMods am = hostItem.TryGetComp<CompApparelMods>();
                if (wm != null) can = wm.CanInstall(modThing, out fail);
                else if (am != null) can = am.CanInstall(modThing, out fail);
                else fail = "Incompatible";

                if (!can) continue;

                Thing capturedMod = modThing;
                opts.Add(new FloatMenuOption(string.Format("TMM_Menu_InstallMod".Translate(), capturedMod.LabelShortCap), delegate
                {
                    QueueInstallModJob(pawn, hostItem, capturedMod);
                }));
            }
        }

        public static void QueueInstallModJob(Pawn pawn, Thing hostItem, Thing modThing)
        {
            JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_InstallEquipmentMod");
            if (jd == null || pawn == null || hostItem == null || modThing == null) return;
            Building bench = EquipmentUtil.FindNearestWorkbench(pawn);
            Job job = bench != null
                ? JobMaker.MakeJob(jd, hostItem, modThing, bench)
                : JobMaker.MakeJob(jd, hostItem, modThing);
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public static void QueueRemoveModJob(Pawn pawn, Thing hostItem, int slotIndex)
        {
            JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_RemoveEquipmentMod");
            if (jd == null || pawn == null || hostItem == null) return;
            Building bench = EquipmentUtil.FindNearestWorkbench(pawn);
            Job job = bench != null
                ? JobMaker.MakeJob(jd, hostItem, LocalTargetInfo.Invalid, bench)
                : JobMaker.MakeJob(jd, hostItem);
            job.count = slotIndex;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static Apparel FindSlotApparel(Pawn pawn, string slotKey)
        {
            if (pawn == null || pawn.apparel == null) return null;
            List<Apparel> worn = pawn.apparel.WornApparel;

            for (int i = 0; i < worn.Count; i++)
            {
                Apparel ap = worn[i];
                if (ap == null || ap.def == null || ap.def.apparel == null || ap.def.apparel.layers == null) continue;
                List<ApparelLayerDef> layers = ap.def.apparel.layers;
                List<BodyPartGroupDef> parts = ap.def.apparel.bodyPartGroups;

                if (slotKey == "powerarmor")
                {
                    if (HasLayer(layers, "TMM_PowerArmor")) return ap;
                }
                else if (slotKey == "backpack")
                {
                    if (HasLayer(layers, "TMM_Backpack")) return ap;
                }
                else if (slotKey == "belt")
                {
                    if (layers.Contains(ApparelLayerDefOf.Belt)) return ap;
                }
                else if (slotKey == "eyes")
                {
                    if (layers.Contains(ApparelLayerDefOf.EyeCover)) return ap;
                }
                else if (slotKey == "head")
                {
                    if (layers.Contains(ApparelLayerDefOf.Overhead)) return ap;
                }
                else if (slotKey == "shell")
                {
                    if (layers.Contains(ApparelLayerDefOf.Shell) && !HasLayer(layers, "TMM_PowerArmor")) return ap;
                }
                else if (slotKey == "middle")
                {
                    if (layers.Contains(ApparelLayerDefOf.Middle) && parts != null && parts.Contains(BodyPartGroupDefOf.Torso)) return ap;
                }
                else if (slotKey == "skin")
                {
                    if (layers.Contains(ApparelLayerDefOf.OnSkin) && parts != null && parts.Contains(BodyPartGroupDefOf.Torso)) return ap;
                }
                else if (slotKey == "legs")
                {
                    bool hasLegs = parts != null && parts.Contains(BodyPartGroupDefOf.Legs);
                    bool hasTorso = parts != null && parts.Contains(BodyPartGroupDefOf.Torso);
                    if (hasLegs && !hasTorso && !HasLayer(layers, "TMM_PowerArmor")) return ap;
                }
            }
            return null;
        }

        private static bool HasLayer(List<ApparelLayerDef> layers, string defName)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].defName == defName) return true;
            }
            return false;
        }

        private static float ComputeBestArmor(Pawn pawn, StatDef stat)
        {
            float best = 0f;
            if (pawn == null || pawn.apparel == null) return best;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] == null) continue;
                float val = worn[i].GetStatValue(stat);
                if (val > best) best = val;
            }
            return best;
        }

        private static string BuildItemTooltip(Thing item)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(item.LabelCap);
            if (item.def.useHitPoints)
            {
                sb.AppendLine(string.Format("HP: {0} / {1}", item.HitPoints, item.MaxHitPoints));
            }
            if (item is Apparel)
            {
                sb.AppendLine(string.Format("Sharp: {0}  Blunt: {1}  Heat: {2}",
                    item.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent(),
                    item.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent(),
                    item.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent()));
            }
            CompWeaponMods wm = item.TryGetComp<CompWeaponMods>();
            if (wm != null && wm.InstalledMods.Count > 0)
            {
                sb.AppendLine("--- Modules ---");
                for (int i = 0; i < wm.InstalledMods.Count; i++)
                {
                    sb.AppendLine("• " + wm.InstalledMods[i].DisplayLabel());
                }
            }
            CompApparelMods am = item.TryGetComp<CompApparelMods>();
            if (am != null && am.InstalledMods.Count > 0)
            {
                sb.AppendLine("--- Lining ---");
                for (int i = 0; i < am.InstalledMods.Count; i++)
                {
                    sb.AppendLine("• " + am.InstalledMods[i].DisplayLabel());
                }
            }
            return sb.ToString().TrimEnd();
        }
    }

    // =========================================================================
    // DEF INJECTION & HARMONY PATCHES
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class EquipmentSlotsBootstrap
    {
        private static bool s_applyingModDamage;

        static EquipmentSlotsBootstrap()
        {
            InjectDefs();
            ApplyHarmonyPatches();
        }

        private static void InjectDefs()
        {
            ApparelLayerDef powerArmorLayer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("TMM_PowerArmor");

            // 1. Register StatParts for apparel mods on Sharp, Blunt, Heat armor
            RegisterArmorStatPart(StatDefOf.ArmorRating_Sharp);
            RegisterArmorStatPart(StatDefOf.ArmorRating_Blunt);
            RegisterArmorStatPart(StatDefOf.ArmorRating_Heat);

            // 2. Inject CompWeaponMods, CompApparelMods, CompColtAmmo, PowerArmor layer, and ITab_TMM_Equipment
            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (def == null) continue;

                // Power armor layer migration
                if (powerArmorLayer != null && EquipmentUtil.IsPowerArmorDef(def))
                {
                    if (def.apparel.layers == null) def.apparel.layers = new List<ApparelLayerDef>();
                    if (!def.apparel.layers.Contains(powerArmorLayer))
                    {
                        def.apparel.layers.Remove(ApparelLayerDefOf.Shell);
                        def.apparel.layers.Add(powerArmorLayer);
                    }
                }

                // Universal weapon mod slots
                if (EquipmentUtil.IsWeaponModEligible(def))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (def.GetCompProperties<CompProperties_WeaponMods>() == null)
                    {
                        CompProperties_WeaponMods cp = new CompProperties_WeaponMods();
                        cp.maxSlots = EquipmentUtil.DefaultWeaponSlotCount(def);
                        def.comps.Add(cp);
                    }
                }

                // Universal coat / jacket / duster apparel mod slot
                if (EquipmentUtil.IsApparelModEligible(def))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (def.GetCompProperties<CompProperties_ApparelMods>() == null)
                    {
                        CompProperties_ApparelMods ap = new CompProperties_ApparelMods();
                        ap.maxSlots = 1;
                        def.comps.Add(ap);
                    }
                }

                // Guarantee Colt ammo comp on The Colt
                if (def.defName == "TMM_Legendary_TheColt")
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (def.GetCompProperties<CompProperties_ColtAmmo>() == null)
                    {
                        def.comps.Add(new CompProperties_ColtAmmo());
                    }
                }

                // Guarantee additive Equipment ITab on all humanlike races (without touching ITab_Pawn_Gear)
                if (def.race != null && def.race.Humanlike)
                {
                    if (def.inspectorTabs == null) def.inspectorTabs = new List<Type>();
                    if (!def.inspectorTabs.Contains(typeof(ITab_TMM_Equipment)))
                    {
                        def.inspectorTabs.Add(typeof(ITab_TMM_Equipment));
                    }
                    if (def.inspectorTabsResolved != null)
                    {
                        bool hasResolved = false;
                        for (int t = 0; t < def.inspectorTabsResolved.Count; t++)
                        {
                            if (def.inspectorTabsResolved[t] is ITab_TMM_Equipment)
                            {
                                hasResolved = true;
                                break;
                            }
                        }
                        if (!hasResolved)
                        {
                            def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_TMM_Equipment))); // CHECK API
                        }
                    }
                }
            }
        }

        private static void RegisterArmorStatPart(StatDef stat)
        {
            if (stat == null) return;
            if (stat.parts == null) stat.parts = new List<StatPart>();
            for (int i = 0; i < stat.parts.Count; i++)
            {
                if (stat.parts[i] is StatPart_ApparelMods) return;
            }
            stat.parts.Add(new StatPart_ApparelMods(stat));
        }

        private static void ApplyHarmonyPatches()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.equipment");

            // 1. Extra weapon module damage & charge consumption on hit
            MethodInfo postApply = AccessTools.Method(typeof(Pawn), "PostApplyDamage", new Type[] { typeof(DamageInfo), typeof(float) }); // CHECK API
            if (postApply != null)
            {
                harmony.Patch(postApply, postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_Pawn_PostApplyDamage"));
            }

            // 2. Caravan carrying capacity boost from TMM_Backpack apparel
            MethodInfo massCap = AccessTools.Method(typeof(MassUtility), "Capacity", new Type[] { typeof(Pawn), typeof(StringBuilder) }); // CHECK API
            if (massCap != null)
            {
                harmony.Patch(massCap, postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_MassUtility_Capacity"));
            }

            // 3. Story gate for TMM_ColtBullets research (unlock.colt_bullets)
            PropertyInfo canStartProp = AccessTools.Property(typeof(ResearchProjectDef), "CanStartNow"); // CHECK API
            if (canStartProp != null && canStartProp.GetGetMethod() != null)
            {
                harmony.Patch(canStartProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_Research_CanStartNow"));
            }

            // 4. Story gate for Railroad Ballistic Weave recipe (unlock.railroad_ballistic)
            PropertyInfo availProp = AccessTools.Property(typeof(RecipeDef), "AvailableNow"); // CHECK API
            if (availProp != null && availProp.GetGetMethod() != null)
            {
                harmony.Patch(availProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_Recipe_AvailableNow"));
            }

            // 5. Right-click FloatMenu options for module items & Colt silver bullets on map
            MethodInfo floatMenu = AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"); // CHECK API
            if (floatMenu != null)
            {
                harmony.Patch(floatMenu, postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_AddHumanlikeOrders"));
            }

            // 6. Fallback Colt ammo consumption if CompColtAmmo runs standalone without CompLimitedCharges
            MethodInfo tryCastShot = AccessTools.Method(typeof(Verb_Shoot), "TryCastShot");
            if (tryCastShot != null)
            {
                harmony.Patch(tryCastShot,
                    prefix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Prefix_VerbShoot_ColtAmmo"),
                    postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_VerbShoot_ColtAmmo"));
            }
        }

        public static void Postfix_Pawn_PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (s_applyingModDamage) return;
            if (__instance == null || __instance.Dead) return;
            if (dinfo.Def == null || !dinfo.Def.externalViolence) return;

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return;

            ThingWithComps weapon = FindAttackerWeapon(attacker, dinfo.Weapon);
            if (weapon == null) return;

            CompWeaponMods mods = weapon.GetComp<CompWeaponMods>();
            if (mods == null || mods.InstalledMods.Count == 0) return;

            s_applyingModDamage = true;
            try
            {
                for (int i = 0; i < mods.InstalledMods.Count; i++)
                {
                    InstalledModEntry entry = mods.InstalledMods[i];
                    if (entry == null || !entry.CanTrigger) continue;

                    CompProperties_EquipmentMod mp = entry.ModProps;
                    if (mp == null || mp.extraDamageDef.NullOrEmpty() || mp.extraDamageAmount <= 0f) continue;

                    DamageDef extraDef = DefDatabase<DamageDef>.GetNamedSilentFail(mp.extraDamageDef) ?? DamageDefOf.Burn;
                    entry.ConsumeCharge();

                    if (!__instance.Dead)
                    {
                        DamageInfo extraInfo = new DamageInfo(extraDef, mp.extraDamageAmount, mp.extraArmorPenetration,
                            dinfo.Angle, attacker, dinfo.HitPart, dinfo.Weapon);
                        __instance.TakeDamage(extraInfo);
                    }
                }
            }
            finally
            {
                s_applyingModDamage = false;
            }
        }

        private static ThingWithComps FindAttackerWeapon(Pawn attacker, ThingDef weaponDef)
        {
            if (attacker == null) return null;
            if (attacker.equipment != null && attacker.equipment.Primary != null)
            {
                if (weaponDef == null || attacker.equipment.Primary.def == weaponDef)
                {
                    return attacker.equipment.Primary;
                }
            }
            if (weaponDef != null && attacker.inventory != null && attacker.inventory.innerContainer != null)
            {
                for (int i = 0; i < attacker.inventory.innerContainer.Count; i++)
                {
                    ThingWithComps twc = attacker.inventory.innerContainer[i] as ThingWithComps;
                    if (twc != null && twc.def == weaponDef) return twc;
                }
            }
            return null;
        }

        public static void Postfix_MassUtility_Capacity(Pawn p, StringBuilder explanation, ref float __result)
        {
            if (p == null || p.apparel == null) return;
            List<Apparel> worn = p.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel ap = worn[i];
                if (ap == null || ap.def == null) continue;
                BackpackExtension ext = ap.def.GetModExtension<BackpackExtension>();
                if (ext != null && ext.bonusCaravanKg > 0f)
                {
                    __result += ext.bonusCaravanKg;
                    if (explanation != null)
                    {
                        explanation.AppendLine(string.Format("  - {0}: +{1:0.#} kg", ap.LabelCap, ext.bonusCaravanKg));
                    }
                }
            }
        }

        public static void Postfix_Research_CanStartNow(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            if (__instance.defName == "TMM_ColtBullets")
            {
                if (!EquipmentUtil.HasStoryFlag("unlock.colt_bullets"))
                {
                    __result = false;
                }
            }
        }

        public static void Postfix_Recipe_AvailableNow(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            if (__instance.defName == "TMM_Make_RailroadBallisticWeave")
            {
                if (!EquipmentUtil.HasStoryFlag("unlock.railroad_ballistic"))
                {
                    __result = false;
                }
            }
        }

        public static void Postfix_AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn == null || opts == null || pawn.Map == null || !pawn.IsColonistPlayerControlled) return;
            IntVec3 cell = IntVec3.FromVector3(clickPos);
            if (!cell.InBounds(pawn.Map)) return;

            List<Thing> things = cell.GetThingList(pawn.Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t == null || t.Destroyed) continue;

                // Option 1: Install clicked module onto equipped primary weapon or Shell apparel
                CompEquipmentMod modComp = t.TryGetComp<CompEquipmentMod>();
                if (modComp != null)
                {
                    if (modComp.Props.modKind == EquipmentModKind.Weapon && pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps w = pawn.equipment.Primary;
                        CompWeaponMods wm = w.GetComp<CompWeaponMods>();
                        string fail;
                        if (wm != null && wm.CanInstall(t, out fail))
                        {
                            Thing capturedMod = t;
                            opts.Add(new FloatMenuOption(string.Format("TMM_Float_InstallOn".Translate(), capturedMod.LabelShortCap, w.LabelShortCap), delegate
                            {
                                ITab_TMM_Equipment.QueueInstallModJob(pawn, w, capturedMod);
                            }));
                        }
                    }
                    else if (modComp.Props.modKind == EquipmentModKind.Apparel && pawn.apparel != null)
                    {
                        List<Apparel> worn = pawn.apparel.WornApparel;
                        for (int a = 0; a < worn.Count; a++)
                        {
                            Apparel ap = worn[a];
                            CompApparelMods am = ap != null ? ap.GetComp<CompApparelMods>() : null;
                            string fail;
                            if (am != null && am.CanInstall(t, out fail))
                            {
                                Thing capturedMod = t;
                                Apparel capturedAp = ap;
                                opts.Add(new FloatMenuOption(string.Format("TMM_Float_InstallOn".Translate(), capturedMod.LabelShortCap, capturedAp.LabelShortCap), delegate
                                {
                                    ITab_TMM_Equipment.QueueInstallModJob(pawn, capturedAp, capturedMod);
                                }));
                            }
                        }
                    }
                }

                // Option 2: Reload Colt from clicked Silver Colt Bullets
                if (t.def != null && t.def.defName == "TMM_Ammo_ColtSilverBullet" && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    ThingWithComps colt = pawn.equipment.Primary;
                    CompColtAmmo coltAmmo = colt.GetComp<CompColtAmmo>();
                    if (coltAmmo != null && coltAmmo.Charges < coltAmmo.MaxCharges)
                    {
                        Thing capturedAmmo = t;
                        opts.Add(new FloatMenuOption(string.Format("TMM_ReloadColtGizmo".Translate(), coltAmmo.Charges, coltAmmo.MaxCharges), delegate
                        {
                            JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_ReloadColt");
                            if (jd != null)
                            {
                                Job j = JobMaker.MakeJob(jd, colt, capturedAmmo);
                                j.count = coltAmmo.MaxCharges - coltAmmo.Charges;
                                pawn.jobs.TryTakeOrderedJob(j, JobTag.Misc);
                            }
                        }));
                    }
                }
            }
        }

        public static bool Prefix_VerbShoot_ColtAmmo(Verb_Shoot __instance, ref bool __result)
        {
            ThingWithComps eq = __instance.EquipmentSource;
            if (eq == null) return true;
            CompColtAmmo ammo = eq.GetComp<CompColtAmmo>();
            if (ammo == null || ammo.HasCompLimitedCharges()) return true;
            if (ammo.Charges <= 0)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static void Postfix_VerbShoot_ColtAmmo(Verb_Shoot __instance, bool __result)
        {
            if (!__result) return;
            ThingWithComps eq = __instance.EquipmentSource;
            if (eq == null) return;
            CompColtAmmo ammo = eq.GetComp<CompColtAmmo>();
            if (ammo == null || ammo.HasCompLimitedCharges()) return;
            ammo.ConsumeRound();
        }
    }
}
