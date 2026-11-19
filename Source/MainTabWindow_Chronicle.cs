using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// «Хроника Искры»: current act, faction storylines, open and offered quests, decision log.
    public class MainTabWindow_Chronicle : MainTabWindow
    {
        private Vector2 scroll;
        private float viewHeight = 600f;

        public override Vector2 InitialSize { get { return new Vector2(760f, 640f); } }

        /// Lines the window shows; public so the probe can check the model without drawing.
        public static List<string> BuildLines(SparkStory s)
        {
            var lines = new List<string>();
            if (s == null) { lines.Add("TMM.Story.NoStory".Translate()); return lines; }
            lines.Add("## " + "TMM.Story.ActHeader".Translate(Mathf.Max(1, s.GetInt("act"))));
            lines.Add("TMM.Story.Holodisk".Translate(s.GetInt("holodisk"), 4));

            var active = s.Runs.Where(r => r.status == StoryQuestStatus.Active && r.Def != null).ToList();
            lines.Add("## " + "TMM.Story.ActiveHeader".Translate(active.Count));
            foreach (StoryQuestRun r in active)
            {
                StoryQuestDef d = r.Def;
                StoryContext ctx = s.Context(r);
                string obj = r.stage >= 0 && r.stage < d.stages.Count ? d.stages[r.stage].Objective(ctx) : "";
                lines.Add("• " + d.LabelCap + " — " + StoryUtil.GiverName(d) + (obj.NullOrEmpty() ? "" : ": " + obj));
            }

            var offered = s.Runs.Where(r => r.status == StoryQuestStatus.Offered && r.Def != null).ToList();
            if (offered.Count > 0)
            {
                lines.Add("## " + "TMM.Story.OfferedHeader".Translate());
                foreach (StoryQuestRun r in offered) lines.Add("• " + r.Def.LabelCap + " — " + StoryUtil.GiverName(r.Def));
            }

            var arcs = DefDatabase<StoryQuestDef>.AllDefs.Where(d => !d.faction.NullOrEmpty() && !d.main)
                .GroupBy(d => d.faction).ToList();
            if (arcs.Count > 0)
            {
                lines.Add("## " + "TMM.Story.ArcsHeader".Translate());
                foreach (var g in arcs.OrderBy(x => x.Key))
                {
                    Faction f = StoryUtil.FactionOf(g.Key);
                    if (f == null) continue;
                    int done = g.Count(d => s.IsDone(d.defName));
                    lines.Add("• " + f.Name + ": " + done + " / " + g.Count());
                }
            }

            var finished = s.Runs.Where(r => (r.status == StoryQuestStatus.Completed || r.status == StoryQuestStatus.Failed) && r.Def != null)
                .OrderByDescending(r => r.finishedTick).ToList();
            lines.Add("## " + "TMM.Story.LogHeader".Translate(finished.Count));
            foreach (StoryQuestRun r in finished)
                lines.Add("• " + r.Def.LabelCap + (r.status == StoryQuestStatus.Failed ? " (" + "TMM.Story.Failed".Translate().ToString() + ")" : "") +
                          (r.outcome.NullOrEmpty() ? "" : " → " + r.outcome));
            return lines;
        }

        public override void DoWindowContents(Rect inRect)
        {
            List<string> lines = BuildLines(SparkStory.Instance);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "TMM.Story.Title".Translate());
            Text.Font = GameFont.Small;
            Rect outRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            Rect view = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(outRect, ref scroll, view);
            float y = 0f;
            foreach (string line in lines)
            {
                bool header = line.StartsWith("## ");
                string shown = header ? line.Substring(3) : line;
                if (header) { y += 6f; GUI.color = new Color(0.95f, 0.8f, 0.45f); }
                float h = Text.CalcHeight(shown, view.width);
                Widgets.Label(new Rect(0f, y, view.width, h), shown);
                GUI.color = Color.white;
                y += h + 2f;
            }
            viewHeight = Mathf.Max(y + 10f, outRect.height);
            Widgets.EndScrollView();
        }
    }
}
