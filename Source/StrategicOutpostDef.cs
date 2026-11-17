using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public class StrategicOutpostDef : WorldObjectDef
    {
        public int baseUpgradeCost = 500;
        public float costMultiplier = 1.6f;
        public int maxLevel = 10;
        public int baseCapacity = 2;
        public int capacityPerLevel = 1;
        public int laborTicksPerDay = 60000;

        public int CalculateUpgradeCost(int currentLevel)
        {
            if (currentLevel >= Math.Min(10, maxLevel)) return 0;
            if (float.IsNaN(costMultiplier) || float.IsInfinity(costMultiplier)) return int.MaxValue;
            decimal value = Math.Max(1, baseUpgradeCost);
            decimal factor = (decimal)Math.Max(1, Math.Round(costMultiplier, 6));
            for (int n = 0; n < Math.Max(0, currentLevel - 1); n++)
            {
                value *= factor;
                if (value >= int.MaxValue) return int.MaxValue;
            }
            return (int)decimal.Ceiling(value);
        }

        public int CalculateMaxCapacity(int currentLevel)
        {
            return Math.Max(1, Math.Min(10, currentLevel)) + 1;
        }
    }

    public class StrategicOutpostProductDef : Def
    {
        public StrategicOutpostSpecialization specialization = StrategicOutpostSpecialization.Mining;
        public ThingDef producedThingDef;
        public SkillDef relevantSkill;
        public WorkTypeDef workType;
        public ResearchProjectDef requiredResearch;
        public RecipeDef recipe;
        public ThingDef workstation;
        // Legacy explicit inputs are retained for data compatibility; factories always use RecipeDef.
        public List<ThingDefCountClass> requiredIngredients = new List<ThingDefCountClass>();
        public float baseDailyYieldPerPawn = 10f;
        public double yieldPerSkill = -1;
        public bool disabledByDefault;
        public string disabledReason;

        public double Rate
        {
            get { return yieldPerSkill >= 0 ? yieldPerSkill : (double)baseDailyYieldPerPawn / 10.0; }
        }

        public bool IsUnlocked(out string reason)
        {
            reason = "StrategicOutpost_ProductUnavailable".Translate();
            if (disabledByDefault || producedThingDef == null || producedThingDef.MadeFromStuff || relevantSkill == null ||
                double.IsNaN(Rate) || double.IsInfinity(Rate) || Rate <= 0) return false;
            if (requiredResearch != null && !requiredResearch.IsFinished)
            {
                reason = "StrategicOutpost_ResearchRequired".Translate(requiredResearch.LabelCap);
                return false;
            }
            if (specialization == StrategicOutpostSpecialization.Factory)
            {
                if (recipe == null || workstation == null || !workstation.AllRecipes.Contains(recipe) || !recipe.AvailableNow ||
                    recipe.products == null || recipe.products.Count != 1 || recipe.products[0].thingDef != producedThingDef || recipe.products[0].count != 1 ||
                    recipe.ingredients == null || recipe.ingredients.Count == 0) return false;
                if (workstation.researchPrerequisites != null && workstation.researchPrerequisites.Any(r => r == null || !r.IsFinished)) return false;
                if (recipe.ingredients.Any(i => i == null || !i.IsFixedIngredient || i.FixedIngredient == null)) return false;
            }
            reason = null;
            return true;
        }

        public bool WorkerCanProduce(Pawn pawn)
        {
            if (!StrategicOutpostCaravanUtility.IsSuitableColonist(pawn) || pawn.skills == null ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return false;
            var skill = pawn.skills.GetSkill(relevantSkill);
            if (skill == null || skill.TotallyDisabled || skill.Level <= 0) return false;
            var work = workType ?? (relevantSkill == SkillDefOf.Mining ? WorkTypeDefOf.Mining : relevantSkill == SkillDefOf.Plants ? WorkTypeDefOf.Growing : WorkTypeDefOf.Crafting);
            if (pawn.WorkTypeIsDisabled(work)) return false;
            return recipe == null || recipe.skillRequirements == null || recipe.skillRequirements.All(r => pawn.skills.GetSkill(r.skill).Level >= r.minLevel);
        }

        private bool UnitCosts(out Dictionary<ThingDef, int> costs)
        {
            costs = new Dictionary<ThingDef, int>();
            if (specialization == StrategicOutpostSpecialization.Factory)
            {
                string reason;
                if (!IsUnlocked(out reason)) return false;
                foreach (var ingredient in recipe.ingredients)
                {
                    var def = ingredient.FixedIngredient;
                    int count = ingredient.CountRequiredOfFor(def, recipe, null);
                    if (count <= 0 || count > 1000000) return false;
                    costs[def] = costs.ContainsKey(def) ? checked(costs[def] + count) : count;
                }
            }
            else if (requiredIngredients != null)
            {
                foreach (var ingredient in requiredIngredients)
                {
                    if (ingredient == null || ingredient.thingDef == null || ingredient.count <= 0) return false;
                    costs[ingredient.thingDef] = costs.ContainsKey(ingredient.thingDef) ? checked(costs[ingredient.thingDef] + ingredient.count) : ingredient.count;
                }
            }
            return true;
        }

        public int CalculateMaxUnitsFromIngredients(ThingOwner<Thing> rawStorage)
        {
            Dictionary<ThingDef, int> costs;
            if (!UnitCosts(out costs)) return 0;
            int max = int.MaxValue;
            foreach (var cost in costs)
            {
                if (rawStorage == null) return 0;
                max = Math.Min(max, rawStorage.TotalStackCountOfDef(cost.Key) / cost.Value);
            }
            return max;
        }

        public bool ConsumeIngredients(ThingOwner<Thing> rawStorage, int units)
        {
            if (units <= 0) return false;
            Dictionary<ThingDef, int> costs;
            if (!UnitCosts(out costs)) return false;
            var payment = new OutpostSubHolder(null, "IngredientPayment");
            if (!OutpostTransaction.Reserve(rawStorage, payment.Container, costs, units)) return false;
            OutpostTransaction.Destroy(payment.Container);
            return true;
        }

        public bool ProduceInto(ThingOwner<Thing> rawStorage, ThingOwner<Thing> output, int units)
        {
            string reason;
            if (units <= 0 || output == null || !IsUnlocked(out reason)) return false;
            Dictionary<ThingDef, int> costs;
            if (!UnitCosts(out costs)) return false;
            var payment = new OutpostSubHolder(null, "IngredientPayment");
            if (!OutpostTransaction.Reserve(rawStorage, payment.Container, costs, units)) return false;
            var created = new List<Thing>();
            bool committed = false;
            try
            {
                int remaining = units;
                while (remaining > 0)
                {
                    var item = ThingMaker.MakeThing(producedThingDef);
                    item.stackCount = Math.Min(remaining, Math.Max(1, producedThingDef.stackLimit));
                    created.Add(item);
                    if (!output.TryAdd(item, false)) return false;
                    remaining -= item.stackCount;
                }
                committed = true;
                OutpostTransaction.Destroy(payment.Container);
                return true;
            }
            finally
            {
                if (!committed)
                {
                    foreach (var item in created)
                    {
                        if (item.holdingOwner != null) item.holdingOwner.Remove(item);
                        if (!item.Destroyed) item.Destroy();
                    }
                    foreach (var item in payment.Container.InnerListForReading.ToArray())
                    {
                        payment.Container.TryTransferToContainer(item, rawStorage, item.stackCount, false);
                    }
                }
            }
        }
    }

    internal static class OutpostTransaction
    {
        public static bool Reserve(ThingOwner<Thing> source, ThingOwner<Thing> escrow, Dictionary<ThingDef, int> costs, int multiplier)
        {
            if (costs.Count == 0) return true;
            if (source == null || multiplier <= 0 || costs.Any(c => (long)c.Value * multiplier > int.MaxValue || source.TotalStackCountOfDef(c.Key) < (long)c.Value * multiplier)) return false;
            foreach (var cost in costs)
            {
                int needed = cost.Value * multiplier;
                foreach (var item in source.InnerListForReading.Where(t => t.def == cost.Key).ToArray())
                {
                    int moved = source.TryTransferToContainer(item, escrow, Math.Min(needed, item.stackCount), false);
                    needed -= moved;
                    if (needed <= 0) break;
                }
                if (needed > 0)
                {
                    foreach (var item in escrow.InnerListForReading.ToArray())
                    {
                        escrow.TryTransferToContainer(item, source, item.stackCount, false);
                    }
                    return false;
                }
            }
            return true;
        }

        public static void Destroy(ThingOwner<Thing> owner)
        {
            foreach (var item in owner.InnerListForReading.ToArray())
            {
                owner.Remove(item);
                item.Destroy();
            }
        }
    }
}
