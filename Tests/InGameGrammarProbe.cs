using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Grammar;

public sealed class OdysseyGrammarProbe : MapComponent
{
    private bool done;
    public OdysseyGrammarProbe(Map map):base(map){}
    public override void MapComponentUpdate()
    {
        if(done||Find.CurrentMap!=map||!Environment.GetCommandLineArgs().Contains("-tmm-grammar-probe"))return;
        done=true;var errors=new List<string>();int checks=0;
        bool active=ModsConfig.IsActive("ludeon.rimworld.odyssey");
        if(active){
            try{
                var weapon=DefDatabase<RulePackDef>.GetNamed("NamerUniqueWeapon");
                var pack=AccessTools.Field(weapon.GetType(),"rulePack").GetValue(weapon);
                var lines=(IEnumerable<string>)AccessTools.Field(pack.GetType(),"rulesStrings").GetValue(pack);
                foreach(var line in lines){
                    var rule=new Rule_String(line);
                    if(string.IsNullOrEmpty(rule.keyword)||line.IndexOf("->",line.IndexOf("->",StringComparison.Ordinal)+2,StringComparison.Ordinal)>=0)
                        throw new Exception("Malformed unique-weapon rule: "+line);
                    checks++;
                }
            }catch(Exception ex){errors.Add(ex.ToString());}
        }
        if(active)foreach(var pair in new[]{new[]{"NamerFactionTradersGuild","r_name"},new[]{"NamerGravship","r_name"}}){
            try{
                var def=DefDatabase<RulePackDef>.GetNamed(pair[0]);
                var pack=AccessTools.Field(def.GetType(),"rulePack").GetValue(def);
                var lines=(IEnumerable<string>)AccessTools.Field(pack.GetType(),"rulesStrings").GetValue(pack);
                if(lines.Any(s=>!s.Contains("->")))throw new Exception("Malformed runtime rule in "+pair[0]);checks++;
                for(int i=0;i<64;i++){
                    string name=NameGenerator.GenerateName(def,Enumerable.Empty<string>(),false,pair[1]);
                    if(string.IsNullOrWhiteSpace(name)||name.Contains("[")||name.Contains("]"))throw new Exception("Invalid generated name in "+pair[0]+": "+name);
                    checks++;
                }
            }catch(Exception ex){errors.Add(ex.ToString());}
        }
        Func<string,string> escape=s=>"\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"GrammarProbe.json"),"{\"passed\":"+(errors.Count==0?"true":"false")+",\"odysseyActive\":"+(active?"true":"false")+",\"checks\":"+checks+",\"errors\":["+string.Join(",",errors.Select(escape))+"]}");
    }
}
