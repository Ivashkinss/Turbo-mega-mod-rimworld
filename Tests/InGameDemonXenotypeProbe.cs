using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// In-game acceptance of the demon xenotype, stage 1 (appearance). Flag: -tmm-demon-xenotype-probe.
// Writes DemonXenotypeProbe.json and portrait images DemonPortrait_<rot>.bmp for visual review.
[StaticConstructorOnStartup]
public static class DemonXenotypeProbeLaunch
{
    static DemonXenotypeProbeLaunch()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (Find.Maps == null) return;
            Map home = Find.Maps.FirstOrDefault(m => m != null && m.IsPlayerHome);
            if (home != null && home.GetComponent<DemonXenotypeProbe>() == null) home.components.Add(new DemonXenotypeProbe(home));
        });
    }
}

public sealed class DemonXenotypeProbe : MapComponent
{
    private sealed class Check { public string name, expected, actual; public bool passed; }
    private readonly List<Check> checks = new List<Check>();
    private readonly List<string> facts = new List<string>();
    private bool done;
    private int stage;
    private int frames;
    private Pawn demon;

    private static readonly string[] AppearanceGenes = { "TMM_Headbone_DemonHorns", "TMM_Tail_Demon", "TMM_Eyes_DemonBlack" };
    private static readonly string[] AllTextures =
    {
        "Things/TMM/Xenotypes/Demon/DemonHorns", "Things/TMM/Xenotypes/Demon/DemonHornsSmall", "Things/TMM/Xenotypes/Demon/DemonHornsRam",
        "Things/TMM/Xenotypes/Demon/DemonTail", "Things/TMM/Xenotypes/Demon/DevilEye", "Things/TMM/Xenotypes/Demon/DemonEyeBlack"
    };

    public DemonXenotypeProbe(Map map) : base(map) { }

    private void C(string name, bool ok, object expected, object actual)
    {
        checks.Add(new Check { name = name, passed = ok, expected = Convert.ToString(expected), actual = Convert.ToString(actual) });
    }

    public override void MapComponentUpdate()
    {
        if (done || !map.IsPlayerHome || !Environment.GetCommandLineArgs().Contains("-tmm-demon-xenotype-probe")) return;
        try
        {
            if (stage == 0) { Prepare(); stage = 1; return; }
            if (stage == 1) { if (++frames < 90) return; Verify(); Finish(null); }
        }
        catch (Exception ex) { Finish(ex); }
    }

    private void Prepare()
    {
        C("BiotechActive", ModsConfig.BiotechActive, true, ModsConfig.BiotechActive);
        XenotypeDef xeno = DefDatabase<XenotypeDef>.GetNamedSilentFail("TMM_Demon");
        C("XenotypeLoaded", xeno != null, "TMM_Demon", xeno);
        foreach (string g in new[] { "TMM_Headbone_DemonHorns", "TMM_Headbone_DemonHornsSmall", "TMM_Headbone_DemonHornsRam", "TMM_Tail_Demon", "TMM_Eyes_Devil", "TMM_Eyes_DemonBlack" })
            C("GeneLoaded_" + g, DefDatabase<GeneDef>.GetNamedSilentFail(g) != null, true, DefDatabase<GeneDef>.GetNamedSilentFail(g) != null);
        // Every texture must really load in all three drawn rotations.
        foreach (string t in AllTextures)
            foreach (string r in new[] { "_south", "_north", "_east" })
            {
                Texture2D tex = ContentFinder<Texture2D>.Get(t + r, false);
                C("Texture" + t.Substring(t.LastIndexOf('/') + 1) + r, tex != null, "loaded", tex == null ? "missing" : tex.width + "x" + tex.height);
            }
        if (xeno == null) throw new Exception("TMM_Demon xenotype missing");
        demon = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
            canGeneratePawnRelations: false, forcedXenotype: xeno, developmentalStages: DevelopmentalStage.Adult));
        GenSpawn.Spawn(demon, CellFinder.RandomClosewalkCellNear(map.Center, map, 6), map);
    }

    private void Verify()
    {
        C("PawnIsDemon", demon.genes != null && demon.genes.Xenotype != null && demon.genes.Xenotype.defName == "TMM_Demon", "TMM_Demon", demon.genes == null || demon.genes.Xenotype == null ? "null" : demon.genes.Xenotype.defName);
        foreach (string g in AppearanceGenes)
        {
            bool has = demon.genes.GenesListForReading.Any(x => x.def.defName == g && x.Active);
            C("ActiveGene_" + g, has, "active", has ? "active" : "absent");
        }
        Gene skin = demon.genes.GenesListForReading.FirstOrDefault(x => x.Active && x.def.skinColorOverride != null);
        facts.Add("skinGene=" + (skin == null ? "none" : skin.def.defName));
        C("DemonSkinGene", skin != null && (skin.def.defName == "Skin_DeepRed" || skin.def.defName == "Skin_InkBlack"), "Skin_DeepRed|Skin_InkBlack", skin == null ? "none" : skin.def.defName);

        // Portraits without apparel so horns, eyes and tail are visible; saved for human review.
        int saved = 0;
        foreach (Rot4 rot in new[] { Rot4.South, Rot4.East, Rot4.North })
        {
            RenderTexture rt = PortraitsCache.Get(demon, new Vector2(256f, 256f), rot, default(Vector3), 1f, true, true, false, false);
            if (rt == null) continue;
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "DemonPortrait_" + rot.ToStringHuman() + ".bmp");
            SaveBmp(rt, path);
            if (File.Exists(path)) saved++;
        }
        C("PortraitsSaved", saved == 3, 3, saved);
    }

    private static void SaveBmp(RenderTexture rt, string path)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        Color32[] px = tex.GetPixels32();
        int w = tex.width, h = tex.height, size = 54 + w * h * 4;
        using (var bw = new BinaryWriter(File.Create(path)))
        {
            bw.Write((byte)'B'); bw.Write((byte)'M'); bw.Write(size); bw.Write(0); bw.Write(54);
            bw.Write(40); bw.Write(w); bw.Write(h); bw.Write((short)1); bw.Write((short)32); bw.Write(0); bw.Write(w * h * 4); bw.Write(2835); bw.Write(2835); bw.Write(0); bw.Write(0);
            // Composite on grey so transparent areas are readable.
            foreach (Color32 c in px)
            {
                float a = c.a / 255f;
                bw.Write((byte)(c.b * a + 112 * (1 - a))); bw.Write((byte)(c.g * a + 112 * (1 - a))); bw.Write((byte)(c.r * a + 112 * (1 - a))); bw.Write((byte)255);
            }
        }
        UnityEngine.Object.Destroy(tex);
    }

    private static string J(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""); }

    private void Finish(Exception ex)
    {
        done = true;
        bool passed = ex == null && checks.Count > 0 && checks.All(c => c.passed);
        var cj = checks.Select(c => string.Format("{{\"name\":\"{0}\",\"passed\":{1},\"expected\":\"{2}\",\"actual\":\"{3}\"}}", J(c.name), c.passed ? "true" : "false", J(c.expected), J(c.actual)));
        string json = "{\n  \"passed\": " + (passed ? "true" : "false") + ",\n  \"checksCount\": " + checks.Count +
            ",\n  \"facts\": [" + string.Join(",", facts.Select(f => "\"" + J(f) + "\"").ToArray()) + "],\n  \"checks\": [\n    " + string.Join(",\n    ", cj.ToArray()) +
            "\n  ],\n  \"errors\": [" + (ex == null ? "" : "\"" + J(ex.ToString()) + "\"") + "]\n}";
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DemonXenotypeProbe.json"), json);
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DemonXenotypeUiProbe.json"),
            "{\"passed\":" + (passed ? "true" : "false") + ",\"visualLayoutInspected\":false,\"note\":\"portraits saved as BMP for human review\"}");
    }
}
