using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class MainTabWindow_FactionHierarchy : MainTabWindow
    {
        public Faction selectedFaction = null;
        private Vector2 factionListScroll = Vector2.zero;
        private Vector2 hierarchyTreeScroll = Vector2.zero;

        public override Vector2 InitialSize
        {
            get { return new Vector2(1040f, 680f); }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg != null)
            {
                reg.EnsureLeadersInitialized();
            }
            EnsureSelection();
        }

        private void EnsureSelection()
        {
            if (Find.FactionManager == null) return;
            List<Faction> visible = GetVisibleFactions();
            if (visible.Count == 0)
            {
                selectedFaction = null;
                return;
            }
            if (selectedFaction == null || !visible.Contains(selectedFaction))
            {
                selectedFaction = visible[0];
            }
        }

        public static List<Faction> GetVisibleFactions()
        {
            if (Find.FactionManager == null) return new List<Faction>();
            List<Faction> list = Find.FactionManager.AllFactionsVisible.ToList();

            // Sort: factions with HeroDefs first, then player faction, then others
            return list.OrderByDescending(f => !f.IsPlayer && HasHeroDefs(f))
                       .ThenBy(f => f.IsPlayer)
                       .ThenBy(f => f.Name)
                       .ToList();
        }

        public static bool HasHeroDefs(Faction f)
        {
            if (f == null || f.def == null) return false;
            string fd = f.def.defName;
            return DefDatabase<HeroDef>.AllDefs.Any(d => d.factionDef == fd);
        }

        public static List<HeroDef> GetFactionHeroDefs(Faction f)
        {
            if (f == null || f.def == null) return new List<HeroDef>();
            string fd = f.def.defName;
            return DefDatabase<HeroDef>.AllDefs
                .Where(d => d.factionDef == fd)
                .OrderByDescending(d => d.isLeader)
                .ThenByDescending(d => d.councilMember)
                .ThenByDescending(d => d.rank)
                .ToList();
        }

        public static Pawn GetDisplayedLeader(Faction f)
        {
            if (f == null) return null;
            return f.leader;
        }

        public static List<HeroDef> GetCouncilMembers(Faction f)
        {
            return GetFactionHeroDefs(f).Where(d => d.councilMember).ToList();
        }

        public static List<HeroDef> GetLeaderDefs(Faction f)
        {
            return GetFactionHeroDefs(f).Where(d => d.isLeader && !d.councilMember).ToList();
        }

        public static List<HeroDef> GetRank4Defs(Faction f)
        {
            return GetFactionHeroDefs(f).Where(d => d.rank == 4 && !d.isLeader && !d.councilMember).ToList();
        }

        public static List<HeroDef> GetRank3Defs(Faction f)
        {
            return GetFactionHeroDefs(f).Where(d => d.rank == 3 && !d.isLeader && !d.councilMember).ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont origFont = Text.Font;
            TextAnchor origAnchor = Text.Anchor;
            Color origColor = GUI.color;
            bool origWrap = Text.WordWrap;

            try
            {
                EnsureSelection();

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, 0f, 400f, 35f), "TMM_FactionHierarchy_Title".Translate());
                Text.Font = GameFont.Small;

                float mainY = 40f;
                float leftWidth = 310f;
                Rect leftBounds = new Rect(0f, mainY, leftWidth, inRect.height - mainY);
                Rect rightBounds = new Rect(leftWidth + 12f, mainY, inRect.width - leftWidth - 12f, inRect.height - mainY);

                DrawFactionList(leftBounds);
                DrawFactionHierarchy(rightBounds);
            }
            finally
            {
                Text.Font = origFont;
                Text.Anchor = origAnchor;
                GUI.color = origColor;
                Text.WordWrap = origWrap;
            }
        }

        private void DrawFactionList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);

            List<Faction> factions = GetVisibleFactions();
            float rowHeight = 60f;
            float totalHeight = (float)factions.Count * (rowHeight + 4f);
            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, Mathf.Max(inner.height, totalHeight));

            Widgets.BeginScrollView(inner, ref factionListScroll, viewRect);
            float curY = 0f;

            for (int i = 0; i < factions.Count; i++)
            {
                Faction f = factions[i];
                Rect rowRect = new Rect(0f, curY, viewRect.width, rowHeight);

                if (f == selectedFaction)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(rowRect);
                }

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedFaction = f;
                }

                // Faction icon
                Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + 6f, 28f, 28f);
                if (f.def != null && f.def.FactionIcon != null)
                {
                    GUI.color = FactionIconPresentation.ReadableTint(f);
                    GUI.DrawTexture(iconRect, f.def.FactionIcon);
                    GUI.color = Color.white;
                }

                // Faction name
                float textX = iconRect.xMax + 6f;
                float textW = rowRect.width - textX - 4f;
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(textX, rowRect.y + 4f, textW, 22f), f.Name);

                // Relation & settlements
                Text.Font = GameFont.Tiny;
                string relText = f.IsPlayer
                    ? "Player".Translate().ToString()
                    : f.PlayerGoodwill.ToStringWithSign() + " (" + f.PlayerRelationKind.GetLabel() + ")";

                if (!f.IsPlayer)
                {
                    if (f.PlayerRelationKind == FactionRelationKind.Ally) GUI.color = Color.green;
                    else if (f.PlayerRelationKind == FactionRelationKind.Hostile) GUI.color = Color.red;
                    else GUI.color = Color.yellow;
                }
                Widgets.Label(new Rect(textX, rowRect.y + 24f, textW, 16f), relText);
                GUI.color = Color.white;

                int settlements = Find.WorldObjects != null ? Find.WorldObjects.Settlements.Count(s => s.Faction == f) : 0;
                string detail = "TMM_FactionHierarchy_SettlementsCount".Translate(settlements);
                if (HasHeroDefs(f))
                {
                    detail += " | " + "★";
                }
                GUI.color = Color.gray;
                Widgets.Label(new Rect(textX, rowRect.y + 40f, textW, 16f), detail);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                curY += rowHeight + 4f;
            }

            Widgets.EndScrollView();
        }

        private void DrawFactionHierarchy(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(10f);

            if (selectedFaction == null) return;

            // Header: Faction details
            float headerH = 44f;
            Rect iconRect = new Rect(inner.x, inner.y, 36f, 36f);
            if (selectedFaction.def != null && selectedFaction.def.FactionIcon != null)
            {
                GUI.color = FactionIconPresentation.ReadableTint(selectedFaction);
                GUI.DrawTexture(iconRect, selectedFaction.def.FactionIcon);
                GUI.color = Color.white;
            }

            float hX = iconRect.xMax + 10f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(hX, inner.y, 400f, 32f), selectedFaction.Name);
            Text.Font = GameFont.Small;

            string leaderName = selectedFaction.leader != null ? selectedFaction.leader.Name.ToStringFull : "TMM_FactionHierarchy_None".Translate().ToString();
            string leaderStr = "TMM_FactionHierarchy_Leader".Translate(leaderName);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(hX, inner.y + 24f, 400f, 20f), leaderStr);
            GUI.color = Color.white;

            float contentY = inner.y + headerH + 6f;
            Rect contentRect = new Rect(inner.x, contentY, inner.width, inner.height - headerH - 6f);

            bool hasHeroes = HasHeroDefs(selectedFaction);
            if (!hasHeroes)
            {
                DrawFactionWithoutHeroes(contentRect);
            }
            else
            {
                DrawFactionWithHeroes(contentRect);
            }
        }

        private void DrawFactionWithoutHeroes(Rect rect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x, rect.y + 10f, rect.width, 30f), "TMM_FactionHierarchy_HierarchyUnknown".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (selectedFaction.leader != null)
            {
                Rect leaderCardRect = new Rect(rect.x, rect.y + 50f, rect.width - 20f, 68f);
                DrawGenericLeaderCard(leaderCardRect, selectedFaction.leader);
            }
        }

        private void DrawGenericLeaderCard(Rect rect, Pawn leader)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);

            // Portrait
            Rect portraitRect = new Rect(inner.x, inner.y, 56f, 56f);
            DrawPawnPortrait(portraitRect, leader);

            float textX = portraitRect.xMax + 10f;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(textX, inner.y + 2f, 300f, 22f), leader.Name.ToStringFull);

            GUI.color = Color.yellow;
            Widgets.Label(new Rect(textX, inner.y + 24f, 200f, 20f), "[" + "TMM_FactionHierarchy_LeaderTag".Translate() + "]");
            GUI.color = Color.white;

            string loc = leader.Spawned && leader.Map != null
                ? (leader.Map.Parent != null ? leader.Map.Parent.LabelCap.ToString() : "TMM_FactionHierarchy_Location_Map".Translate().ToString())
                : "TMM_FactionHierarchy_Location_World".Translate().ToString();
            GUI.color = Color.gray;
            Widgets.Label(new Rect(textX, inner.y + 44f, 300f, 18f), loc);
            GUI.color = Color.white;

            if (leader.Spawned)
            {
                Rect btnRect = new Rect(rect.xMax - 85f, inner.y + 16f, 75f, 26f);
                if (Widgets.ButtonText(btnRect, "TMM_FactionHierarchy_ViewOnMap".Translate()))
                {
                    CameraJumper.TryJump(leader);
                }
            }
        }

        private void DrawFactionWithHeroes(Rect rect)
        {
            List<HeroDef> allHeroes = GetFactionHeroDefs(selectedFaction);
            HeroRegistry reg = HeroRegistry.Instance;

            // Categorize
            List<HeroDef> council = GetCouncilMembers(selectedFaction);
            List<HeroDef> leaders = GetLeaderDefs(selectedFaction);
            List<HeroDef> rank4 = GetRank4Defs(selectedFaction);
            List<HeroDef> rank3 = GetRank3Defs(selectedFaction);

            float rowHeight = 72f;
            float sectionHeaderHeight = 28f;
            int totalSections = (council.Count > 0 ? 1 : 0) + (leaders.Count > 0 ? 1 : 0) + (rank4.Count > 0 ? 1 : 0) + (rank3.Count > 0 ? 1 : 0);
            float totalHeight = (float)allHeroes.Count * (rowHeight + 4f) + (float)totalSections * sectionHeaderHeight + 20f;

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, totalHeight));
            Widgets.BeginScrollView(rect, ref hierarchyTreeScroll, viewRect);
            float curY = 0f;

            // 1. Council
            if (council.Count > 0)
            {
                DrawSectionHeader(new Rect(0f, curY, viewRect.width, sectionHeaderHeight), "TMM_FactionHierarchy_Council".Translate());
                curY += sectionHeaderHeight;
                for (int i = 0; i < council.Count; i++)
                {
                    DrawHeroCard(new Rect(0f, curY, viewRect.width, rowHeight), council[i], reg);
                    curY += rowHeight + 4f;
                }
            }

            // 2. Leaders
            if (leaders.Count > 0)
            {
                DrawSectionHeader(new Rect(0f, curY, viewRect.width, sectionHeaderHeight), "TMM_FactionHierarchy_LeaderTag".Translate());
                curY += sectionHeaderHeight;
                for (int i = 0; i < leaders.Count; i++)
                {
                    DrawHeroCard(new Rect(0f, curY, viewRect.width, rowHeight), leaders[i], reg);
                    curY += rowHeight + 4f;
                }
            }

            // 3. Rank 4
            if (rank4.Count > 0)
            {
                DrawSectionHeader(new Rect(0f, curY, viewRect.width, sectionHeaderHeight), "TMM_FactionHierarchy_Rank4Section".Translate());
                curY += sectionHeaderHeight;
                for (int i = 0; i < rank4.Count; i++)
                {
                    DrawHeroCard(new Rect(0f, curY, viewRect.width, rowHeight), rank4[i], reg);
                    curY += rowHeight + 4f;
                }
            }

            // 4. Rank 3
            if (rank3.Count > 0)
            {
                DrawSectionHeader(new Rect(0f, curY, viewRect.width, sectionHeaderHeight), "TMM_FactionHierarchy_Rank3Section".Translate());
                curY += sectionHeaderHeight;
                for (int i = 0; i < rank3.Count; i++)
                {
                    DrawHeroCard(new Rect(0f, curY, viewRect.width, rowHeight), rank3[i], reg);
                    curY += rowHeight + 4f;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawSectionHeader(Rect rect, string title)
        {
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 22f), title);
            Widgets.DrawLineHorizontal(rect.x, rect.y + 24f, rect.width);
            GUI.color = Color.white;
        }

        private void DrawHeroCard(Rect rect, HeroDef def, HeroRegistry reg)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);

            HeroRecord rec = reg != null ? reg.RecordFor(def) : null;
            bool isKnown = reg != null && reg.IsKnown(def, rec, selectedFaction);

            if (!isKnown)
            {
                DrawUnknownHeroCard(inner, def);
                return;
            }

            // Known hero
            Pawn pawn = rec != null ? rec.pawn : null;
            if (pawn == null && reg != null && (def.isLeader || def.councilMember))
            {
                pawn = reg.GetOrCreate(def);
                rec = reg.RecordFor(def);
            }

            // Portrait
            Rect portraitRect = new Rect(inner.x, inner.y, 58f, 58f);
            if (pawn != null)
            {
                DrawPawnPortrait(portraitRect, pawn);
            }
            else
            {
                DrawUnknownSilhouette(portraitRect);
            }

            // Name & Nickname
            float textX = portraitRect.xMax + 10f;
            Text.Font = GameFont.Small;
            string displayName = pawn != null ? pawn.Name.ToStringFull : (def.nameFirst + " '" + def.nameNick + "' " + def.nameLast);
            Widgets.Label(new Rect(textX, inner.y + 2f, 320f, 22f), displayName);

            // Badges & Rank
            float badgeX = textX;
            float badgeY = inner.y + 24f;

            if (selectedFaction.leader != null && pawn != null && selectedFaction.leader == pawn)
            {
                GUI.color = Color.yellow;
                string lTag = "[" + "TMM_FactionHierarchy_LeaderTag".Translate() + "]";
                Vector2 sz = Text.CalcSize(lTag);
                Widgets.Label(new Rect(badgeX, badgeY, sz.x + 4f, 20f), lTag);
                badgeX += sz.x + 8f;
                GUI.color = Color.white;
            }
            else if (def.councilMember)
            {
                GUI.color = Color.cyan;
                string cTag = "[" + "TMM_FactionHierarchy_CouncilMember".Translate() + "]";
                Vector2 sz = Text.CalcSize(cTag);
                Widgets.Label(new Rect(badgeX, badgeY, sz.x + 4f, 20f), cTag);
                badgeX += sz.x + 8f;
                GUI.color = Color.white;
            }

            if (IsSuccessorOfCurrentLeader(def, selectedFaction))
            {
                GUI.color = Color.green;
                string sTag = "→ [" + "TMM_FactionHierarchy_SuccessorTag".Translate() + "]";
                Vector2 sz = Text.CalcSize(sTag);
                Widgets.Label(new Rect(badgeX, badgeY, sz.x + 4f, 20f), sTag);
                badgeX += sz.x + 8f;
                GUI.color = Color.white;
            }

            string rTag = "TMM_FactionHierarchy_RankTag".Translate(def.rank);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(badgeX, badgeY, 120f, 20f), rTag);
            GUI.color = Color.white;

            // State & Location
            HeroState st = rec != null ? rec.state : HeroState.Unspawned;
            string stateLabel = GetStateLabel(st, pawn);
            GUI.color = GetStateColor(st);
            Widgets.Label(new Rect(textX, inner.y + 44f, 320f, 18f), stateLabel);
            GUI.color = Color.white;

            // Backstory Tooltip
            string tip = GetHeroTooltip(def, pawn, st);
            TooltipHandler.TipRegion(rect, tip);

            // View on map button
            if (pawn != null && (pawn.Spawned || pawn.GetCaravan() != null))
            {
                Rect btnRect = new Rect(rect.xMax - 85f, inner.y + 16f, 75f, 26f);
                if (Widgets.ButtonText(btnRect, "TMM_FactionHierarchy_ViewOnMap".Translate()))
                {
                    if (pawn.Spawned) CameraJumper.TryJump(pawn);
                    else if (pawn.GetCaravan() != null) CameraJumper.TryJump(pawn.GetCaravan());
                }
            }
        }

        private void DrawUnknownHeroCard(Rect inner, HeroDef def)
        {
            Rect portraitRect = new Rect(inner.x, inner.y, 58f, 58f);
            DrawUnknownSilhouette(portraitRect);

            float textX = portraitRect.xMax + 10f;
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(textX, inner.y + 6f, 320f, 22f), "TMM_FactionHierarchy_UnknownHero".Translate(def.rank));

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(textX, inner.y + 30f, 320f, 20f), "TMM_FactionHierarchy_UnknownHeroDesc".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(inner, "TMM_FactionHierarchy_UnknownHeroTip".Translate());
        }

        private static bool IsSuccessorOfCurrentLeader(HeroDef def, Faction faction)
        {
            if (def == null || faction == null) return false;
            HeroRegistry reg = HeroRegistry.Instance;
            if (reg == null) return false;
            Pawn leader = faction.leader;
            if (leader == null) return false;
            HeroRecord leaderRec = reg.Records.FirstOrDefault(r => r.pawn == leader);
            if (leaderRec == null) return false;
            HeroDef leaderDef = DefDatabase<HeroDef>.GetNamedSilentFail(leaderRec.heroDefName);
            return leaderDef != null && leaderDef.successor == def;
        }

        private string GetStateLabel(HeroState st, Pawn pawn)
        {
            switch (st)
            {
                case HeroState.Unspawned:
                    return "TMM_FactionHierarchy_State_Unspawned".Translate();
                case HeroState.AtHome:
                    return "TMM_FactionHierarchy_State_AtHome".Translate();
                case HeroState.Away:
                    if (pawn != null && pawn.GetCaravan() != null)
                        return "TMM_FactionHierarchy_State_InCaravan".Translate();
                    if (pawn != null && pawn.Map != null)
                    {
                        string mName = pawn.Map.Parent != null ? pawn.Map.Parent.LabelCap.ToString() : "TMM_FactionHierarchy_Location_Map".Translate().ToString();
                        return "TMM_FactionHierarchy_State_OnMap".Translate(mName);
                    }
                    return "TMM_FactionHierarchy_State_Away".Translate();
                case HeroState.Captive:
                    string captor = pawn != null && pawn.Faction != null ? pawn.Faction.Name : "TMM_FactionHierarchy_Location_Unknown".Translate().ToString();
                    return "TMM_FactionHierarchy_State_Captive".Translate(captor);
                case HeroState.Dead:
                    return "TMM_FactionHierarchy_State_Dead".Translate();
                case HeroState.Joined:
                    return "TMM_FactionHierarchy_State_Joined".Translate();
                case HeroState.Missing:
                    return "TMM_FactionHierarchy_State_Missing".Translate();
                default:
                    return st.ToString();
            }
        }

        private Color GetStateColor(HeroState st)
        {
            switch (st)
            {
                case HeroState.Dead: return Color.red;
                case HeroState.Captive: return new Color(1f, 0.5f, 0.2f);
                case HeroState.Joined: return Color.green;
                case HeroState.AtHome: return Color.white;
                case HeroState.Away: return Color.yellow;
                default: return Color.gray;
            }
        }

        private string GetHeroTooltip(HeroDef def, Pawn pawn, HeroState st)
        {
            string s = def.label ?? def.defName;
            if (pawn != null && pawn.story != null)
            {
                if (pawn.story.Childhood != null)
                {
                    s += "\n\n" + pawn.story.Childhood.title + ":\n" + pawn.story.Childhood.baseDesc;
                }
                if (pawn.story.Adulthood != null)
                {
                    s += "\n\n" + pawn.story.Adulthood.title + ":\n" + pawn.story.Adulthood.baseDesc;
                }
            }
            else
            {
                if (def.childhood != null)
                {
                    s += "\n\n" + def.childhood.title + ":\n" + def.childhood.baseDesc;
                }
                if (def.adulthood != null)
                {
                    s += "\n\n" + def.adulthood.title + ":\n" + def.adulthood.baseDesc;
                }
            }
            return s;
        }

        private void DrawPawnPortrait(Rect rect, Pawn pawn)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.12f));
            if (pawn != null)
            {
                RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(rect.width, rect.height), Rot4.South, default(Vector3), 1.25f);
                if (portrait != null)
                {
                    if (pawn.Dead) GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    GUI.DrawTexture(rect, portrait);
                    GUI.color = Color.white;
                }
            }
            Widgets.DrawBox(rect);
        }

        private void DrawUnknownSilhouette(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f));
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            Widgets.Label(rect, "?");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            Widgets.DrawBox(rect);
        }
    }
}
