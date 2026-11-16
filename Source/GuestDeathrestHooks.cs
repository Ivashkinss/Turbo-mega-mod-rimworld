using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class GuestDeathrestHooks
    {
        public static bool BindingPatchInstalled;
        static GuestDeathrestHooks()
        {
            if(!ModsConfig.BiotechActive||!HotelBillingHooks.Enabled)return;
            var h=new Harmony("turbomegamod.hotel.deathrest");
            h.Patch(AccessTools.Method(typeof(JobDriver_Deathrest),"MakeNewToils"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Toils"));
            h.Patch(AccessTools.Method(typeof(JobGiver_GetDeathrest),"TryGiveJob"),prefix:new HarmonyMethod(typeof(GuestDeathrestHooks),"NativeJob"));
            h.Patch(AccessTools.Method(typeof(Gene_Deathrest),"Wake"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Woke"));
            h.Patch(AccessTools.Method(typeof(Gene_Deathrest),"CanBindToBindable"),transpiler:new HarmonyMethod(typeof(GuestDeathrestHooks),"Binding"));
            h.Patch(AccessTools.Method(typeof(Gene_Deathrest),"BindTo"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Bound"));
            h.Patch(AccessTools.Method(typeof(LordJob),"ShouldRemovePawn"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Keep"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.Trigger_TicksPassedAndOkayToLeave"),"ActivateOn"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Hold"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.LordJob_VisitColony"),"OnLeaveTriggered"),prefix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Leaving"));
            h.Patch(AccessTools.Method(AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),"IsAvailableBed"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Available"));
            h.Patch(AccessTools.Method(typeof(ThingWithComps),"TickRare"),postfix:new HarmonyMethod(typeof(GuestDeathrestHooks),"Rare"));
            Install(DefDatabase<DutyDef>.GetNamed("Relax").thinkNode);
            LongEventHandler.ExecuteWhenFinished(ConfigureCasket);
        }
        private static void ConfigureCasket()
        {
            var def=DefDatabase<ThingDef>.GetNamedSilentFail("DeathrestCasketGuest");
            var original=DefDatabase<ThingDef>.GetNamedSilentFail("DeathrestCasket");
            if(def==null||original==null)return;
            def.tickerType=TickerType.Rare;
            // A rented casket uses OwnedBed, not the permanent
            // AssignedDeathrestCasket slot (whose load validator removes renters).
            var rental=def.comps.FirstOrDefault(c=>c.compClass==typeof(CompAssignableToPawn_DeathrestCasket));
            if(rental!=null)rental.compClass=typeof(CompAssignableToPawn_Bed);
            // Hospitality clones public fields. In 1.7 basePowerConsumption
            // became private and silently remains zero in the generated copy.
            var field=AccessTools.Field(typeof(CompProperties_Power),"basePowerConsumption");
            var from=original.GetCompProperties<CompProperties_Power>();var to=def.GetCompProperties<CompProperties_Power>();
            if(field!=null&&from!=null&&to!=null)field.SetValue(to,field.GetValue(from));
        }
        private static bool Install(ThinkNode node)
        {
            if(node is ThinkNode_PrioritySorter&&node.subNodes.Any(n=>n.GetType().FullName=="Hospitality.JobGiver_Relax")){
                node.subNodes.Insert(0,new JobGiver_GuestDeathrest());return true;}
            foreach(var child in node.subNodes)if(Install(child))return true;return false;
        }
        private static bool NativeJob(Pawn pawn,ref Job __result)
        {
            if(!GuestDeathrest.Visiting(pawn))return true;
            __result=GuestDeathrest.MakeJob(pawn);return false;
        }
        private static void Toils(JobDriver __instance,ref IEnumerable<Toil> __result){__result=Protect(__instance,__result);}
        private static IEnumerable<Toil> Protect(JobDriver driver,IEnumerable<Toil> toils)
        {
            foreach(var toil in toils){var original=toil.initAction;if(original!=null)toil.initAction=delegate{
                var p=driver.pawn;
                if(GuestDeathrest.Visiting(p)&&GuestDeathrest.Stay(p)==null&&!p.Map.GetComponent<GuestDeathrest>().Begin(p,driver.job.targetA.Thing as Building_Bed)){
                    driver.EndJobWith(JobCondition.Incompletable);return;}
                original();};yield return toil;}
        }
        private static void Woke(Gene_Deathrest __instance)
        {
            var p=__instance.pawn;var s=GuestDeathrest.Stay(p);if(s!=null)p.MapHeld.GetComponent<GuestDeathrest>().End(s);
        }
        private static void Keep(LordJob __instance,Pawn __0,PawnLostCondition __1,ref bool __result)
        {
            var s=GuestDeathrest.Stay(__0);if(__1==PawnLostCondition.Incapped&&s!=null&&s.lord!=null&&s.lord.LordJob==__instance)__result=false;
        }
        private static void Hold(Lord __0,ref bool __result){if(__result&&__0.ownedPawns.Any(p=>GuestDeathrest.Stay(p)!=null))__result=false;}
        private static void Leaving(object __instance)
        {
            var lord=AccessTools.Field(__instance.GetType(),"lord").GetValue(__instance) as Lord;
            if(lord!=null)foreach(var p in lord.ownedPawns.ToArray())GuestDeathrest.Cancel(p);
        }
        private static bool GuestCasket(Thing t){return t!=null&&t.def.defName=="DeathrestCasketGuest";}
        private static void Available(Building_Bed __0,Pawn __1,ref bool __result)
        {
            if(!__result||!GuestCasket(__0))return;
            var pawn=__1;var gene=GuestDeathrest.Gene(pawn);var comp=__0.TryGetComp<CompDeathrestBindable>();
            if(gene==null||!gene.Active||comp==null||(comp.BoundPawn!=null&&comp.BoundPawn!=pawn))__result=false;
        }
        private static void Rare(ThingWithComps __instance)
        {
            // Rare ticks update the casket's own native comps. Preserve the
            // guest-bed long housekeeping without also ticking comps twice.
            if(GuestCasket(__instance)&&__instance.IsHashIntervalTick(2500))__instance.TickLong();
        }
        public static bool SameFactionOrOwnCasket(Faction building,Faction pawn,Gene_Deathrest gene,CompDeathrestBindable comp)
        {
            if(building==pawn)return true;
            var s=GuestDeathrest.Stay(gene.pawn);
            return BindingPatchInstalled&&s!=null&&s.bed==comp.parent&&GuestCasket(comp.parent)&&building==Faction.OfPlayer&&
                (comp.BoundPawn==null||comp.BoundPawn==gene.pawn);
        }
        private static IEnumerable<CodeInstruction> Binding(IEnumerable<CodeInstruction> instructions)
        {
            var code=instructions.ToList();int found=0;
            for(int i=1;i<code.Count;i++){
                var branch=code[i];if(!code[i-1].Calls(AccessTools.PropertyGetter(typeof(Thing),"Faction")))continue;
                bool equal=branch.opcode==OpCodes.Beq||branch.opcode==OpCodes.Beq_S;
                bool different=branch.opcode==OpCodes.Bne_Un||branch.opcode==OpCodes.Bne_Un_S;
                if(!equal&&!different)continue;
                var first=new CodeInstruction(OpCodes.Ldarg_0);first.labels.AddRange(branch.labels);first.blocks.AddRange(branch.blocks);branch.labels.Clear();branch.blocks.Clear();
                code.InsertRange(i,new[]{first,new CodeInstruction(OpCodes.Ldarg_1),new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(GuestDeathrestHooks),"SameFactionOrOwnCasket"))});i+=3;
                branch.opcode=equal?OpCodes.Brtrue:OpCodes.Brfalse;found++;
            }
            if(found!=1)throw new InvalidOperationException("Expected one native deathrest faction comparison, got "+found);
            BindingPatchInstalled=true;return code;
        }
        private static void Bound(Gene_Deathrest __instance,CompDeathrestBindable __0)
        {
            var s=GuestDeathrest.Stay(__instance.pawn);
            if(s!=null&&s.bed==__0.parent&&__0.BoundPawn==s.pawn&&s.bed.Faction!=s.pawn.Faction)s.temporaryBinding=true;
        }
    }
}
