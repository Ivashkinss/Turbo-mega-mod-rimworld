using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    /// <summary>
    /// Optional uniform definition attached to a ColonyRank in XML.
    /// Supports apparel defs (synced into a vanilla ApparelPolicy) and optional weapon def(s).
    /// </summary>
    public sealed class ColonyRankUniform
    {
        public List<ThingDef> apparel = new List<ThingDef>();
        public ThingDef weapon;
        public List<ThingDef> weapons = new List<ThingDef>();

        public bool HasApparel
        {
            get
            {
                if (apparel == null) return false;
                for (int i = 0; i < apparel.Count; i++)
                {
                    if (apparel[i] != null) return true;
                }
                return false;
            }
        }

        public bool HasWeapons
        {
            get
            {
                if (weapon != null) return true;
                if (weapons == null) return false;
                for (int i = 0; i < weapons.Count; i++)
                {
                    if (weapons[i] != null) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns all valid weapon ThingDefs in priority order (single weapon first, then weapons list).
        /// </summary>
        public List<ThingDef> GetWeapons()
        {
            List<ThingDef> result = new List<ThingDef>();
            if (weapon != null) result.Add(weapon);
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    ThingDef w = weapons[i];
                    if (w != null && !result.Contains(w)) result.Add(w);
                }
            }
            return result;
        }

        public bool ContainsWeapon(ThingDef def)
        {
            if (def == null) return false;
            if (weapon == def) return true;
            if (weapons == null) return false;
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] == def) return true;
            }
            return false;
        }

        /// <summary>
        /// Priority index of the weapon within the rank uniform (0 = highest priority, -1 = not in uniform).
        /// </summary>
        public int WeaponPriorityIndex(ThingDef def)
        {
            if (def == null) return -1;
            List<ThingDef> list = GetWeapons();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == def) return i;
            }
            return -1;
        }
    }

    /// <summary>
    /// Creates and maintains vanilla ApparelPolicy entries ("Role: Rank") in Current.Game.outfitDatabase
    /// and switches pawns onto their rank policy upon role assignment / promotion while preserving
    /// any custom player-chosen policies.
    /// </summary>
    public static class ColonyRankUniforms
    {
        private const float UpgradeScoreMargin = 1.12f;
        private const float MinimumWeaponHpRatio = 0.5f;
        private const int EquipCooldownTicks = 600;

        private static readonly Dictionary<int, int> LastEquipOrderTickByPawn = new Dictionary<int, int>();

        public static string PolicyLabel(ColonyRoleDef role, ColonyRank rank)
        {
            if (role == null || rank == null) return string.Empty;
            string rolePart = role.LabelCap.ToString();
            string rankPart = string.IsNullOrEmpty(rank.label) ? string.Empty : rank.label.CapitalizeFirst();
            return rolePart + ": " + rankPart;
        }

        /// <summary>
        /// Ensures a vanilla ApparelPolicy exists and matches the rank's uniform apparel list.
        /// Returns null when the rank has no apparel uniform or no game/database is loaded.
        /// </summary>
        public static ApparelPolicy EnsurePolicyFor(ColonyRoleDef role, ColonyRank rank)
        {
            if (role == null || rank == null || rank.uniform == null || !rank.uniform.HasApparel) return null;
            if (Current.Game == null || Current.Game.outfitDatabase == null) return null;

            OutfitDatabase db = Current.Game.outfitDatabase;
            string targetLabel = PolicyLabel(role, rank);
            if (string.IsNullOrEmpty(targetLabel)) return null;

            ApparelPolicy policy = FindPolicyByLabel(db, targetLabel);
            if (policy == null)
            {
                policy = db.MakeNewOutfit(); // CHECK API
                if (policy == null) return null;
                policy.label = targetLabel;
                // Only a new policy gets the uniform list: what the player allows or forbids later is theirs, also after loading.
                ConfigurePolicyFilter(policy, rank.uniform);
            }

            return policy;
        }

        /// <summary>
        /// Synchronizes all defined role/rank uniform policies into Current.Game.outfitDatabase.
        /// Safe to call on new game, load, or promotion.
        /// </summary>
        public static void SyncAllRankPolicies()
        {
            if (Current.Game == null || Current.Game.outfitDatabase == null) return;
            List<ColonyRoleDef> allRoles = DefDatabase<ColonyRoleDef>.AllDefsListForReading;
            for (int i = 0; i < allRoles.Count; i++)
            {
                ColonyRoleDef role = allRoles[i];
                if (role == null || role.ranks == null) continue;
                for (int j = 0; j < role.ranks.Count; j++)
                {
                    ColonyRank rank = role.ranks[j];
                    if (rank != null && rank.uniform != null && rank.uniform.HasApparel)
                    {
                        EnsurePolicyFor(role, rank);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the given ApparelPolicy corresponds to any role/rank uniform policy.
        /// </summary>
        public static bool IsManagedRankPolicy(ApparelPolicy policy)
        {
            ColonyRoleDef role;
            ColonyRank rank;
            return TryGetManagedRank(policy, out role, out rank);
        }

        internal static bool TryGetManagedRank(ApparelPolicy policy, out ColonyRoleDef managedRole, out ColonyRank managedRank)
        {
            managedRole = null;
            managedRank = null;
            if (policy == null || string.IsNullOrEmpty(policy.label)) return false;
            List<ColonyRoleDef> allRoles = DefDatabase<ColonyRoleDef>.AllDefsListForReading;
            for (int i = 0; i < allRoles.Count; i++)
            {
                ColonyRoleDef role = allRoles[i];
                if (role == null || role.ranks == null) continue;
                for (int j = 0; j < role.ranks.Count; j++)
                {
                    ColonyRank rank = role.ranks[j];
                    if (rank == null || rank.uniform == null || !rank.uniform.HasApparel) continue;
                    if (string.Equals(policy.label, PolicyLabel(role, rank), StringComparison.OrdinalIgnoreCase))
                    {
                        managedRole = role;
                        managedRank = rank;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsPolicyForRank(ApparelPolicy policy, ColonyRoleDef role, ColonyRank rank)
        {
            if (policy == null || role == null || rank == null) return false;
            return string.Equals(policy.label, PolicyLabel(role, rank), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Switches the pawn to the new rank's ApparelPolicy ONLY if the pawn currently uses
        /// the default policy, the previous rank's policy, or another managed rank policy.
        /// Any custom policy manually chosen by the player is never overwritten.
        /// </summary>
        public static bool ApplyUniformPolicy(Pawn pawn, ColonyRoleDef role, ColonyRank newRank, ColonyRank previousRank)
        {
            if (pawn == null || pawn.Dead || pawn.outfits == null) return false;
            if (role == null || newRank == null || newRank.uniform == null || !newRank.uniform.HasApparel) return false;
            if (Current.Game == null || Current.Game.outfitDatabase == null) return false;

            ApparelPolicy targetPolicy = EnsurePolicyFor(role, newRank);
            if (targetPolicy == null) return false;

            OutfitDatabase db = Current.Game.outfitDatabase;
            ApparelPolicy current = pawn.outfits.CurrentApparelPolicy; // CHECK API
            ApparelPolicy defaultPolicy = db.DefaultOutfit(); // CHECK API

            // After a rank with a uniform the default policy can only be the player's own choice: keep it.
            bool hadUniform = previousRank != null && previousRank.uniform != null && previousRank.uniform.HasApparel;
            bool canReplace = current == null
                || (current == defaultPolicy && !hadUniform)
                || IsPolicyForRank(current, role, previousRank)
                || IsManagedRankPolicy(current);

            if (!canReplace) return false;

            pawn.outfits.CurrentApparelPolicy = targetPolicy; // CHECK API
            return true;
        }

        /// A colonist under a rank policy never takes off unique apparel by itself, and takes off other apparel that is not part
        /// of the uniform only when a uniform piece for that place lies ready in storage. Without this the policy, which the
        /// mod switches on by itself, undressed the colonist at once and others picked the clothes up.
        public static bool KeepsOn(Pawn pawn, Apparel worn)
        {
            if (pawn == null || worn == null || pawn.outfits == null) return false;
            ApparelPolicy policy = pawn.outfits.CurrentApparelPolicy;
            if (!IsManagedRankPolicy(policy)) return false;
            if (IsUnique(worn.def)) return true;
            return !policy.filter.Allows(worn) && !UniformPieceReady(pawn, worn, policy);
        }

        private static bool IsUnique(ThingDef def)
        {
            if (def.HasModExtension<SignatureGearExtension>() || def.GetCompProperties<CompProperties_LegendaryItem>() != null) return true;
            return def.apparel != null && def.apparel.tags != null && def.apparel.tags.Contains("TMM_SignatureGear");
        }

        private static bool UniformPieceReady(Pawn pawn, Apparel worn, ApparelPolicy policy)
        {
            if (!pawn.Spawned) return false;
            List<Thing> all = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            for (int i = 0; i < all.Count; i++)
            {
                Apparel piece = all[i] as Apparel;
                if (piece == null || !policy.filter.Allows(piece) || ApparelUtility.CanWearTogether(worn.def, piece.def, pawn.RaceProps.body)) continue;
                if (!piece.IsInAnyStorage() || piece.IsForbidden(pawn) || piece.IsBurning()) continue;
                if (CompBiocodable.IsBiocoded(piece) && !CompBiocodable.IsBiocodedFor(piece, pawn)) continue;
                if (!ApparelUtility.HasPartsToWear(pawn, piece.def) || !piece.PawnCanWear(pawn, true)) continue;
                if (JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, piece) < 0.05f) continue;
                if (pawn.CanReserveAndReach(piece, PathEndMode.OnCell, pawn.NormalMaxDanger())) return true;
            }
            return false;
        }

        private static ApparelPolicy FindPolicyByLabel(OutfitDatabase db, string label)
        {
            if (db == null || db.AllOutfits == null) return null;
            List<ApparelPolicy> all = db.AllOutfits;
            for (int i = 0; i < all.Count; i++)
            {
                ApparelPolicy p = all[i];
                if (p != null && string.Equals(p.label, label, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

        private static void ConfigurePolicyFilter(ApparelPolicy policy, ColonyRankUniform uniform)
        {
            if (policy == null || policy.filter == null || uniform == null) return;

            policy.filter.SetDisallowAll();

            if (SpecialThingFilterDefOf.AllowDeadmansApparel != null)
            {
                policy.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, false);
            }

            if (uniform.apparel != null)
            {
                for (int i = 0; i < uniform.apparel.Count; i++)
                {
                    ThingDef def = uniform.apparel[i];
                    if (def != null && def.IsApparel)
                    {
                        policy.filter.SetAllow(def, true);
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates a weapon's practical value for deciding whether a rank uniform weapon is an upgrade.
        /// Uses MarketValue (incorporates tech tier, quality, stuff, and hit points) with a small bonus
        /// for higher-priority rank weapons so pawns prefer their rank's signature firearm over equal trash.
        /// </summary>
        public static float EvaluateWeaponScore(ThingWithComps weapon, ColonyRankUniform uniform)
        {
            if (weapon == null || weapon.Destroyed) return 0f;

            float score = weapon.MarketValue;
            if (score < 1f) score = 1f;

            if (uniform != null)
            {
                int idx = uniform.WeaponPriorityIndex(weapon.def);
                if (idx >= 0)
                {
                    float tierBonus = 1f + Math.Max(0, 3 - idx) * 0.03f;
                    score *= tierBonus;
                }
            }

            return score;
        }

        /// <summary>
        /// Checks whether a candidate weapon from the armory is eligible for the pawn and strictly better
        /// than the pawn's current primary weapon (with hysteresis to guarantee zero re-equip loops).
        /// </summary>
        public static bool IsCandidateBetterThanCurrent(Pawn pawn, ThingWithComps candidate, ColonyRankUniform uniform)
        {
            if (pawn == null || candidate == null || candidate.Destroyed || uniform == null) return false;
            if (!uniform.ContainsWeapon(candidate.def)) return false;

            if (candidate.def.useHitPoints && candidate.MaxHitPoints > 0)
            {
                float hpRatio = (float)candidate.HitPoints / candidate.MaxHitPoints;
                if (hpRatio < MinimumWeaponHpRatio) return false;
            }

            string cantReason;
            if (!EquipmentUtility.CanEquip(candidate, pawn, out cantReason)) return false; // CHECK API

            ThingWithComps current = pawn.equipment == null ? null : pawn.equipment.Primary;
            if (current == null) return true;
            if (current == candidate) return false;

            // Never override a player-bound persona/bladelink weapon automatically.
            if (IsPersonaWeapon(current)) return false;

            float currentScore = EvaluateWeaponScore(current, uniform);
            float candidateScore = EvaluateWeaponScore(candidate, uniform);

            // If pawn already holds the exact same weapon def, require a clear quality/value jump.
            if (current.def == candidate.def)
            {
                return candidateScore >= currentScore * 1.25f && (candidateScore - currentScore) >= 60f;
            }

            // Require strict hysteresis margin so two similar weapons can never cause swap loops.
            return candidateScore >= currentScore * UpgradeScoreMargin;
        }

        private static bool IsPersonaWeapon(ThingWithComps weapon)
        {
            if (weapon == null) return false;
            if (ModsConfig.RoyaltyActive && weapon.TryGetComp<CompBladelinkWeapon>() != null) return true; // CHECK API
            return false;
        }

        public static bool IsOnEquipCooldown(Pawn pawn)
        {
            if (pawn == null || Find.TickManager == null) return false;
            int lastTick;
            if (LastEquipOrderTickByPawn.TryGetValue(pawn.thingIDNumber, out lastTick))
            {
                return Find.TickManager.TicksGame - lastTick < EquipCooldownTicks;
            }
            return false;
        }

        public static void MarkEquipOrderIssued(Pawn pawn)
        {
            if (pawn == null || Find.TickManager == null) return;
            LastEquipOrderTickByPawn[pawn.thingIDNumber] = Find.TickManager.TicksGame;
        }

        public static void ClearCooldowns()
        {
            LastEquipOrderTickByPawn.Clear();
        }
    }

    /// Vanilla asks this only when a colonist optimizes apparel by itself; orders of the player do not pass here.
    [StaticConstructorOnStartup]
    public static class ColonyRankUniformHarmony
    {
        static ColonyRankUniformHarmony()
        {
            MethodInfo drop = AccessTools.Method(typeof(OutfitForcedHandler), "AllowedToAutomaticallyDrop");
            if (drop == null) { Log.Warning("[TMM] Rank uniforms: AllowedToAutomaticallyDrop not found; rank policies may undress colonists."); return; }
            new Harmony("colonyproject.integrated.rankuniforms").Patch(drop, postfix: new HarmonyMethod(typeof(ColonyRankUniformHarmony), "Postfix_AllowedToAutomaticallyDrop"));
        }

        public static void Postfix_AllowedToAutomaticallyDrop(Apparel __0, ref bool __result)
        {
            if (__result && __0 != null && ColonyRankUniforms.KeepsOn(__0.Wearer, __0)) __result = false;
        }
    }

    /// <summary>
    /// Low-priority WorkGiver allowing colonists with a rank weapon uniform to self-equip
    /// a matching weapon stored in colony storage (armory) if they do not already carry a better one.
    /// </summary>
    public sealed class WorkGiver_EquipRankWeapon : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForGroup(ThingRequestGroup.Weapon); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.OnCell; }
        }

        public override bool Prioritized
        {
            get { return true; }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn == null || !pawn.IsFreeColonist || pawn.Dead || pawn.Downed || pawn.Drafted) return true;
            if (pawn.InMentalState || pawn.equipment == null) return true;
            if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return true;
            if (!forced && ColonyRankUniforms.IsOnEquipCooldown(pawn)) return true;

            ColonyRankUniform uniform = GetActiveUniform(pawn);
            if (uniform == null || !uniform.HasWeapons) return true;

            return false;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Map.listerThings == null)
            {
                return new List<Thing>();
            }
            return pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            ThingWithComps weapon = t.Thing as ThingWithComps;
            ColonyRankUniform uniform = GetActiveUniform(pawn);
            return ColonyRankUniforms.EvaluateWeaponScore(weapon, uniform);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            ThingWithComps weapon = t as ThingWithComps;
            if (weapon == null || !weapon.Spawned || weapon.Destroyed) return false;
            if (weapon.IsForbidden(pawn) || weapon.IsBurning()) return false;

            // Armory check: only pick weapons kept in colony storage (shelves, racks, stockpiles).
            if (!weapon.IsInAnyStorage()) return false; // CHECK API

            ColonyRankUniform uniform = GetActiveUniform(pawn);
            if (uniform == null || !uniform.HasWeapons) return false;

            if (!ColonyRankUniforms.IsCandidateBetterThanCurrent(pawn, weapon, uniform)) return false;

            if (!pawn.CanReserveAndReach(weapon, PathEndMode.OnCell, forced ? Danger.Deadly : Danger.None))
            {
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null) return null;
            ColonyRankUniforms.MarkEquipOrderIssued(pawn);
            return JobMaker.MakeJob(JobDefOf.Equip, t);
        }

        private static ColonyRankUniform GetActiveUniform(Pawn pawn)
        {
            ColonyRoles roles = ColonyRoles.Instance;
            if (roles == null || pawn == null) return null;
            ColonyRoleAssignment assignment = roles.For(pawn);
            if (assignment == null || assignment.Rank == null) return null;
            return RankWeaponPreferences.Get(assignment.role, assignment.Rank);
        }
    }

    public sealed class RankWeaponPreferences : GameComponent
    {
        private List<Preference> overrides = new List<Preference>();

        public RankWeaponPreferences(Game game) { }

        internal static ColonyRankUniform Get(ColonyRoleDef role, ColonyRank rank)
        {
            RankWeaponPreferences storage = Current.Game.GetComponent<RankWeaponPreferences>();
            Preference preference = storage.Find(role, rank);
            return preference == null ? rank.uniform : preference.Uniform;
        }

        internal static void Save(ColonyRoleDef role, ColonyRank rank, List<ThingDef> weapons)
        {
            RankWeaponPreferences storage = Current.Game.GetComponent<RankWeaponPreferences>();
            Preference preference = storage.Find(role, rank);
            if (preference == null)
            {
                preference = new Preference { Role = role, Rank = role.ranks.IndexOf(rank) };
                storage.overrides.Add(preference);
            }
            preference.Uniform.weapons = new List<ThingDef>(weapons);
        }

        private Preference Find(ColonyRoleDef role, ColonyRank rank)
        {
            int index = role.ranks.IndexOf(rank);
            return overrides.Find(preference => preference.Role == role && preference.Rank == index);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref overrides, "TMM_rankWeaponPreferences", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && overrides == null) overrides = new List<Preference>();
        }

        private sealed class Preference : IExposable
        {
            public ColonyRoleDef Role;
            public int Rank;
            public readonly ColonyRankUniform Uniform = new ColonyRankUniform();

            public Preference() { }

            public void ExposeData()
            {
                Scribe_Defs.Look(ref Role, "role");
                Scribe_Values.Look(ref Rank, "rank");
                Scribe_Collections.Look(ref Uniform.weapons, "weapons", LookMode.Def);
                if (Scribe.mode == LoadSaveMode.PostLoadInit && Uniform.weapons == null) Uniform.weapons = new List<ThingDef>();
            }
        }
    }
}
