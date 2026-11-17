using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class ServiceReadinessData
    {
        public int gatheredTick;
        public bool deskActive;
        public string areaName;
        public int areaCells;
        public string staffAreaName;
        public bool automaticServices;

        // Hotel
        public bool hotelModuleEnabled;
        public bool guestsWelcome;
        public bool refuseUntilBeds;
        public int roomRate;
        public int guestBedsTotal;
        public int guestSlotsTotal;
        public int guestSlotsOccupied;
        public int guestSlotsAvailable;
        public int guestBedsFullyFree;
        public int staffBedsCount;
        public int hotelStaysCount;
        public string nextVisitString;

        // Clinic
        public bool clinicModuleEnabled;
        public bool surgeryAllowed;
        public bool clinicIsOpen;
        public bool clinicAdmissionEnabled;
        public bool clinicScheduleHourAllowed;
        public int clinicOpenHoursCount;
        public int clinicBedsTotal;
        public int clinicSlotsTotal;
        public int clinicSlotsOccupied;
        public int clinicSlotsAvailable;
        public int doctorsReady;

        // Restaurant
        public bool restaurantOpen;
        public int restaurantPercent;
        public int counterCount;
        public int menuCount;
        public int fridgesCount;
        public int mealsInStockTotal;
        public int waitersReady;

        // Sales
        public int storesTotal;
        public int storesPublic;
        public int storesStaffOnly;
        public int tradeableStacksTotal;
        public int tradeableUnitsTotal;
        public int sellersReady;

        // Staff
        public int totalEmployees;
        public int residentEmployees;
        public int companyEmployees;
        public int cleanersReady;
        public int hostsReady;
        public int cooksReady;

        private static ComplexWorkMode Mode(Building_ComplexDesk desk, Pawn pawn)
        {
            var employee = desk.employees == null ? null : desk.employees.FirstOrDefault(e => e != null && e.pawn == pawn);
            return employee == null ? ComplexWorkMode.Both : employee.mode;
        }
        private static bool StaffBed(Building_ComplexDesk desk, Building_Bed bed)
        {
            return ComplexLayout.StaffOnly(desk, bed.Position) || (desk.employees != null && desk.employees.Any(e => e != null && e.resident && e.managedBed == bed));
        }
        public static ServiceReadinessData Gather(Building_ComplexDesk desk)
        {
            var data = new ServiceReadinessData();
            if (desk == null || !desk.Spawned || desk.Map == null) return data;

            var map = desk.Map;
            data.gatheredTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            data.deskActive = desk.Active;
            data.areaName = desk.area != null ? desk.area.Label : null;
            data.areaCells = desk.area != null ? desk.area.TrueCount : 0;
            data.staffAreaName = desk.staffArea != null ? desk.staffArea.Label : null;
            data.automaticServices = desk.automaticServices;

            // 1. Hotel
            data.hotelModuleEnabled = HotelBillingHooks.Enabled;
            data.roomRate = desk.roomRate;

            var bedsInArea = map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>()
                .Where(b => b.Spawned && b.Map == map && desk.area != null && desk.area[b.Position] &&
                            b.def != null && b.def.building != null && b.def.building.bed_humanlike &&
                            !b.ForPrisoners && !b.Medical)
                .ToList();

            data.staffBedsCount = bedsInArea.Count(b => StaffBed(desk, b));

            // Count actual guest beds; auto-conversion may not have run yet.
            var guestBeds = bedsInArea.Where(b =>
                !StaffBed(desk, b) &&
                ComplexLayout.IsGuest(b)
            ).ToList();

            data.guestBedsTotal = guestBeds.Count;
            data.guestSlotsTotal = guestBeds.Sum(b => b.SleepingSlotsCount);

            int occupiedSum = 0;
            int freeBedsCount = 0;
            foreach (var bed in guestBeds)
            {
                var heldPawns = new HashSet<Pawn>();
                if (bed.CurOccupants != null)
                {
                    foreach (var p in bed.CurOccupants) if (p != null) heldPawns.Add(p);
                }
                if (bed.OwnersForReading != null)
                {
                    foreach (var p in bed.OwnersForReading) if (p != null) heldPawns.Add(p);
                }
                int bedOccupied = Math.Min(bed.SleepingSlotsCount, heldPawns.Count);
                occupiedSum += bedOccupied;
                if (bedOccupied == 0) freeBedsCount++;
            }
            data.guestSlotsOccupied = occupiedSum;
            data.guestSlotsAvailable = Math.Max(0, data.guestSlotsTotal - data.guestSlotsOccupied);
            data.guestBedsFullyFree = freeBedsCount;

            var hotelBilling = map.GetComponent<HotelBilling>();
            data.hotelStaysCount = hotelBilling != null && hotelBilling.stays != null ? hotelBilling.stays.Count : 0;

            var hospComp = map.components.FirstOrDefault(c => c.GetType().FullName == "Hospitality.Hospitality_MapComponent");
            if (hospComp != null)
            {
                var fWelcome = AccessTools.Field(hospComp.GetType(), "guestsAreWelcome");
                if (fWelcome != null) data.guestsWelcome = (bool)fWelcome.GetValue(hospComp);
                var fRefuse = AccessTools.Field(hospComp.GetType(), "refuseGuestsUntilWeHaveBeds");
                if (fRefuse != null) data.refuseUntilBeds = (bool)fRefuse.GetValue(hospComp);

                int next = int.MaxValue;
                var lookup = AccessTools.Method(hospComp.GetType(), "GetNextVisit");
                if (lookup != null && Find.FactionManager != null)
                {
                    foreach (var faction in Find.FactionManager.AllFactionsListForReading)
                    {
                        var visit = lookup.Invoke(hospComp, new object[] { faction });
                        if (visit == null) continue;
                        var fireTick = AccessTools.Property(visit.GetType(), "FireTick");
                        if (fireTick == null) continue;
                        int due = (int)fireTick.GetValue(visit, null);
                        next = Math.Min(next, due);
                    }
                }
                if (next != int.MaxValue && Find.TickManager != null)
                {
                    float hours = Math.Max(0, next - Find.TickManager.TicksGame) / 2500f;
                    data.nextVisitString = "TMM.ServiceReadiness.NextVisitHours".Translate(hours.ToString("F1")).ToString();
                }
                else
                {
                    data.nextVisitString = "TMM.ServiceReadiness.NoPlannedVisits".Translate().ToString();
                }
            }

            // 2. Clinic
            data.clinicModuleEnabled = Clinic.Enabled;
            data.surgeryAllowed = desk.surgeryAllowed;
            var hospital = Clinic.Hospital(map);
            if (hospital != null)
            {
                var admission = AccessTools.Field(hospital.GetType(), "openForBusiness");
                data.clinicAdmissionEnabled = admission != null && (bool)admission.GetValue(hospital);
                var surgery = AccessTools.Field(hospital.GetType(), "AcceptSurgery");
                data.surgeryAllowed = data.surgeryAllowed && surgery != null && (bool)surgery.GetValue(hospital);

                var mIsOpen = AccessTools.Method(hospital.GetType(), "IsOpen");
                if (mIsOpen != null)
                {
                    data.clinicIsOpen = (bool)mIsOpen.Invoke(hospital, null);
                }

                var fHours = AccessTools.Field(hospital.GetType(), "openingHours");
                var hours = fHours != null ? fHours.GetValue(hospital) as List<bool> : null;
                if (hours != null)
                {
                    data.clinicOpenHoursCount = hours.Count(h => h);
                    int currentHour = GenLocalDate.HourOfDay(map);
                    if (currentHour >= 0 && currentHour < hours.Count) data.clinicScheduleHourAllowed = hours[currentHour];
                }
            }

            var wardBeds = map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>()
                .Where(Clinic.WardBed)
                .ToList();
            data.clinicBedsTotal = wardBeds.Count;
            data.clinicSlotsTotal = wardBeds.Sum(b => b.SleepingSlotsCount);

            int clinicOccupiedSum = 0;
            foreach (var b in wardBeds)
            {
                var clinicHeld = new HashSet<Pawn>();
                if (b.CurOccupants != null)
                {
                    foreach (var p in b.CurOccupants) if (p != null) clinicHeld.Add(p);
                }
                if (b.OwnersForReading != null)
                {
                    foreach (var p in b.OwnersForReading) if (p != null) clinicHeld.Add(p);
                }
                clinicOccupiedSum += Math.Min(b.SleepingSlotsCount, clinicHeld.Count);
            }
            data.clinicSlotsOccupied = clinicOccupiedSum;
            data.clinicSlotsAvailable = Math.Max(0, data.clinicSlotsTotal - data.clinicSlotsOccupied);

            var eligibleColonyPawns = map.mapPawns.AllPawnsSpawned
                .Where(p => p != null && p.Faction == Faction.OfPlayer && !p.IsPrisoner &&
                            !p.Dead && !p.Downed && !p.Drafted && !p.InMentalState &&
                            p.workSettings != null)
                .ToList();

            data.doctorsReady = eligibleColonyPawns.Count(p =>
                !p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) && p.workSettings.GetPriority(WorkTypeDefOf.Doctor) > 0 &&
                p.health != null && p.health.capacities != null &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Sight) &&
                Mode(desk, p) != ComplexWorkMode.ColonyOnly);

            // 3. Restaurant
            data.restaurantOpen = desk.restaurantOpen;
            data.restaurantPercent = desk.restaurantPercent;
            var counters = map.listerBuildings.allBuildingsColonist.OfType<Building_ServiceCounter>()
                .Where(c => desk.area != null && desk.area[c.Position])
                .ToList();
            data.counterCount = counters.Count;
            data.menuCount = desk.restaurantMenu != null ? desk.restaurantMenu.Count : 0;

            var storages = map.listerBuildings.allBuildingsColonist.OfType<Building_Storage>()
                .Where(s => RestaurantMeals.FridgeAt(map, s.Position))
                .ToList();
            data.fridgesCount = storages.Count;

            data.mealsInStockTotal = 0;
            if (desk.restaurantMenu != null && desk.restaurantMenu.Count > 0)
            {
                var primaryCounter = counters.FirstOrDefault();
                foreach (var def in desk.restaurantMenu.Distinct())
                {
                    if (def == null) continue;
                    if (primaryCounter != null)
                    {
                        data.mealsInStockTotal += map.listerThings.ThingsOfDef(def)
                            .Where(t => t.Spawned && RestaurantMeals.Stock(primaryCounter, t))
                            .Sum(t => t.stackCount);
                    }
                    else
                    {
                        data.mealsInStockTotal += map.listerThings.ThingsOfDef(def)
                            .Where(t => t.Spawned && RestaurantMeals.FridgeAt(map, t.Position))
                            .Sum(t => t.stackCount);
                    }
                }
            }

            var waiterWork = DefDatabase<WorkTypeDef>.GetNamedSilentFail("TMM_RestaurantService");
            data.waitersReady = waiterWork == null ? 0 : eligibleColonyPawns.Count(p =>
                !p.WorkTypeIsDisabled(waiterWork) && p.workSettings.GetPriority(waiterWork) > 0 &&
                p.health != null && p.health.capacities != null &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                Mode(desk, p) != ComplexWorkMode.ColonyOnly);

            // 4. Sales
            var stores = SaleStock.Stores(map).ToList();
            data.storesTotal = stores.Count;
            data.storesPublic = stores.Count(SaleStock.Public);
            data.storesStaffOnly = data.storesTotal - data.storesPublic;

            var stock = stores.SelectMany(SaleStock.Contents).Where(SaleStock.Tradeable).Distinct().ToList();
            data.tradeableStacksTotal = stock.Count;
            data.tradeableUnitsTotal = stock.Sum(t => t.stackCount);

            data.sellersReady = !desk.Active ? 0 : eligibleColonyPawns.Count(p =>
                !p.WorkTypeIsDisabled(ComplexSales.SalesWork) && p.workSettings.GetPriority(ComplexSales.SalesWork) > 0 &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && p.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                Mode(desk, p) != ComplexWorkMode.ColonyOnly);

            // 5. Staff

            var validEmployees = (desk.employees ?? Enumerable.Empty<ComplexEmployee>())
                .Where(e => e != null && e.pawn != null && !e.pawn.Destroyed && !e.pawn.Dead)
                .ToList();

            data.totalEmployees = validEmployees.Count;
            data.residentEmployees = validEmployees.Count(e => e.resident);
            data.companyEmployees = desk.companyOpen ? validEmployees.Count(e => e.company) : 0;

            var cleanWork = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cleaning") ?? WorkTypeDefOf.Cleaning;
            data.cleanersReady = cleanWork == null ? 0 : eligibleColonyPawns.Count(p =>
                !p.WorkTypeIsDisabled(cleanWork) && p.workSettings.GetPriority(cleanWork) > 0 &&
                p.health != null && p.health.capacities != null &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                Mode(desk, p) != ComplexWorkMode.ColonyOnly);

            var companyWork = CompanyRules.Work;
            data.hostsReady = companyWork == null || !desk.Active ? 0 : eligibleColonyPawns.Count(p =>
                validEmployees.Any(e => e.pawn == p && e.company) && !p.WorkTypeIsDisabled(companyWork) && p.workSettings.GetPriority(companyWork) > 0 &&
                Mode(desk, p) != ComplexWorkMode.ColonyOnly);

            var cookWork = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking");
            data.cooksReady = cookWork == null ? 0 : eligibleColonyPawns.Count(p =>
                cookWork != null && !p.WorkTypeIsDisabled(cookWork) && p.workSettings.GetPriority(cookWork) > 0 &&
                p.health != null && p.health.capacities != null &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                Mode(desk, p) != ComplexWorkMode.ColonyOnly);

            return data;
        }
    }

    public sealed class Dialog_ServiceReadiness : Window
    {
        private readonly Building_ComplexDesk desk;
        private ServiceReadinessData data;
        private Vector2 scroll;
        private float lastCalculatedHeight = 1400f;

        public Dialog_ServiceReadiness(Building_ComplexDesk desk)
        {
            this.desk = desk;
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            Refresh();
        }

        public override Vector2 InitialSize { get { return new Vector2(780, 750); } }

        public void Refresh()
        {
            if (desk != null && desk.Spawned && desk.Map != null)
            {
                data = ServiceReadinessData.Gather(desk);
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            if (desk == null || !desk.Spawned || desk.Map == null || data == null)
            {
                Close();
                return;
            }

            var prevFont = Text.Font;
            var prevAnchor = Text.Anchor;
            var prevColor = GUI.color;
            var prevWordWrap = Text.WordWrap;

            try
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.WordWrap = true;

                // Title
                Widgets.Label(new Rect(0, 0, rect.width - 150, 34), "TMM.ServiceReadiness.Title".Translate());

                // Refresh Button
                Text.Font = GameFont.Small;
                if (Widgets.ButtonText(new Rect(rect.width - 140, 2, 140, 32), "TMM.ServiceReadiness.Refresh".Translate()))
                {
                    Refresh();
                }

                // Help text
                string help = "TMM.ServiceReadiness.Help".Translate();
                float helpH = Text.CalcHeight(help, rect.width);
                Widgets.Label(new Rect(0, 40, rect.width, helpH), help);

                float topOffset = 44 + helpH + 8;
                var viewRect = new Rect(0, topOffset, rect.width, rect.height - topOffset - 42);
                var innerRect = new Rect(0, 0, viewRect.width - 20, Math.Max(viewRect.height, lastCalculatedHeight));

                Widgets.BeginScrollView(viewRect, ref scroll, innerRect);
                try
                {
                    float y = 0f;

                    // Section: General
                    DrawSectionHeader(innerRect, ref y, "TMM.ServiceReadiness.SectionGeneral".Translate());
                    string deskStatusText = data.deskActive
                        ? (string)"TMM.ServiceReadiness.ActiveDesk".Translate()
                        : (string)"TMM.ServiceReadiness.InactiveDesk".Translate();
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.DeskStatusLabel".Translate(), deskStatusText, !data.deskActive);

                    string areaVal = string.IsNullOrEmpty(data.areaName)
                        ? (string)"TMM.ServiceReadiness.NoArea".Translate()
                        : "TMM.ServiceReadiness.AreaCells".Translate(data.areaName, data.areaCells).ToString();
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.Area".Translate(), areaVal, string.IsNullOrEmpty(data.areaName));

                    if (!string.IsNullOrEmpty(data.staffAreaName))
                        DrawRow(innerRect, ref y, "TMM.ServiceReadiness.StaffArea".Translate(), data.staffAreaName);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.AutoServices".Translate(),
                        (data.automaticServices ? "TMM.ServiceReadiness.Enabled" : "TMM.ServiceReadiness.Disabled").Translate());

                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.HospitalityModule".Translate(),
                        (data.hotelModuleEnabled ? "TMM.ServiceReadiness.ModuleDetected" : "TMM.ServiceReadiness.ModuleMissing").Translate(), !data.hotelModuleEnabled);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.HospitalModule".Translate(),
                        (data.clinicModuleEnabled ? "TMM.ServiceReadiness.ModuleDetected" : "TMM.ServiceReadiness.ModuleMissing").Translate(), !data.clinicModuleEnabled);

                    if (!data.deskActive)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.InactiveDeskWarning".Translate());
                    y += 10f;

                    // Section: Hotel
                    DrawSectionHeader(innerRect, ref y, "TMM.ServiceReadiness.SectionHotel".Translate());

                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.GuestsWelcomeLabel".Translate(),
                        (data.guestsWelcome ? "TMM.ServiceReadiness.Allowed" : "TMM.ServiceReadiness.Forbidden").Translate(), !data.guestsWelcome);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.RefuseUntilBedsLabel".Translate(),
                        (data.refuseUntilBeds ? "TMM.ServiceReadiness.Yes" : "TMM.ServiceReadiness.No").Translate());

                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.RoomRate".Translate(), "TMM.ServiceReadiness.RoomRateValue".Translate(data.roomRate));
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.GuestBeds".Translate(),
                        "TMM.ServiceReadiness.GuestBedsValue".Translate(data.guestBedsTotal, data.guestSlotsTotal), data.guestBedsTotal == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.GuestSlotsOccupied".Translate(), data.guestSlotsOccupied.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.GuestSlotsAvailable".Translate(),
                        data.guestSlotsAvailable.ToString(), data.guestSlotsAvailable == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.GuestBedsFullyFree".Translate(),
                        data.guestBedsFullyFree.ToString(), data.guestBedsFullyFree == 0);

                    if (data.staffBedsCount > 0)
                        DrawRow(innerRect, ref y, "TMM.ServiceReadiness.StaffBedsInArea".Translate(), data.staffBedsCount.ToString());
                    if (data.hotelStaysCount > 0)
                        DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ActiveHotelStays".Translate(), data.hotelStaysCount.ToString());
                    if (!string.IsNullOrEmpty(data.nextVisitString))
                        DrawRow(innerRect, ref y, "TMM.ServiceReadiness.NextVisitLabel".Translate(), data.nextVisitString);

                    if (!data.guestsWelcome)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.GuestsWelcomeDisabledWarning".Translate());
                    if (data.guestSlotsAvailable == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoGuestBedsWarning".Translate());
                    DrawNote(innerRect, ref y, "TMM.ServiceReadiness.HotelNote".Translate());
                    y += 10f;

                    // Section: Clinic
                    DrawSectionHeader(innerRect, ref y, "TMM.ServiceReadiness.SectionClinic".Translate());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ClinicAdmissionLabel".Translate(),
                        (data.clinicAdmissionEnabled ? "TMM.ServiceReadiness.Enabled" : "TMM.ServiceReadiness.Disabled").Translate(), !data.clinicAdmissionEnabled);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.NativeIsOpenLabel".Translate(),
                        (data.clinicIsOpen ? "TMM.ServiceReadiness.OpenNow" : "TMM.ServiceReadiness.ClosedNow").Translate(), !data.clinicIsOpen);
                    string hoursSummary = "TMM.ServiceReadiness.ScheduleHoursValue".Translate(
                        data.clinicOpenHoursCount,
                        (data.clinicScheduleHourAllowed ? "TMM.ServiceReadiness.HourAllowed" : "TMM.ServiceReadiness.HourClosed").Translate()
                    );
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ScheduleHoursLabel".Translate(), hoursSummary);

                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.SurgeryStatus".Translate(),
                        (data.surgeryAllowed ? "TMM.ServiceReadiness.Allowed" : "TMM.ServiceReadiness.Forbidden").Translate(), !data.surgeryAllowed);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ClinicBeds".Translate(),
                        "TMM.ServiceReadiness.ClinicBedsValue".Translate(data.clinicBedsTotal, data.clinicSlotsTotal), data.clinicBedsTotal == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ClinicOccupied".Translate(), data.clinicSlotsOccupied.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ClinicAvailable".Translate(), data.clinicSlotsAvailable.ToString(), data.clinicSlotsAvailable == 0);

                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyDoctors".Translate(), data.doctorsReady.ToString(), data.doctorsReady == 0);

                    if (data.clinicBedsTotal == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoClinicBedsWarning".Translate());
                    if (data.doctorsReady == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoDoctorsWarning".Translate());
                    DrawNote(innerRect, ref y, "TMM.ServiceReadiness.ClinicNote".Translate());
                    y += 10f;

                    // Section: Restaurant
                    DrawSectionHeader(innerRect, ref y, "TMM.ServiceReadiness.SectionRestaurant".Translate());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.RestaurantOrders".Translate(),
                        (data.restaurantOpen ? "TMM.ServiceReadiness.Enabled" : "TMM.ServiceReadiness.Disabled").Translate(), !data.restaurantOpen);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.PricePercent".Translate(), data.restaurantPercent + "%");
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ServiceCounters".Translate(), data.counterCount.ToString(), data.counterCount == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.MenuItems".Translate(), data.menuCount.ToString(), data.menuCount == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.FridgesFound".Translate(), data.fridgesCount.ToString(), data.fridgesCount == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyMealsStock".Translate(),
                        "TMM.ServiceReadiness.Pieces".Translate(data.mealsInStockTotal), data.mealsInStockTotal == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyWaiters".Translate(), data.waitersReady.ToString(), data.waitersReady == 0);

                    if (!data.restaurantOpen)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.RestaurantClosedWarning".Translate());
                    if (data.counterCount == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoCountersWarning".Translate());
                    if (data.menuCount == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.EmptyMenuWarning".Translate());
                    else if (data.mealsInStockTotal == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoMealsWarning".Translate());
                    if (data.waitersReady == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoWaitersWarning".Translate());
                    DrawNote(innerRect, ref y, "TMM.ServiceReadiness.RestaurantNote".Translate());
                    y += 10f;

                    // Section: Sales
                    DrawSectionHeader(innerRect, ref y, "TMM.ServiceReadiness.SectionSales".Translate());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.StoresTotal".Translate(), data.storesTotal.ToString(), data.storesTotal == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.StoresPublic".Translate(), data.storesPublic.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.StoresStaffOnly".Translate(), data.storesStaffOnly.ToString());

                    string itemsSummary = "TMM.ServiceReadiness.SaleItemsSummary".Translate(data.tradeableStacksTotal, data.tradeableUnitsTotal);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.SaleItemsTotal".Translate(), itemsSummary, data.tradeableStacksTotal == 0);
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadySellers".Translate(),
                        data.sellersReady.ToString(), data.sellersReady == 0 && data.storesStaffOnly > 0);

                    if (data.storesTotal == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoStoresWarning".Translate());
                    else if (data.storesStaffOnly > 0 && data.sellersReady == 0)
                        DrawWarningBox(innerRect, ref y, "TMM.ServiceReadiness.NoSellersWarning".Translate());
                    DrawNote(innerRect, ref y, "TMM.ServiceReadiness.SalesNote".Translate());
                    y += 10f;

                    // Section: Staff
                    DrawSectionHeader(innerRect, ref y, "TMM.ServiceReadiness.SectionStaff".Translate());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.TotalEmployees".Translate(), data.totalEmployees.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ResidentEmployees".Translate(), data.residentEmployees.ToString());
                    if (desk.companyOpen)
                        DrawRow(innerRect, ref y, "TMM.ServiceReadiness.CompanyEmployees".Translate(), data.companyEmployees.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyDoctors".Translate(), data.doctorsReady.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyWaiters".Translate(), data.waitersReady.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadySellers".Translate(), data.sellersReady.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyCleaners".Translate(), data.cleanersReady.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyHosts".Translate(), data.hostsReady.ToString());
                    DrawRow(innerRect, ref y, "TMM.ServiceReadiness.ReadyCooks".Translate(), data.cooksReady.ToString());
                    DrawNote(innerRect, ref y, "TMM.ServiceReadiness.StaffNote".Translate());
                    y += 10f;

                    lastCalculatedHeight = y + 20f;
                }
                finally
                {
                    Widgets.EndScrollView();
                }
            }
            finally
            {
                Text.Font = prevFont;
                Text.Anchor = prevAnchor;
                GUI.color = prevColor;
                Text.WordWrap = prevWordWrap;
            }
        }

        private static void DrawSectionHeader(Rect inner, ref float y, string title)
        {
            var bar = new Rect(0, y, inner.width, 28);
            Widgets.DrawBoxSolid(bar, new Color(0.2f, 0.25f, 0.32f, 0.5f));
            var labelRect = new Rect(8, y + 2, inner.width - 16, 24);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.9f, 0.95f, 1f);
            Widgets.Label(labelRect, title);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            y += 32;
        }

        private static void DrawRow(Rect inner, ref float y, string label, string value, bool isWarning = false)
        {
            float totalW = inner.width - 16;
            float leftW = totalW * 0.62f;
            float rightW = totalW * 0.38f;

            float leftH = Text.CalcHeight(label, leftW);
            float rightH = Text.CalcHeight(value, rightW);
            float rowH = Math.Max(26f, Math.Max(leftH, rightH) + 4f);

            var rowRect = new Rect(8, y, totalW, rowH);
            if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);

            var leftRect = new Rect(8, y + 2, leftW, rowH - 4);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.85f, 0.85f, 0.88f);
            Widgets.Label(leftRect, label);

            var rightRect = new Rect(8 + leftW, y + 2, rightW, rowH - 4);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = isWarning ? new Color(1f, 0.55f, 0.45f) : Color.white;
            Widgets.Label(rightRect, value);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            y += rowH + 2f;
        }

        private static void DrawWarningBox(Rect inner, ref float y, string warningText)
        {
            float textH = Text.CalcHeight(warningText, inner.width - 32);
            float boxH = Math.Max(26f, textH + 8f);
            var boxRect = new Rect(8, y, inner.width - 16, boxH);
            Widgets.DrawBoxSolid(boxRect, new Color(0.45f, 0.2f, 0.15f, 0.45f));
            GUI.color = new Color(1f, 0.75f, 0.35f);
            Widgets.Label(new Rect(14, y + 4, inner.width - 28, textH), warningText);
            GUI.color = Color.white;
            y += boxH + 6f;
        }

        private static void DrawNote(Rect inner, ref float y, string noteText)
        {
            float textH = Text.CalcHeight(noteText, inner.width - 24);
            var noteRect = new Rect(12, y, inner.width - 24, textH);
            GUI.color = new Color(0.7f, 0.75f, 0.8f);
            Widgets.Label(noteRect, noteText);
            GUI.color = Color.white;
            y += textH + 8f;
        }
    }
}
