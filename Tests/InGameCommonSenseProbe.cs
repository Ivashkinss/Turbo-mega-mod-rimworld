// Isolated tests of the original component and its actual game objects.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

public sealed class CommonSenseProbe : MapComponent
{
    private bool reported, uiReported;
    public CommonSenseProbe(Map map) : base(map) { }
    private static string Escape(string s) { return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
    private static void Check(bool v,string s) { if(!v)throw new Exception(s); }
    private static Verse.Mod GetMod() { return LoadedModManager.ModHandles.Single(m=>m.GetType().FullName=="CommonSense.CommonSense"); }
    public override void MapComponentUpdate()
    {
        if(reported||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-commonsense-probe"))return;
        reported=true;var errors=new List<string>();int checks=0,patches=0;
        var saved=new Dictionary<FieldInfo,object>();ModSettings settings=null;
        try
        {
            var mod=GetMod();var assembly=mod.GetType().Assembly;
            Check(AppDomain.CurrentDomain.GetAssemblies().Count(asm=>asm.GetName().Name=="CommonSense")==1,"Duplicate DLL");checks++;
            bool original=ModsConfig.IsActive("avilmask.commonsense");string id=original?"avilmask.commonsense":"colonyproject.integrated";
            Check(mod.Content.PackageId==id,"Wrong content pack");checks++;
            string root=LoadedModManager.RunningModsListForReading.Single(m=>m.PackageId==id).RootDir;
            string path=Path.Combine(root,original?"1.6/Assemblies/CommonSense.dll":"Components/CommonSense/Assemblies/CommonSense.dll");
            Check(string.Equals(Path.GetFullPath(assembly.Location),Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase),"Wrong DLL origin");checks++;
            patches=Harmony.GetAllPatchedMethods().Count(m=>{var p=Harmony.GetPatchInfo(m);return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Any(x=>x.PatchMethod.Module.Assembly==assembly);});
            Check(patches>=15,"Incomplete gameplay patches");checks++;
            Type st=AccessTools.TypeByName("CommonSense.Settings");
            settings=(ModSettings)typeof(Verse.Mod).GetMethods().Single(m=>m.Name=="GetSettings"&&m.IsGenericMethodDefinition).MakeGenericMethod(st).Invoke(mod,null);
            foreach(var f in st.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(f=>!f.IsInitOnly&&(f.FieldType==typeof(bool)||f.FieldType==typeof(int))))saved[f]=f.GetValue(f.IsStatic?null:settings);
            Check(saved.Count>=33,"Missing settings");checks++;
            var separate=AccessTools.Field(st,"separate_meals");separate.SetValue(separate.IsStatic?null:settings,true);
            string file=Path.Combine(GenFilePaths.SaveDataFolderPath,"CommonSenseSettings.xml");
            Scribe.saver.InitSaving(file,"Settings");settings.ExposeData();Scribe.saver.FinalizeSaving();
            separate.SetValue(separate.IsStatic?null:settings,false);
            Scribe.loader.InitLoading(file);settings.ExposeData();Scribe.loader.FinalizeLoading();
            Check((bool)separate.GetValue(separate.IsStatic?null:settings),"Settings Scribe failed");checks++;
            Thing a=ThingMaker.MakeThing(ThingDefOf.MealSimple),b=ThingMaker.MakeThing(ThingDefOf.MealSimple);
            var ia=a.TryGetComp<CompIngredients>();var ib=b.TryGetComp<CompIngredients>();
            ia.ingredients.Clear();ib.ingredients.Clear();ia.ingredients.Add(DefDatabase<ThingDef>.GetNamed("RawRice"));ib.ingredients.Add(DefDatabase<ThingDef>.GetNamed("RawRice"));
            Check(a.CanStackWith(b),"Matching meals cannot stack");checks++;
            ib.ingredients.Clear();ib.ingredients.Add(DefDatabase<ThingDef>.GetNamed("Meat_Human"));
            Check(!a.CanStackWith(b),"Cannibal and ordinary meals mixed with separation enabled");checks++;
            var stackPatch=AccessTools.Method(AccessTools.TypeByName("CommonSense.CompIngredients_CanStackWith_CommonSensePatch"),"Postfix");
            object[] enabledStackInput={true,a,b};stackPatch.Invoke(null,enabledStackInput);
            Check(!(bool)enabledStackInput[0],"Enabled Common Sense food filter did not reject different ingredient categories");checks++;
            Check(Harmony.GetAllPatchedMethods().Any(m=>{var p=Harmony.GetPatchInfo(m);return p.Postfixes.Any(x=>x.PatchMethod==stackPatch);}),"Meal filtering postfix was not installed");checks++;
            separate.SetValue(separate.IsStatic?null:settings,false);
            // Vanilla 1.7 can itself reject these meals. With the feature off,
            // the original Common Sense postfix must preserve its input.
            object[] stackInput={true,a,b};
            stackPatch.Invoke(null,stackInput);
            Check((bool)stackInput[0],"Disabled meal separation changed the incoming stacking decision");checks++;
            var cleanFlag=AccessTools.Field(st,"clean_gizmo");cleanFlag.SetValue(cleanFlag.IsStatic?null:settings,true);
            var bench=(ThingWithComps)ThingMaker.MakeThing(ThingDefOf.TableButcher,ThingDefOf.WoodLog);bench.SetFaction(Faction.OfPlayer);
            var comp=bench.AllComps.Single(c=>c.GetType().FullName=="CommonSense.DoCleanComp");
            Check(bench.def.comps.Count(c=>c.compClass==comp.GetType())==1,"Duplicate/missing cleaning comp");checks++;
            var active=AccessTools.Property(comp.GetType(),"Active");bool before=(bool)active.GetValue(comp,null);
            var command=comp.CompGetGizmosExtra().OfType<Command_Toggle>().Single();
            Check(command.defaultLabel==Translator.Translate("DoCleanCompToggleLabel").RawText,"Cleaning label untranslated");checks++;
            command.toggleAction();Check((bool)active.GetValue(comp,null)!=before,"Cleaning toggle did not change state");checks++;
            string cp=Path.Combine(GenFilePaths.SaveDataFolderPath,"CleaningComp.xml");
            Scribe.saver.InitSaving(cp,"CleaningComp");comp.PostExposeData();Scribe.saver.FinalizeSaving();active.SetValue(comp,before,null);
            Scribe.loader.InitLoading(cp);comp.PostExposeData();Scribe.loader.FinalizeLoading();Check((bool)active.GetValue(comp,null)!=before,"Cleaning toggle Scribe failed");checks++;
            foreach(string name in new[]{"UnloadMarkedItems","SocialRelaxCommonSense"})
            {
                Job job=new Job(DefDatabase<JobDef>.GetNamed(name));Check(job.def.driverClass.Assembly==assembly,"Wrong job driver");checks++;
                string jp=Path.Combine(GenFilePaths.SaveDataFolderPath,name+".xml");Scribe.saver.InitSaving(jp,"Job");Scribe_Deep.Look(ref job,"job");Scribe.saver.FinalizeSaving();job=null;
                Scribe.loader.InitLoading(jp);Scribe_Deep.Look(ref job,"job");Scribe.loader.FinalizeLoading();Check(job!=null&&job.def.defName==name,"Job template Scribe failed");checks++;
            }
            foreach(string icon in new[]{"Unload_Thing","Unload_Thing_Cancel"}){Check(ContentFinder<Texture2D>.Get("UI/Icons/"+icon,false)!=null,"Missing unload icon");checks++;}
        }
        catch(Exception e){errors.Add(e.ToString());}
        finally{foreach(var p in saved)p.Key.SetValue(p.Key.IsStatic?null:settings,p.Value);}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"CommonSenseProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"patchedMethods\":"+patches+",\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");
    }
    public override void MapComponentOnGUI()
    {
        if(uiReported||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-commonsense-probe")||Event.current.type!=EventType.Repaint)return;
        uiReported=true;string error=null;GUI.BeginGroup(new Rect(50,50,900,650));
        try{GetMod().DoSettingsWindowContents(new Rect(0,0,900,650));}catch(Exception e){error=e.ToString();}finally{GUI.EndGroup();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"CommonSenseUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"settingsDrawInvokedInRepaint\":true,\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
