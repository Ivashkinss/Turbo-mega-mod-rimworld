using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// Art helper, not a gameplay test: exports vanilla body, head and sample apparel textures (packed inside the game)
    /// as raw RGBA files so they can serve as fitting templates for new apparel art.
    /// Activated with -tmm-texture-export. Files: TextureExport/&lt;name&gt;.rgba (int width, int height, then RGBA bytes).
    /// </summary>
    public sealed class InGameTextureExportProbe : MapComponent
    {
        private static bool s_done;
        private int m_ticks;

        public InGameTextureExportProbe(Map map) : base(map) { }

        private static bool Enabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++) if (string.Equals(args[i], "-tmm-texture-export", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
        }

        public override void MapComponentTick()
        {
            if (s_done || !Enabled || map != Find.AnyPlayerHomeMap) return;
            if (++m_ticks < 30) return;
            s_done = true;
            List<string> ok = new List<string>();
            List<string> missing = new List<string>();
            string dir = Path.Combine(GenFilePaths.SaveDataFolderPath, "TextureExport");
            Directory.CreateDirectory(dir);
            string[] bodies = { "Male", "Female", "Thin", "Hulk", "Fat" };
            string[] rots = { "south", "north", "east" };
            List<string> paths = new List<string>();
            foreach (string b in bodies)
                foreach (string r in rots)
                {
                    paths.Add("Things/Pawn/Humanlike/Bodies/Naked_" + b + "_" + r);
                    paths.Add("Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic_" + b + "_" + r);
                    paths.Add("Things/Pawn/Humanlike/Apparel/Duster/Duster_" + b + "_" + r);
                    paths.Add("Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor_" + b + "_" + r);
                }
            foreach (string r in rots)
            {
                paths.Add("Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal_" + r);
                paths.Add("Things/Pawn/Humanlike/Apparel/CowboyHat/CowboyHat_" + r);
                paths.Add("Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet_" + r);
            }
            foreach (string p in paths)
            {
                try
                {
                    Texture2D tex = ContentFinder<Texture2D>.Get(p, false);
                    if (tex == null) { missing.Add(p); continue; }
                    RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                    RenderTexture prev = RenderTexture.active;
                    Graphics.Blit(tex, rt);
                    RenderTexture.active = rt;
                    Texture2D copy = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
                    copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                    copy.Apply();
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    Color32[] px = copy.GetPixels32();
                    string name = p.Substring(p.LastIndexOf('/') + 1);
                    using (BinaryWriter bw = new BinaryWriter(File.Create(Path.Combine(dir, name + ".rgba"))))
                    {
                        bw.Write(copy.width); bw.Write(copy.height);
                        // Unity rows go bottom-up; write top-down so the file reads like an image.
                        for (int y = copy.height - 1; y >= 0; y--)
                            for (int x = 0; x < copy.width; x++)
                            {
                                Color32 c = px[y * copy.width + x];
                                bw.Write(c.r); bw.Write(c.g); bw.Write(c.b); bw.Write(c.a);
                            }
                    }
                    UnityEngine.Object.Destroy(copy);
                    ok.Add(name + " " + tex.width + "x" + tex.height);
                }
                catch (Exception ex) { missing.Add(p + " (" + ex.GetType().Name + ")"); }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"passed\":").Append(ok.Count > 0 ? "true" : "false").Append(",\"exported\":").Append(ok.Count).Append(",\"missing\":[");
            for (int i = 0; i < missing.Count; i++) { if (i > 0) sb.Append(','); sb.Append('"').Append(missing[i].Replace("\"", "'")).Append('"'); }
            sb.Append("],\"files\":[");
            for (int i = 0; i < ok.Count; i++) { if (i > 0) sb.Append(','); sb.Append('"').Append(ok[i]).Append('"'); }
            sb.Append("]}");
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "TextureExportProbe.json"), sb.ToString());
            File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "TextureExportUiProbe.json"), "{\"passed\":" + (ok.Count > 0 ? "true" : "false") + "}");
        }
    }
}
