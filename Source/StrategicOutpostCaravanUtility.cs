using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public static class StrategicOutpostCaravanUtility
    {
        public static bool IsSuitableColonist(Pawn p)
        {
            if (p == null || p.Dead || p.Downed) return false;
            if (!p.IsColonist || p.IsPrisoner || p.IsSlave) return false;
            if (p.health == null || !p.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) return false;
            return true;
        }

        public static StrategicOutpost GetOutpostAtTile(int tile)
        {
            if (tile < 0 || Find.WorldObjects == null) return null;
            List<WorldObject> objs = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < objs.Count; i++)
            {
                if (objs[i].Tile == tile && objs[i] is StrategicOutpost)
                {
                    return objs[i] as StrategicOutpost;
                }
            }
            return null;
        }

        public static bool CanCreateOutpostAt(int tile, out string reason)
        {
            if (tile < 0)
            {
                reason = "StrategicOutpost_InvalidTile".Translate();
                return false;
            }

            if (Find.WorldGrid == null || tile >= Find.WorldGrid.TilesCount)
            {
                reason = "StrategicOutpost_InvalidTile".Translate();
                return false;
            }

            PlanetTile pt = tile;
            if (pt.Layer != Find.WorldGrid.Surface)
            {
                reason = "StrategicOutpost_InvalidTile".Translate();
                return false;
            }

            if (Find.World.Impassable(tile))
            {
                reason = "StrategicOutpost_ImpassableTile".Translate();
                return false;
            }

            StrategicOutpost existing = GetOutpostAtTile(tile);
            if (existing != null)
            {
                reason = "StrategicOutpost_AlreadyExistsHere".Translate();
                return false;
            }

            Settlement settlement = Find.WorldObjects.SettlementAt(tile);
            if (settlement != null)
            {
                reason = "StrategicOutpost_CannotBuildOnSettlement".Translate();
                return false;
            }

            Site site = Find.WorldObjects.SiteAt(tile);
            if (site != null)
            {
                reason = "StrategicOutpost_CannotBuildOnSite".Translate();
                return false;
            }

            reason = null;
            return true;
        }

        public static bool CanCaravanFoundOutpost(Caravan caravan, out string reason)
        {
            if (caravan == null || !caravan.Spawned || caravan.Faction != Faction.OfPlayer || caravan.Tile.Layer != Find.WorldGrid.Surface)
            {
                reason = "StrategicOutpost_NoPlayerCaravan".Translate();
                return false;
            }

            if (caravan.pather != null && caravan.pather.Moving)
            {
                reason = "StrategicOutpost_CaravanIsMoving".Translate();
                return false;
            }

            if (!CanCreateOutpostAt(caravan.Tile, out reason))
            {
                return false;
            }

            int colonistCount = caravan.PawnsListForReading.Count(IsSuitableColonist);
            if (colonistCount < 2)
            {
                reason = "StrategicOutpost_RequiresTwoColonists".Translate(colonistCount);
                return false;
            }

            var founders = caravan.PawnsListForReading.Where(IsSuitableColonist).Take(2).ToList();
            var remaining = caravan.PawnsListForReading.Where(p => !founders.Contains(p)).ToList();
            if (!remaining.Any(IsSuitableColonist) && remaining.Any(p => !p.RaceProps.Animal || p.Faction != Faction.OfPlayer || p.Dead || p.Downed))
            {
                reason = "StrategicOutpost_AdditionalEscort".Translate();
                return false;
            }

            reason = null;
            return true;
        }

        public static StrategicOutpost FoundOutpostFromCaravan(Caravan caravan, StrategicOutpostSpecialization spec)
        {
            string failReason;
            if (!CanCaravanFoundOutpost(caravan, out failReason))
            {
                Messages.Message(failReason, caravan, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            List<Pawn> allPawns = caravan.PawnsListForReading.ToList();
            List<Pawn> selectedColonists = allPawns.Where(IsSuitableColonist).Take(2).ToList();

            StrategicOutpostDef outpostDef = StrategicOutpostDefOf.TMM_StrategicOutpost;
            if (outpostDef == null)
            {
                outpostDef = DefDatabase<StrategicOutpostDef>.GetNamedSilentFail("TMM_StrategicOutpost");
            }

            if (outpostDef == null)
            {
                Messages.Message("Critical: TMM_StrategicOutpost def missing from database.", MessageTypeDefOf.RejectInput, false);
                return null;
            }

            StrategicOutpost outpost = (StrategicOutpost)WorldObjectMaker.MakeWorldObject(outpostDef);
            outpost.Tile = caravan.Tile;
            outpost.SetFaction(Faction.OfPlayer);
            outpost.level = 1;
            outpost.specialization = spec;

            Settlement homeColony = Find.WorldObjects.Settlements.FirstOrDefault(s => s != null && s.Faction == Faction.OfPlayer && s.Spawned && s.Tile.Layer == Find.WorldGrid.Surface);
            if (homeColony != null)
            {
                outpost.targetColonyTile = homeColony.Tile;
            }

            Find.WorldObjects.Add(outpost);

            var addedPawns = new List<Pawn>();
            var remaining = allPawns.Where(p => !selectedColonists.Contains(p)).ToList();
            var animals = remaining.Any(IsSuitableColonist) ? new List<Pawn>() : remaining;

            foreach (var pawn in selectedColonists.Concat(animals))
            {
                string error;
                if (outpost.AddPawnFromCaravan(caravan, pawn, out error))
                {
                    addedPawns.Add(pawn);
                    continue;
                }
                foreach (var transferred in addedPawns)
                {
                    transferred.holdingOwner.Remove(transferred);
                    caravan.AddPawn(transferred, false);
                    if (caravan.ContainsPawn(transferred)) StrategicOutpost.SetStoredPawnProtection(transferred, false);
                }
                Find.WorldObjects.Remove(outpost);
                Messages.Message("StrategicOutpost_FoundingRollback".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            // Transfer physical carried inventory into outpost supply depot storage
            foreach (var pawn in addedPawns)
            {
                if (pawn.inventory != null)
                {
                    foreach (var item in pawn.inventory.innerContainer.InnerListForReading.ToArray())
                    {
                        pawn.inventory.innerContainer.TryTransferToContainer(item, outpost.RawMaterials, item.stackCount, false);
                    }
                }
            }

            if (caravan.PawnsListForReading.Count == 0)
            {
                Find.WorldObjects.Remove(caravan);
            }

            Messages.Message("StrategicOutpost_FoundingSuccess".Translate(outpost.LabelCap, addedPawns.Count), outpost, MessageTypeDefOf.PositiveEvent);
            return outpost;
        }

        public static List<FloatMenuOption> GetSpecializationOptions(Caravan caravan)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            var specs = new[]
            {
                StrategicOutpostSpecialization.Mining,
                StrategicOutpostSpecialization.Farming,
                StrategicOutpostSpecialization.Watchtower,
                StrategicOutpostSpecialization.Factory
            };

            foreach (StrategicOutpostSpecialization spec in specs)
            {
                StrategicOutpostSpecialization localSpec = spec;
                string labelKey = "StrategicOutpost_Spec_" + localSpec.ToString();
                options.Add(new FloatMenuOption(labelKey.Translate(), delegate
                {
                    FoundOutpostFromCaravan(caravan, localSpec);
                }));
            }
            return options;
        }

        public static IEnumerable<Gizmo> GetCaravanCommands(Caravan caravan)
        {
            if (caravan == null || caravan.Faction != Faction.OfPlayer) yield break;

            StrategicOutpost existingOutpost = GetOutpostAtTile(caravan.Tile);

            if (existingOutpost != null)
            {
                Command_Action manageCmd = new Command_Action();
                manageCmd.defaultLabel = "StrategicOutpost_ManageOutpost_Label".Translate();
                manageCmd.defaultDesc = "StrategicOutpost_ManageOutpost_Desc".Translate();
                manageCmd.icon = ContentFinder<Texture2D>.Get("UI/Commands/ManageOutpost", false) ?? TexCommand.ClearPrioritizedWork;
                manageCmd.action = delegate
                {
                    Find.WindowStack.Add(new Dialog_StrategicOutpost(existingOutpost, caravan));
                };
                yield return manageCmd;

                Command_Action enterCmd = new Command_Action();
                enterCmd.defaultLabel = "StrategicOutpost_EnterMap_Label".Translate();
                enterCmd.defaultDesc = "StrategicOutpost_EnterMap_Desc".Translate();
                enterCmd.icon = ContentFinder<Texture2D>.Get("UI/Commands/EnterOutpost", false) ?? TexCommand.ClearPrioritizedWork;
                enterCmd.action = delegate
                {
                    Map map = existingOutpost.GetOrGenerateMap();
                    CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, false, null);
                    CameraJumper.TryJump(map.Center, map);
                };
                yield return enterCmd;
            }
            else
            {
                string cantReason;
                bool canFound = CanCaravanFoundOutpost(caravan, out cantReason);

                Command_Action foundCmd = new Command_Action();
                foundCmd.defaultLabel = "StrategicOutpost_FoundOutpost_Label".Translate();
                foundCmd.defaultDesc = "StrategicOutpost_FoundOutpost_Desc".Translate();
                foundCmd.icon = ContentFinder<Texture2D>.Get("UI/Commands/FoundOutpost", false) ?? TexCommand.ClearPrioritizedWork;
                foundCmd.action = delegate
                {
                    if (canFound)
                    {
                        List<FloatMenuOption> opts = GetSpecializationOptions(caravan);
                        Find.WindowStack.Add(new FloatMenu(opts));
                    }
                    else
                    {
                        Messages.Message(cantReason, caravan, MessageTypeDefOf.RejectInput, false);
                    }
                };

                if (!canFound)
                {
                    foundCmd.Disable(cantReason);
                }
                yield return foundCmd;
            }
        }
    }
}
