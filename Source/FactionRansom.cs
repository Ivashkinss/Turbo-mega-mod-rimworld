using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    public enum FactionRansomState
    {
        None,
        Refused,
        PaidPendingRelease,
        DeceivedHeld,
        Released
    }

    public sealed class FactionRansomEntry : IExposable, IThingHolder
    {
        public Pawn pawn;
        public Faction originalFaction;
        public int targetAmount;
        public int placedAmount;
        public bool offered;
        public bool agreed;
        public bool deceived;
        public int deceptionTick;
        public bool released;
        public int releaseTick;
        public IntVec3 dropCell = IntVec3.Invalid;
        public Map map;
        private ThingOwner<Thing> escrow;

        public FactionRansomEntry()
        {
            escrow = new ThingOwner<Thing>(this);
        }

        public IThingHolder ParentHolder { get { return null; } }
        public ThingOwner GetDirectlyHeldThings() { return escrow; }
        public void GetChildHolders(List<IThingHolder> holders) { ThingOwnerUtility.AppendThingHoldersFromThings(holders, escrow); }

        public int EscrowCoins { get { return escrow == null ? 0 : escrow.Sum(t => t.stackCount); } }
        public int TotalFundedCoins { get { return placedAmount + EscrowCoins; } }
        public bool IsFullyPaid { get { return agreed && EscrowCoins == 0 && placedAmount >= targetAmount; } }
        public bool CanDeceive { get { return IsFullyPaid && !deceived && !released; } }
        public bool CanRelease { get { return IsFullyPaid && !released; } }

        public FactionRansomState State
        {
            get
            {
                if (!offered) return FactionRansomState.None;
                if (!agreed) return FactionRansomState.Refused;
                if (released) return FactionRansomState.Released;
                if (deceived) return FactionRansomState.DeceivedHeld;
                return FactionRansomState.PaidPendingRelease;
            }
        }

        public int ProcessEscrow(Map targetMap, Building_ComplexDesk desk)
        {
            if (escrow == null || escrow.Count == 0) return 0;
            // Escrow is strictly bound to the map of the transaction
            Map m = map;
            if (m == null || targetMap != m) return 0;

            IntVec3 cell = (desk != null && desk.Spawned && desk.Map == m) ? desk.Position : (dropCell.IsValid ? dropCell : IntVec3.Invalid);
            if (!cell.IsValid || !cell.InBounds(m)) return 0;

            int totalPlaced = 0;
            foreach (var silver in escrow.ToArray())
            {
                int before = escrow.Sum(t => t.stackCount);
                Thing dropped;
                bool ok = escrow.TryDrop(silver, cell, m, ThingPlaceMode.Near, silver.stackCount, out dropped);
                int placed = before - escrow.Sum(t => t.stackCount);
                if (placed > 0)
                {
                    placedAmount += placed;
                    totalPlaced += placed;
                }
            }
            return totalPlaced;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref originalFaction, "originalFaction");
            Scribe_Values.Look(ref targetAmount, "targetAmount");
            Scribe_Values.Look(ref placedAmount, "placedAmount");
            Scribe_Values.Look(ref offered, "offered");
            Scribe_Values.Look(ref agreed, "agreed");
            Scribe_Values.Look(ref deceived, "deceived");
            Scribe_Values.Look(ref deceptionTick, "deceptionTick");
            Scribe_Values.Look(ref released, "released");
            Scribe_Values.Look(ref releaseTick, "releaseTick");
            Scribe_Values.Look(ref dropCell, "dropCell");
            Scribe_References.Look(ref map, "map");
            Scribe_Deep.Look(ref escrow, "escrow", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (escrow == null) escrow = new ThingOwner<Thing>(this);
            }
        }
    }

    public sealed class FactionRansoms : GameComponent
    {
        public List<FactionRansomEntry> entries = new List<FactionRansomEntry>();
        public int totalRansomIncome;

        public FactionRansoms(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref totalRansomIncome, "TMM_totalRansomIncome", 0);
            Scribe_Collections.Look(ref entries, "TMM_factionRansoms", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (entries == null) entries = new List<FactionRansomEntry>();
                entries.RemoveAll(e => e == null);
            }
        }

        public void RecordIncome(int amount)
        {
            if (amount > 0) totalRansomIncome += amount;
        }

        public void ProcessPendingEscrow(Map m)
        {
            if (m == null) return;
            var desk = ComplexLayout.Desk(m);
            foreach (var entry in entries.ToArray())
            {
                // Strict map binding: escrow only drops on the exact map of transaction
                if (entry != null && entry.agreed && entry.EscrowCoins > 0 && entry.map == m)
                {
                    int placed = entry.ProcessEscrow(m, desk);
                    if (placed > 0) RecordIncome(placed);
                }
            }
        }

        public FactionRansomEntry Entry(Pawn pawn)
        {
            return entries.FirstOrDefault(e => e != null && e.pawn == pawn);
        }

        public string Status(Pawn pawn)
        {
            var entry = Entry(pawn);
            if (entry == null || !entry.offered) return null;
            if (!entry.agreed) return "TMM.Ransom.StatusRefused".Translate(entry.targetAmount);
            if (entry.released)
            {
                return (entry.deceived ? "TMM.Ransom.StatusReleasedDeceived" : "TMM.Ransom.StatusReleased").Translate(entry.targetAmount);
            }
            if (entry.deceived)
            {
                return "TMM.Ransom.StatusDeceived".Translate(entry.targetAmount);
            }
            if (entry.EscrowCoins > 0)
            {
                return "TMM.Ransom.StatusEscrowPending".Translate(entry.targetAmount, entry.placedAmount);
            }
            return "TMM.Ransom.StatusPaidPending".Translate(entry.targetAmount);
        }

        public static bool WillFactionAccept(Pawn pawn)
        {
            if (pawn == null || Find.World == null || Find.World.info == null) return false;
            int seed = GenText.StableStringHash("TMM:faction-ransom:" + Find.World.info.seedString + ":" + pawn.GetUniqueLoadID());
            Rand.PushState(seed);
            try { return Rand.Chance(0.5f); }
            finally { Rand.PopState(); }
        }

        public static bool HasAgreedRansom(Pawn pawn)
        {
            if (pawn == null || Current.Game == null) return false;
            var comp = Current.Game.GetComponent<FactionRansoms>();
            var entry = comp == null ? null : comp.Entry(pawn);
            return entry != null && entry.agreed;
        }

        public static FactionRansomEntry Offer(Pawn pawn, int target)
        {
            if (pawn == null) return null;
            var ransoms = Current.Game.GetComponent<FactionRansoms>();
            var rEntry = ransoms == null ? null : ransoms.Entry(pawn);

            // If already offered once, terms and decision are immutable; return without re-paying or re-checking
            if (rEntry != null && rEntry.offered) return rEntry;

            // Strict context validation: must be spawned colony slave on map with active desk
            if (!pawn.Spawned || pawn.Map == null || !pawn.IsSlaveOfColony || target < 1 || target > 1000000) return null;
            if (pawn.Dead || pawn.Downed || pawn.InMentalState) return null;
            if (pawn.BillStack != null && pawn.BillStack.Bills.Any())
            {
                Messages.Message("TMM.Contract.Unavailable".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            var desk = ComplexLayout.Desk(pawn.Map);
            if (desk == null || !desk.Active)
            {
                Messages.Message("TMM.Contract.Unavailable".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            var home = pawn.HomeFaction;
            if (home == null || home.IsPlayer || home == Faction.OfPlayer || home.defeated)
            {
                Messages.Message("TMM.Ransom.NoValidFaction".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            // Mutual exclusivity with free labour contract or permanent membership
            var buyouts = Current.Game.GetComponent<WorkBuyouts>();
            var bEntry = buyouts == null ? null : buyouts.Entry(pawn);
            if (bEntry != null && (bEntry.freeContract || bEntry.joinedPermanently || bEntry.released))
            {
                Messages.Message("TMM.Ransom.ContractExists".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            if(bEntry!=null)target=bEntry.target; // Preserve the already agreed legacy obligation.
            Map m = pawn.Map;
            if (rEntry == null)
            {
                rEntry = new FactionRansomEntry
                {
                    pawn = pawn,
                    originalFaction = home,
                    targetAmount = target,
                    map = m,
                    dropCell = desk.Position
                };
                ransoms.entries.Add(rEntry);
            }

            rEntry.offered = true;
            rEntry.agreed = WillFactionAccept(pawn);

            if (rEntry.agreed)
            {
                // Freeze legacy buyout obligation if present (without erasing target, credited, or receipts)
                if (bEntry != null)
                {
                    bEntry.active = false;
                }

                // External faction transfer into escrow
                for (int remaining = target; remaining > 0; )
                {
                    var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                    silver.stackCount = Math.Min(remaining, ThingDefOf.Silver.stackLimit);
                    rEntry.GetDirectlyHeldThings().TryAdd(silver, false);
                    remaining -= silver.stackCount;
                }

                int placed = rEntry.ProcessEscrow(m, desk);
                if (placed > 0)
                {
                    ransoms.RecordIncome(placed);
                }

                Messages.Message("TMM.Ransom.Accepted".Translate(pawn.LabelShortCap, home.Name, target), pawn, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("TMM.Ransom.Refused".Translate(pawn.LabelShortCap, home.Name), pawn, MessageTypeDefOf.NeutralEvent, false);
            }

            return rEntry;
        }

        public static bool Deceive(Pawn pawn)
        {
            if (pawn == null) return false;
            var ransoms = Current.Game.GetComponent<FactionRansoms>();
            var entry = ransoms == null ? null : ransoms.Entry(pawn);
            if (entry == null || !entry.agreed || entry.released || entry.deceived) return false;
            if (!entry.IsFullyPaid) return false;

            // Strict alive, spawned, and colony slave control checks
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.InMentalState || !pawn.IsSlaveOfColony) return false;

            entry.deceived = true;
            entry.deceptionTick = Find.TickManager.TicksGame;

            Messages.Message("TMM.Ransom.DeceivedWarning".Translate(pawn.LabelShortCap, entry.originalFaction != null ? entry.originalFaction.Name : "Faction"), pawn, MessageTypeDefOf.CautionInput, false);
            return true;
        }

        public static bool Release(Pawn pawn)
        {
            if (pawn == null) return false;
            var ransoms = Current.Game.GetComponent<FactionRansoms>();
            var entry = ransoms == null ? null : ransoms.Entry(pawn);
            if (entry == null || !entry.agreed || entry.released) return false;
            if (!entry.IsFullyPaid) return false;

            // Strict alive, spawned, and colony slave control checks
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.InMentalState || !pawn.IsSlaveOfColony) return false;
            if (pawn.BillStack != null && pawn.BillStack.Bills.Any()) return false;

            IntVec3 exit;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out exit, TraverseMode.ByPawn, false)) return false;

            if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
            {
                Thing dropped;
                if (!pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out dropped)) return false;
            }

            Detach(pawn);
            pawn.jobs.StopAll();
            pawn.guest.SetGuestStatus(null, GuestStatus.Guest);
            if (entry.originalFaction != null && pawn.Faction != entry.originalFaction)
            {
                pawn.SetFaction(entry.originalFaction);
            }
            AccessTools.Method(typeof(GenGuest), "GuestRelease").Invoke(null, new object[] { pawn });

            entry.released = true;
            entry.releaseTick = Find.TickManager.TicksGame;
            return true;
        }

        internal static void Detach(Pawn pawn)
        {
            if (pawn == null) return;
            foreach (var map in Find.Maps)
            {
                var comp = map.GetComponent<ComplexMap>();
                var desk = comp == null ? null : comp.desk;
                if (desk == null) continue;
                var employee = desk.Employee(pawn);
                if (employee == null) continue;
                var service = map.GetComponent<CompanyService>();
                if (service != null) service.Cancel(pawn);
                desk.SetCompany(pawn, false);
                desk.SetRole(pawn, ComplexRole.None);
                desk.SetResidence(pawn, false);
                desk.employees.Remove(employee);
            }
        }
    }

    public sealed class FactionRansomLedger : MapComponent
    {
        private int nextCheck;

        public FactionRansomLedger(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager.TicksGame < nextCheck) return;
            nextCheck = Find.TickManager.TicksGame + 250;
            var ransoms = Current.Game.GetComponent<FactionRansoms>();
            if (ransoms != null) ransoms.ProcessPendingEscrow(map);
        }
    }
}
