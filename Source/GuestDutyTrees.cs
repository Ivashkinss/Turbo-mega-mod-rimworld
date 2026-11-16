using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class GuestDutyTrees
    {
        public static int PatchedCopies, AddedNodes;
        public static bool ExistingKeysPreserved=true;
        private static readonly ConditionalWeakTable<ThinkNode_Duty,object> ready=new ConditionalWeakTable<ThinkNode_Duty,object>();
        static GuestDutyTrees()
        {
            if(!HotelBillingHooks.Enabled)return;
            // ThinkNode_Duty cloned every DutyDef before our startup hooks ran.
            // Editing only DutyDef.thinkNode does not change those live copies.
            var h=new Harmony("turbomegamod.guest.cached.duties");
            h.Patch(AccessTools.Method(typeof(ThinkNode_Duty),"ResolveSubnodes"),
                postfix:new HarmonyMethod(typeof(GuestDutyTrees),"Resolved"));
            h.Patch(AccessTools.Method(typeof(ThinkNode_Duty),"TryIssueJobPackage"),prefix:new HarmonyMethod(typeof(GuestDutyTrees),"Ready"));
            LongEventHandler.ExecuteWhenFinished(Synchronize);
        }
        private static IEnumerable<ThinkNode> Walk(ThinkNode root)
        {
            var seen=new HashSet<ThinkNode>();var stack=new Stack<ThinkNode>();if(root!=null)stack.Push(root);
            while(stack.Count>0){var n=stack.Pop();if(!seen.Add(n))continue;yield return n;for(int i=n.subNodes.Count-1;i>=0;i--)stack.Push(n.subNodes[i]);}
        }
        private static ThinkNode Sorter(ThinkNode root)
        {
            return Walk(root).FirstOrDefault(n=>n is ThinkNode_PrioritySorter&&n.subNodes.Any(c=>c.GetType().FullName=="Hospitality.JobGiver_Relax"));
        }
        private static void Prepare(ThinkNode node,ThinkNode parent,string seed)
        {
            AccessTools.Field(typeof(ThinkNode),"parent").SetValue(node,parent);
            node.ResolveReferences();
            // Assign keys only to new nodes, with the native collision registry
            // and saved RNG state. Existing nodes retain their original keys.
            var assign=AccessTools.Method(AccessTools.TypeByName("Verse.ThinkTreeKeyAssigner"),"AssignSingleKey");
            assign.Invoke(null,new object[]{node,GenText.StableStringHash(seed)});
            for(int i=0;i<node.subNodes.Count;i++)Prepare(node.subNodes[i],node,seed+":"+i);
            AddedNodes++;
        }
        private static void PatchCopy(ThinkNode_Duty duty)
        {
            var def=DefDatabase<DutyDef>.GetNamedSilentFail("Relax");
            if(def==null||def.index>=duty.subNodes.Count)return;
            var root=duty.subNodes[def.index];var target=Sorter(root);if(target==null)return;
            var source=Sorter(def.thinkNode);if(source==null)return;
            var original=Walk(root).ToDictionary(n=>n,n=>n.UniqueSaveKey);
            bool changed=false;
            // Include only our service nodes already installed in this def.
            foreach(var type in new[]{typeof(JobGiver_GuestHemogen),typeof(JobGiver_GuestDeathrest),typeof(JobGiver_GuestOutdoor)}){
                if(!source.subNodes.Any(n=>n.GetType()==type)||target.subNodes.Any(n=>n.GetType()==type))continue;
                var node=(ThinkNode)Activator.CreateInstance(type);
                Prepare(node,target,"TurboMegaMod:Relax:"+root.UniqueSaveKey+":"+type.FullName);
                target.subNodes.Insert(target.subNodes.FindIndex(n=>n.GetType().FullName=="Hospitality.JobGiver_Relax"),node);changed=true;
            }
            if(Walk(def.thinkNode).Any(n=>n is JobGiver_ClinicPatient)&&!Walk(root).Any(n=>n is JobGiver_ClinicPatient)){
                var tag=new ThinkNode_Tagger();AccessTools.Field(typeof(ThinkNode_Tagger),"tagToGive").SetValue(tag,JobTag.RestingForMedicalReasons);
                tag.subNodes.Add(new JobGiver_ClinicPatient());Prepare(tag,root,"TurboMegaMod:Relax:"+root.UniqueSaveKey+":Clinic");
                int at=root.subNodes.FindIndex(n=>n.GetType().FullName=="Hospitality.ThinkNode_InGuestArea");root.subNodes.Insert(at<0?0:at,tag);changed=true;
            }
            ExistingKeysPreserved&=original.All(pair=>pair.Key.UniqueSaveKey==pair.Value);
            if(changed)PatchedCopies++;
        }
        private static void Resolved(ThinkNode_Duty __instance){ready.Remove(__instance);PatchCopy(__instance);}
        private static void Ready(ThinkNode_Duty __instance)
        {
            object marker;if(ready.TryGetValue(__instance,out marker))return;
            PatchCopy(__instance);ready.Add(__instance,new object());
        }
        public static void Synchronize()
        {
            var seen=new HashSet<ThinkNode>();
            foreach(var tree in DefDatabase<ThinkTreeDef>.AllDefsListForReading)
                foreach(var node in Walk(tree.thinkRoot))if(seen.Add(node)&&node is ThinkNode_Duty)PatchCopy((ThinkNode_Duty)node);
        }
    }
}
