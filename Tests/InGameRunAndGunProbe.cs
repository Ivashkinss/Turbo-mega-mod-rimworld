// Isolated HugsLib ownership/settings, gizmo, comp save and real moving-shot probe.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

public sealed class RunAndGunProbe : MapComponent
{
    private bool reported,uiReported;
    private Stopwatch watch;
    private static Pawn shooter;
    private Pawn enemy;
    private static int movingShots;
    private int checks;
    private readonly List<string> errors=new List<string>();
    private readonly Harmony probeHarmony=new Harmony("tmm.diagnostic.runandgun");
    private MethodInfo shotMethod;
    private object forbidRecord;private FieldInfo forbidSelected;private object oldForbid;
    public RunAndGunProbe(Map map):base(map){}
    private static string Escape(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";}
    private static object Base(){return AccessTools.Property(AccessTools.TypeByName("RunAndGun.Base"),"Instance").GetValue(null,null);}
    private static object Read(object obj,string name){return AccessTools.Property(obj.GetType(),name).GetValue(obj,null);}
    private static object Handle(string name){return AccessTools.Field(AccessTools.TypeByName("RunAndGun.Base"),name).GetValue(null);}
    private static void Shot(Verb __instance,bool __result){if(__result&&__instance.CasterPawn==shooter&&shooter.pather.Moving)movingShots++;}
    private void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
    public override void MapComponentUpdate()
    {
        if(reported||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-runandgun-probe"))return;
        try
        {
            if(watch==null)
            {
                shooter=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(p=>!p.Dead&&!p.Downed&&!p.WorkTagIsDisabled(WorkTags.Violent));
                if(shooter==null)return;
                watch=Stopwatch.StartNew();var mod=Base();Check(mod!=null,"HugsLib did not initialize RunAndGun");
                var assembly=mod.GetType().Assembly;
                Check(AppDomain.CurrentDomain.GetAssemblies().Count(a=>a.GetName().Name=="RunAndGun")==1,"Duplicate DLL");
                bool original=ModsConfig.IsActive("roolo.runandgun.kotobike");string id=original?"roolo.runandgun.kotobike":"colonyproject.integrated";
                var pack=(ModContentPack)Read(mod,"ModContentPack");Check(pack.PackageId==id,"Wrong HugsLib owner");
                string expected=Path.Combine(pack.RootDir,original?"1.5/Assemblies/RunAndGun.dll":"Components/RunAndGun/Assemblies/RunAndGun.dll");
                Check(string.Equals(Path.GetFullPath(assembly.Location),Path.GetFullPath(expected),StringComparison.OrdinalIgnoreCase),"Unexpected DLL/version selection");
                Check((string)Read(mod,"ModIdentifier")=="RunAndGun","Changed HugsLib settings identifier");
                object settings=Read(mod,"Settings");Check((string)Read(settings,"ModId")=="RunAndGun","Wrong HugsLib settings pack");
                object accuracy=Handle("accuracyPenalty");var value=AccessTools.Property(accuracy.GetType(),"Value");object old=value.GetValue(accuracy,null);
                try
                {
                    value.SetValue(accuracy,25,null);var xml=new XElement("settings");AccessTools.Method(settings.GetType(),"WriteXml").Invoke(settings,new object[]{xml});
                    Check(xml.Descendants("accuracyPenalty").Any(x=>x.Value=="25"),"Accuracy setting not serialized");
                    var ctor=settings.GetType().GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).First();
                    object[] args=ctor.GetParameters().Select(p=>p.ParameterType==typeof(string)?(object)"RunAndGun":p.ParameterType.IsValueType?Activator.CreateInstance(p.ParameterType):null).ToArray();
                    object loaded=ctor.Invoke(args);AccessTools.Method(loaded.GetType(),"LoadFromXml").Invoke(loaded,new object[]{xml.Element("RunAndGun")});
                    Check((string)AccessTools.Method(loaded.GetType(),"PeekValue").Invoke(loaded,new object[]{"accuracyPenalty"})=="25","HugsLib settings parse failed");
                }
                finally{value.SetValue(accuracy,old,null);}
                int patches=Harmony.GetAllPatchedMethods().Count(m=>{var info=Harmony.GetPatchInfo(m);return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Any(p=>p.PatchMethod.Module.Assembly==assembly);});Check(patches>=6,"Missing shooting hooks");
                var comp=shooter.AllComps.Single(c=>c.GetType().FullName=="RunAndGun.CompRunAndGun");var enabled=AccessTools.Field(comp.GetType(),"isEnabled");
                Check(shooter.def.comps.Count(c=>c.compClass==comp.GetType())==1,"Wrong number of pawn comps");
                shooter.equipment.DestroyAllEquipment();var weapon=(ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gun_Autopistol"));shooter.equipment.AddEquipment(weapon);
                var dictionary=(IDictionary)Read(Read(Handle("weaponForbidder"),"Value"),"InnerList");forbidRecord=dictionary[weapon.def.defName];forbidSelected=AccessTools.Field(forbidRecord.GetType(),"isSelected");oldForbid=forbidSelected.GetValue(forbidRecord);forbidSelected.SetValue(forbidRecord,false);
                shooter.drafter.Drafted=true;shooter.drafter.FireAtWill=true;
                var command=shooter.GetGizmos().OfType<Command_Toggle>().Single(g=>g.defaultLabel==Translator.Translate("RG_Action_Enable_Label").RawText);
                bool previous=(bool)enabled.GetValue(comp);command.toggleAction();Check((bool)enabled.GetValue(comp)!=previous,"Mode command did not toggle");enabled.SetValue(comp,true);
                string save=Path.Combine(GenFilePaths.SaveDataFolderPath,"RunAndGunComp.xml");Scribe.saver.InitSaving(save,"Comp");comp.PostExposeData();Scribe.saver.FinalizeSaving();enabled.SetValue(comp,false);
                Scribe.loader.InitLoading(save);comp.PostExposeData();Scribe.loader.FinalizeLoading();Check((bool)enabled.GetValue(comp),"Pawn mode Scribe failed");
                Check(command.icon!=null,"Missing command icon");
                IntVec3 targetCell=CellFinder.RandomClosewalkCellNear(shooter.Position,map,6);
                enemy=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfAncientsHostile);GenSpawn.Spawn(enemy,targetCell,map);
                enemy.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait),JobCondition.InterruptForced);
                Verb verb=shooter.TryGetAttackVerb(enemy);Check(verb!=null&&!verb.IsMeleeAttack,"No ranged verb");
                shotMethod=AccessTools.Method(verb.GetType(),"TryCastShot");probeHarmony.Patch(shotMethod,postfix:new HarmonyMethod(typeof(RunAndGunProbe),"Shot"));
                IntVec3 destination=CellFinder.RandomClosewalkCellNear(shooter.Position,map,35);
                shooter.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Goto,destination),JobCondition.InterruptForced);
            }
            if(shooter.pather.Moving&&enemy!=null&&!enemy.Dead&&Find.TickManager.TicksGame%10==0)shooter.TryStartAttack(enemy);
            if(movingShots>0){Check(true,"Moving projectile");Finish();}
            else if(watch.Elapsed.TotalSeconds>20){errors.Add("No successful shot while moving within 20 seconds");Finish();}
        }
        catch(Exception e){errors.Add(e.ToString());Finish();}
    }
    private void Finish()
    {
        reported=true;if(shotMethod!=null)probeHarmony.Unpatch(shotMethod,HarmonyPatchType.All,"tmm.diagnostic.runandgun");
        if(forbidSelected!=null)forbidSelected.SetValue(forbidRecord,oldForbid);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"RunAndGunProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"checks\":"+checks+",\"successfulShotsWhileMoving\":"+movingShots+",\"attackRequestedByTest\":true,\"errors\":["+string.Join(",",errors.Select(Escape))+"]}");
    }
    public override void MapComponentOnGUI()
    {
        if(uiReported||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-runandgun-probe")||Event.current.type!=EventType.Repaint)return;
        uiReported=true;string error=null;GUI.BeginGroup(new Rect(50,50,900,650));
        try
        {
            var handles=(IEnumerable)Read(Read(Base(),"Settings"),"Handles");int drawers=0;
            foreach(object h in handles){var d=Read(h,"CustomDrawer") as Delegate;if(d!=null){d.DynamicInvoke(new Rect(0,0,900,600));drawers++;}}
            if(drawers<3)throw new Exception("Missing custom HugsLib controls");
        }
        catch(Exception e){error=e.ToString();}finally{GUI.EndGroup();}
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"RunAndGunUiProbe.json"),"{\"passed\":"+(error==null?"true":"false")+",\"customControlsDrawnInRepaint\":true,\"visualLayoutInspected\":false,\"error\":"+(error==null?"null":Escape(error))+"}");
    }
}
