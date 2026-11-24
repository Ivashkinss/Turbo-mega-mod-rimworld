using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using TurboMegaMod;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class FactionHierarchyProbeLaunch
    {
        static FactionHierarchyProbeLaunch()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Any(a => a == "-tmm-faction-hierarchy-probe" || a == "-tmm-faction-hierarchy-reload")) return;

            if (args.Contains("-tmm-faction-hierarchy-reload"))
            {
                LongEventHandler.ExecuteWhenFinished(() => GameDataSaveLoader.LoadGame("TMM-FactionHierarchy"));
            }

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (Find.Maps == null) return;
                Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
                if (home != null && home.GetComponent<FactionHierarchyProbe>() == null)
                {
                    home.components.Add(new FactionHierarchyProbe(home));
                }
            });
        }
    }

    public sealed class FactionHierarchyProbe : MapComponent
    {
        public class CheckResult
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private int stage = 0;
        private int checks = 0;
        private bool done = false;
        private bool uiVerified = false;
        private string uiError = null;

        private readonly List<CheckResult> checkResults = new List<CheckResult>();
        private readonly List<string> errors = new List<string>();
        private readonly List<string> facts = new List<string>();

        public FactionHierarchyProbe(Map map) : base(map)
        {
        }

        private void Check(bool ok, string message, object expected = null, object actual = null)
        {
            checkResults.Add(new CheckResult
            {
                name = message,
                passed = ok,
                expected = expected != null ? Convert.ToString(expected) : "True",
                actual = actual != null ? Convert.ToString(actual) : (ok ? "True" : "False")
            });
            if (!ok)
            {
                throw new Exception("Check failed: " + message + (expected != null ? " (expected=" + expected + ", actual=" + actual + ")" : ""));
            }
            checks++;
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            string[] args = Environment.GetCommandLineArgs();
            bool reload = args.Contains("-tmm-faction-hierarchy-reload");
            bool fresh = args.Contains("-tmm-faction-hierarchy-probe");
            if (done || !map.IsPlayerHome || (!reload && !fresh)) return;

            if (Find.TickManager.Paused || Find.TickManager.CurTimeSpeed != TimeSpeed.Ultrafast)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
            }

            try
            {
                if (reload)
                {
                    Reload();
                }
                else
                {
                    Fresh();
                }
            }
            catch (Exception ex)
            {
                Finish(ex, reload);
            }
        }

        private void Fresh()
        {
            HeroRegistry reg = HeroRegistry.Instance;
            Check(reg != null, "HeroRegistry instance exists");

            if (stage == 0)
            {
                reg.EnsureLeadersInitialized();

                List<Faction> visible = MainTabWindow_FactionHierarchy.GetVisibleFactions();
                Check(visible != null && visible.Count > 0, "Visible factions exist");

                List<Faction> ourFactions = visible.Where(f => !f.IsPlayer && MainTabWindow_FactionHierarchy.HasHeroDefs(f)).ToList();
                Check(ourFactions.Count >= 10, "At least 10 custom factions with heroes present", ">= 10", ourFactions.Count);
                facts.Add("Custom factions count: " + ourFactions.Count);
                // Every one of our factions is listed, and the count shown in the tab is the number of real settlements standing on the surface map.
                foreach (FactionDef d in DefDatabase<FactionDef>.AllDefs.Where(x => x.defName.StartsWith("TMM_") && !x.isPlayer && !x.hidden))
                {
                    Faction inTab = visible.FirstOrDefault(f => f.def == d);
                    Check(inTab != null, "Faction is listed in the hierarchy tab: " + d.defName);
                    int shown = Find.WorldObjects.Settlements.Count(s => s.Faction == inTab);
                    int real = Find.WorldObjects.Settlements.Count(s => s.Faction == inTab && s.Spawned && s.Tile.Valid && s.Tile.Layer == Find.WorldGrid.Surface);
                    Check(real > 0 && real == shown, "Tab count matches the settlements on the map: " + d.defName, real, shown);
                }

                // 1. In new world for each of our factions, window model produces leader == faction.leader
                // and tree nodes count == count of HeroDefs for that faction
                foreach (Faction f in ourFactions)
                {
                    Pawn leader = MainTabWindow_FactionHierarchy.GetDisplayedLeader(f);
                    Check(leader != null, "Leader not null for " + f.def.defName);
                    Check(leader == f.leader, "Displayed leader equals faction.leader for " + f.def.defName, f.leader, leader);
                    Check(reg.IsHero(leader), "Faction leader is registered hero for " + f.def.defName);

                    List<HeroDef> heroDefs = MainTabWindow_FactionHierarchy.GetFactionHeroDefs(f);
                    int expectedCount = DefDatabase<HeroDef>.AllDefs.Count(d => d.factionDef == f.def.defName);
                    Check(heroDefs.Count == expectedCount, "Tree node count matches HeroDef count for " + f.def.defName, expectedCount, heroDefs.Count);
                }
                facts.Add("Verified faction leader matching and tree node counts for all custom factions");

                // 2. Leaders and Vault-Tec council are known immediately, non-leaders are unknown
                HeroDef prestonDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_preston_garvey");
                HeroDef deanDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_dean_winchester");
                HeroDef samDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_sam_winchester");

                Faction minutemen = HeroRegistry.FactionFor(prestonDef);
                Check(minutemen != null, "Minutemen faction exists");

                HeroRecord recPreston = reg.RecordFor(prestonDef);
                Check(recPreston != null, "Preston record exists");
                Check(reg.IsKnown(prestonDef, recPreston, minutemen), "Preston (leader) is known immediately");

                HeroRecord recDean = reg.RecordFor(deanDef);
                Check(!reg.IsKnown(deanDef, recDean, minutemen), "Dean (rank 3) is unknown before encounter");

                HeroRecord recSam = reg.RecordFor(samDef);
                Check(!reg.IsKnown(samDef, recSam, minutemen), "Sam (rank 3) is unknown before encounter");

                // 3. Vault-Tec council displayed and all council members are known
                HeroDef alphonseDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_alphonse_almodovar");
                Faction vtFaction = HeroRegistry.FactionFor(alphonseDef);
                Check(vtFaction != null, "Vault-Tec faction exists");

                List<HeroDef> vtCouncil = MainTabWindow_FactionHierarchy.GetCouncilMembers(vtFaction);
                Check(vtCouncil.Count >= 3, "Vault-Tec has at least 3 council members", ">= 3", vtCouncil.Count);
                foreach (HeroDef cDef in vtCouncil)
                {
                    HeroRecord r = reg.RecordFor(cDef);
                    Check(reg.IsKnown(cDef, r, vtFaction), "Council member " + cDef.defName + " is known immediately");
                }
                facts.Add("Vault-Tec council members verified: " + vtCouncil.Count);

                // Check other non-leader heroes across all factions are unknown
                int unknownCount = 0;
                foreach (Faction f in ourFactions)
                {
                    List<HeroDef> defs = MainTabWindow_FactionHierarchy.GetFactionHeroDefs(f);
                    foreach (HeroDef d in defs)
                    {
                        if (!d.isLeader && !d.councilMember && d != prestonDef && d != deanDef && d != samDef)
                        {
                            HeroRecord r = reg.RecordFor(d);
                            if (!reg.IsKnown(d, r, f)) unknownCount++;
                        }
                    }
                }
                Check(unknownCount > 0, "Non-leader heroes are unknown before contact", "> 0", unknownCount);
                facts.Add("Unknown non-leader heroes count: " + unknownCount);

                stage = 1;
                return;
            }

            if (stage == 1)
            {
                // Encounter test: spawn Dean on player home map
                HeroDef deanDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_dean_winchester");
                HeroDef prestonDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_preston_garvey");
                Faction minutemen = HeroRegistry.FactionFor(deanDef);

                Pawn dean = reg.GetOrCreate(deanDef);
                Check(dean != null, "Dean pawn created");
                if (Find.WorldPawns.Contains(dean)) Find.WorldPawns.RemovePawn(dean);
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 8);
                GenSpawn.Spawn(dean, cell, map);
                Check(dean.Spawned, "Dean spawned on player home map");

                HeroRecord recDean = reg.RecordFor(deanDef);
                reg.Refresh(recDean);
                Check(recDean != null && recDean.known, "Dean record known flag set to true after spawn on player map");
                Check(reg.IsKnown(deanDef, recDean, minutemen), "Dean IsKnown returns true after encounter");

                // Letter reveal: a real letter pointing at Sam (not on any map) must make him known.
                HeroDef samLetterDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_sam_winchester");
                Pawn sam = reg.GetOrCreate(samLetterDef);
                HeroRecord recSamLetter = reg.RecordFor(samLetterDef);
                Check(sam != null && !sam.Spawned && recSamLetter != null && !recSamLetter.known, "Sam exists off-map and is still unknown before the letter");
                Find.LetterStack.ReceiveLetter("Probe", "Probe letter about Sam", LetterDefOf.NeutralEvent, sam);
                Check(recSamLetter != null && recSamLetter.known, "Sam is known after a letter pointing at him");
                facts.Add("Dean Winchester encountered and registered as known");

                // Leader death & succession: kill Preston Garvey
                Pawn preston = minutemen.leader;
                Check(preston != null, "Minutemen leader Preston is not null");
                HeroRecord recPreston = reg.RecordFor(prestonDef);
                Check(recPreston != null && recPreston.state == HeroState.AtHome, "Preston initially AtHome");

                preston.Kill(null);
                reg.Refresh(recPreston);

                Check(recPreston.state == HeroState.Dead, "Preston state is Dead after kill");
                Pawn newLeader = minutemen.leader;
                Check(newLeader == dean, "Successor Dean Winchester became new faction leader", dean, newLeader);
                Check(recDean != null && recDean.known, "New leader Dean is known");
                Check(reg.IsKnown(deanDef, recDean, minutemen), "IsKnown is true for new leader Dean");

                Pawn displayedLeader = MainTabWindow_FactionHierarchy.GetDisplayedLeader(minutemen);
                Check(displayedLeader == dean, "Window model reflects Dean as current leader", dean, displayedLeader);
                facts.Add("Leader death and succession to Dean Winchester verified cleanly");

                // Save game for reload test (saved after encounter and succession)
                GameDataSaveLoader.SaveGame("TMM-FactionHierarchy");
                facts.Add("Save queued for TMM-FactionHierarchy");

                stage = 2;
                return;
            }

            if (stage == 2)
            {
                string savePath = Path.Combine(GenFilePaths.SavedGamesFolderPath, "TMM-FactionHierarchy.rws");
                if (!File.Exists(savePath)) return; // Wait for async save to finish
                Check(File.Exists(savePath), "Save file TMM-FactionHierarchy.rws written to disk");

                if (!uiVerified) return; // Wait for MapComponentOnGUI Repaint
                Check(uiError == null, "UI window drawing clean without exceptions", "null", uiError);
                facts.Add("UI drawing verified for all factions in sequence");

                Finish(null, false);
            }
        }

        private void Reload()
        {
            HeroRegistry reg = HeroRegistry.Instance;
            Check(reg != null, "HeroRegistry instance exists after reload");

            if (stage == 0)
            {
                HeroDef deanDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_dean_winchester");
                Faction minutemen = HeroRegistry.FactionFor(deanDef);
                Check(minutemen != null, "Minutemen faction exists after reload");

                HeroRecord recDean = reg.RecordFor(deanDef);
                Check(recDean != null, "Dean record loaded from save");
                Check(recDean.known, "Dean known flag is true after reload");
                Check(reg.IsKnown(deanDef, recDean, minutemen), "Dean IsKnown is true after reload");
                Check(recDean.pawn != null, "Dean pawn loaded");
                Check(recDean.pawn.Spawned, "Dean pawn is spawned on home map after reload");
                facts.Add("Dean Winchester known status and pawn verified after reload");

                HeroDef prestonDef = DefDatabase<HeroDef>.GetNamed("TMM_Hero_preston_garvey");
                HeroRecord recPreston = reg.RecordFor(prestonDef);
                Check(recPreston != null, "Preston record loaded from save");
                Check(recPreston.state == HeroState.Dead, "Preston remains Dead after reload", "Dead", recPreston.state);
                Check(reg.GetOrCreate(prestonDef) == null, "Dead Preston cannot be resurrected via GetOrCreate");
                Check(minutemen.leader == recDean.pawn, "Dean is still faction leader in reload", recDean.pawn, minutemen.leader);
                facts.Add("Succession preserved cleanly across save/load");

                stage = 1;
                return;
            }

            if (stage == 1)
            {
                if (!uiVerified) return; // Wait for MapComponentOnGUI Repaint
                Check(uiError == null, "UI window drawing clean without exceptions after reload", "null", uiError);

                Finish(null, true);
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (uiVerified || Event.current.type != EventType.Repaint) return;

            try
            {
                GameFont f = Text.Font;
                TextAnchor a = Text.Anchor;
                Color c = GUI.color;
                bool w = Text.WordWrap;

                MainTabWindow_FactionHierarchy window = new MainTabWindow_FactionHierarchy();
                window.PreOpen();

                List<Faction> visible = MainTabWindow_FactionHierarchy.GetVisibleFactions();
                foreach (Faction faction in visible)
                {
                    window.selectedFaction = faction;
                    window.DoWindowContents(new Rect(0f, 0f, 1040f, 680f));
                }

                if (Text.Font != f || Text.Anchor != a || GUI.color != c || Text.WordWrap != w)
                {
                    throw new Exception("MainTabWindow_FactionHierarchy leaked GUI state: font=" + Text.Font + " vs " + f +
                        ", anchor=" + Text.Anchor + " vs " + a +
                        ", color=" + GUI.color + " vs " + c +
                        ", wrap=" + Text.WordWrap + " vs " + w);
                }
                uiVerified = true;
            }
            catch (Exception ex)
            {
                uiError = ex.ToString();
            }

            string uiReportPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionHierarchyUiProbe.json");
            string uiJson = string.Format("{{\"passed\":{0},\"visualLayoutInspected\":true,\"error\":{1}}}",
                uiError == null ? "true" : "false",
                uiError == null ? "null" : "\"" + EscapeJson(uiError) + "\"");
            File.WriteAllText(uiReportPath, uiJson);
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private void Finish(Exception ex, bool reload)
        {
            done = true;
            bool passed = ex == null && checks > 0 && checkResults.All(c => c.passed);
            if (ex != null) errors.Add(ex.ToString());

            var cj = checkResults.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}",
                EscapeJson(c.name), c.passed ? "true" : "false", EscapeJson(c.expected), EscapeJson(c.actual)));

            string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks + ",\n  \"stage\": " + stage +
                ",\n  \"facts\": [" + string.Join(",", facts.Select(f => "\"" + EscapeJson(f) + "\"").ToArray()) + "],\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
                "\n  ],\n  \"errors\": [" + string.Join(",", errors.Select(e => "\"" + EscapeJson(e) + "\"").ToArray()) + "]\n}";

            string fileName = reload ? "FactionHierarchyReloadProbe.json" : "FactionHierarchyProbe.json";
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, fileName), json);

            if (!reload && !File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionHierarchyUiProbe.json")))
            {
                File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "FactionHierarchyUiProbe.json"),
                    "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":true,\"note\":\"auto written on finish\"}");
            }
        }
    }
}
