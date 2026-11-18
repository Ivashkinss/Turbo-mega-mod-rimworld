using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TurboMegaMod
{
    /// Colony roles stage 3a: promotion requirements, promote gizmo, "ready for promotion" letter, thoughts.
    /// Works with and without Ideology; the Ideology promotion ritual builds on Promote() later.
    /// Texts: Documentation/ColonyRoles-Promotion-Texts.json (Gemini).
    public static class ColonyPromotion
    {
        public static ColonyRank NextRank(ColonyRoleAssignment a)
        {
            return a == null || a.role == null || a.rank + 1 >= a.role.ranks.Count ? null : a.role.ranks[a.rank + 1];
        }

        public static int SkillLevel(Pawn p, ColonyRoleDef role)
        {
            return p.skills == null || role.skill == null ? 0 : p.skills.GetSkill(role.skill).Level;
        }

        public static float RecordValue(Pawn p, RecordDef record)
        {
            return record == null || p.records == null ? 0f : p.records.GetValue(record);
        }

        /// Null when the pawn meets every requirement of the next rank; otherwise a readable reason.
        public static string MissingFor(Pawn p, ColonyRoleAssignment a)
        {
            ColonyRank next = NextRank(a);
            if (next == null) return "TMM.Roles.MaxRank".Translate();
            var lines = new List<string>();
            int skill = SkillLevel(p, a.role);
            if (skill < next.skillMin) lines.Add("TMM.Roles.Requirement".Translate(a.role.skill.LabelCap, skill, next.skillMin));
            if (next.record != null)
            {
                float have = RecordValue(p, next.record);
                if (have < next.recordMin) lines.Add("TMM.Roles.Requirement".Translate(next.record.LabelCap, (int)have, next.recordMin));
            }
            return lines.Count == 0 ? null : string.Join("\n", lines.ToArray());
        }

        public static bool Ready(Pawn p, ColonyRoleAssignment a) { return NextRank(a) != null && MissingFor(p, a) == null; }

        public static void Promote(Pawn p, ColonyRoleAssignment a)
        {
            if (!Ready(p, a)) return;
            a.rank++;
            a.readyNotifiedFor = -1;
            ColonyRank r = a.Rank;
            ThoughtDef mine = DefDatabase<ThoughtDef>.GetNamedSilentFail(a.role.Military ? "TMM_PromotedMilitary" : "TMM_PromotedCivil");
            if (mine != null && p.needs != null && p.needs.mood != null) p.needs.mood.thoughts.memories.TryGainMemory(mine);
            ThoughtDef seen = DefDatabase<ThoughtDef>.GetNamedSilentFail("TMM_WitnessedPromotion");
            if (seen != null && p.Map != null)
                foreach (Pawn w in p.Map.mapPawns.FreeColonistsSpawned)
                    if (w != p && w.needs != null && w.needs.mood != null) w.needs.mood.thoughts.memories.TryGainMemory(seen);
            Find.LetterStack.ReceiveLetter("TMM.Roles.PromotedTitle".Translate(p.LabelShort),
                "TMM.Roles.PromotedText".Translate(p.LabelShort, r.label, a.role.label), LetterDefOf.PositiveEvent, p);
        }

        /// Called periodically: one "ready for promotion" letter per reached rank.
        public static void NotifyReady(ColonyRoles roles)
        {
            foreach (ColonyRoleAssignment a in roles.All)
            {
                if (a.pawn == null || a.pawn.Dead || !a.pawn.IsColonist || a.readyNotifiedFor == a.rank + 1 || !Ready(a.pawn, a)) continue;
                a.readyNotifiedFor = a.rank + 1;
                ColonyRank next = NextRank(a);
                Find.LetterStack.ReceiveLetter("TMM.Roles.ReadyTitle".Translate(a.pawn.LabelShort),
                    "TMM.Roles.ReadyText".Translate(a.pawn.LabelShort, next.label, a.role.label), LetterDefOf.NeutralEvent, a.pawn);
            }
        }

        public static Gizmo PromoteGizmo(Pawn p, ColonyRoleAssignment a)
        {
            ColonyRank next = NextRank(a);
            if (next == null) return null;
            string missing = MissingFor(p, a);
            var cmd = new Command_Action
            {
                defaultLabel = "TMM.Roles.PromoteButton".Translate(next.label),
                defaultDesc = "TMM.Roles.NextRank".Translate(next.label) + "\n" + next.description + (missing == null ? "" : "\n\n" + missing),
                icon = TexCommand.Draft,
                action = () => Promote(p, a)
            };
            if (missing != null) cmd.Disable(missing);
            return cmd;
        }
    }
}
