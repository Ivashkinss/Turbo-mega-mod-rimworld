using System.Linq;
using HarmonyLib;
using Verse;

/// Probes close the pausing windows nobody is there to answer. A dialog with answers (a caravan meeting, a quest offer) is
/// answered with its last option instead, as a player would: vanilla gives such a window no other way out, and removing it
/// leaves whatever the answer was to settle (the met caravan's pawns, for one) without an owner.
public static class ProbeWindows
{
    public static string Dismiss(Window w)
    {
        var tree = w as Dialog_NodeTree;
        if (tree != null)
        {
            DiaNode node = (DiaNode)AccessTools.Field(typeof(Dialog_NodeTree), "curNode").GetValue(tree);
            DiaOption last = node == null ? null : node.options.LastOrDefault(o => !o.disabled);
            if (last != null)
            {
                if (last.action != null) last.action();
                if (Find.WindowStack.IsOpen(tree)) Find.WindowStack.TryRemove(tree, false);
                return w.GetType().Name + " answered '" + AccessTools.Field(typeof(DiaOption), "text").GetValue(last) + "'";
            }
        }
        w.Close(false);
        if (Find.WindowStack.IsOpen(w)) Find.WindowStack.TryRemove(w, false);
        return w.GetType().Name + " closed";
    }
}
