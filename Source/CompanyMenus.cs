using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public static class CompanyMenus
    {
        public static IEnumerable<Gizmo> Gizmos(Building_ComplexDesk desk)
        {
            yield return new Command_Toggle { defaultLabel = "TMM.Company.Open".Translate(), defaultDesc = "TMM.Company.Help".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Company/Company"), isActive = () => desk.companyOpen, toggleAction = () => desk.companyOpen = !desk.companyOpen };
            yield return new Command_Action { defaultLabel = "TMM.Company.Price".Translate(desk.companyPercent), defaultDesc = "TMM.Company.PriceHelp".Translate(), icon = ThingDefOf.Silver.uiIcon,
                action = () => Find.WindowStack.Add(new FloatMenu(new[] {50, 75, 100, 125, 150, 200}.Select(p => new FloatMenuOption(p + "%", () => desk.companyPercent = p)).ToList())) };
            yield return new Command_Action { defaultLabel = "TMM.Company.Accounts".Translate(), defaultDesc = "TMM.Company.AccountsHelp".Translate(), icon = ThingDefOf.Silver.uiIcon,
                action = delegate {
                    var accounts = Current.Game.GetComponent<CompanyAccounts>();
                    var options = accounts.wallets.Where(w => w.pawn != null).Select(w => new FloatMenuOption("TMM.Company.Balance".Translate(w.pawn.LabelShortCap,w.earned,Current.Game.GetComponent<WorkBuyouts>().Status(w.pawn)),
                        delegate {
                            Messages.Message("TMM.Company.Balance".Translate(w.pawn.LabelShortCap,w.earned,Current.Game.GetComponent<WorkBuyouts>().Status(w.pawn)),MessageTypeDefOf.NeutralEvent,false);
                        })).ToList();
                    if (options.Count == 0) { Messages.Message("TMM.Company.Empty".Translate(), MessageTypeDefOf.NeutralEvent, false); return; }
                    Find.WindowStack.Add(new FloatMenu(options));
                } };
            yield return new Command_Action { defaultLabel = "TMM.Company.Stop".Translate(), defaultDesc = "TMM.Company.StopHelp".Translate(), icon = ContentFinder<Texture2D>.Get("UI/Company/Company"),
                action = delegate {
                    var service = desk.Map.GetComponent<CompanyService>();
                    var options = service.orders.Where(o => o.desk == desk && !o.completed && !o.cancelled).Select(o => new FloatMenuOption(o.guest.LabelShortCap + " — " + o.employee.LabelShortCap,
                        delegate { o.cancelled = true; service.Process(); })).ToList();
                    if (options.Count == 0) { Messages.Message("TMM.Company.NoSessions".Translate(), MessageTypeDefOf.NeutralEvent, false); return; }
                    Find.WindowStack.Add(new FloatMenu(options));
                } };
        }
    }
}
