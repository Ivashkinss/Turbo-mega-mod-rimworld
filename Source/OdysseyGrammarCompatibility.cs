using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class OdysseyGrammarCompatibility
    {
        static OdysseyGrammarCompatibility()
        {
            Repair();
            var injected=AccessTools.Method(typeof(LoadedLanguage),"InjectIntoData_AfterImpliedDefs");
            if(injected!=null)new Harmony("turbomegamod.odyssey.russian.grammar").Patch(injected,
                postfix:new HarmonyMethod(typeof(OdysseyGrammarCompatibility),"Repair"));
        }
        public static void Repair()
        {
            var language=LanguageDatabase.activeLanguage;
            if(language==null||!language.folderName.StartsWith("Russian",StringComparison.OrdinalIgnoreCase)||!ModsConfig.IsActive("ludeon.rimworld.odyssey"))return;
            bool changed=Replace("NamerFactionTradersGuild","[tradeAdj_fem] [tradeNoun_fem]","r_name->[tradeAdj_fem] [tradeNoun_fem]");
            changed=Replace("NamerGravship","[AdjectiveLarge_fem] [thing_badass_fem]","narrative_name->[AdjectiveLarge_fem] [thing_badass_fem]")||changed;
            changed|=Replace("NamerGravship","subject(p=5)_gen->[NamePerson_gen]","subject_gen(p=5)->[NamePerson_gen]");
            changed|=Replace("NamerGravship","flying_adjective(p=2)_mas->летающий","flying_adjective_mas(p=2)->летающий");
            changed|=Replace("NamerGravship","flying_adjective(p=2)_fem->летающая","flying_adjective_fem(p=2)->летающая");
            changed|=Replace("NamerGravship","ship_word(p=5)_mas->корабль","ship_word_mas(p=5)->корабль");
            changed|=Replace("NamerGravship","ship_word(p=5)_mas->гравикорабль","ship_word_mas(p=5)->гравикорабль");
            changed|=Replace("NamerUniqueWeapon","weapon_noun(p=2)_mas->[weapon_type_mas]","weapon_noun_mas(p=2)->[weapon_type_mas]");
            changed|=Replace("NamerUniqueWeapon","weapon_noun(p=2)_fem->[weapon_type_fem]","weapon_noun_fem(p=2)->[weapon_type_fem]");
            changed|=Replace("NamerUniqueWeapon","weapon_noun(p=2)_neu->[weapon_type_neu]","weapon_noun_neu(p=2)->[weapon_type_neu]");
            changed|=Replace("NamerUniqueWeapon","weapon_adjective(p=2)_fem->[trait_adjective_fem]","weapon_adjective_fem(p=2)->[trait_adjective_fem]");
            changed|=Replace("NamerUniqueWeapon","weapon_adjective(p=2)_neu->[trait_adjective_neu]","weapon_adjective_neu(p=2)->[trait_adjective_neu]");
            changed|=SplitWeaponRule();
            if(!changed)return;
            // Definitions cache parsed strings and included rule packs. Clear
            // translated caches after changing a raw rule; English is intact.
            foreach(var def in DefDatabase<RulePackDef>.AllDefsListForReading){
                AccessTools.Field(typeof(RulePackDef),"cachedRules").SetValue(def,null);
                var pack=AccessTools.Field(typeof(RulePackDef),"rulePack").GetValue(def);
                if(pack!=null)AccessTools.Field(pack.GetType(),"rulesResolved").SetValue(pack,null);
            }
        }
        private static bool Replace(string name,string broken,string repaired)
        {
            var def=DefDatabase<RulePackDef>.GetNamedSilentFail(name);
            if(def==null)return false;
            var pack=AccessTools.Field(typeof(RulePackDef),"rulePack").GetValue(def);
            if(pack==null)return false;
            var lines=AccessTools.Field(pack.GetType(),"rulesStrings").GetValue(pack) as List<string>;
            if(lines==null)return false;
            bool changed=false;
            for(int i=0;i<lines.Count;i++)if(lines[i].Trim()==broken){lines[i]=repaired;changed=true;}
            return changed;
        }
        private static bool SplitWeaponRule()
        {
            var def=DefDatabase<RulePackDef>.GetNamedSilentFail("NamerUniqueWeapon");if(def==null)return false;
            var pack=AccessTools.Field(typeof(RulePackDef),"rulePack").GetValue(def);if(pack==null)return false;
            var lines=AccessTools.Field(pack.GetType(),"rulesStrings").GetValue(pack) as List<string>;if(lines==null)return false;
            const string joined="weapon_adjective(p=2)->[trait_adjective]weapon_adjective(p=2)_mas->[trait_adjective_mas]";
            bool changed=false;
            for(int i=0;i<lines.Count;i++)if(lines[i].Trim()==joined){
                lines[i]="weapon_adjective(p=2)->[trait_adjective]";
                lines.Insert(++i,"weapon_adjective_mas(p=2)->[trait_adjective_mas]");changed=true;
            }
            return changed;
        }
    }
}
