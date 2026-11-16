using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    public sealed class BedTariff : IExposable
    {
        public Building_Bed bed;
        public int rate;
        public void ExposeData(){Scribe_References.Look(ref bed,"bed");Scribe_Values.Look(ref rate,"rate");}
    }
    public sealed class ComplexPricing : MapComponent
    {
        public int salePercent=55;
        public List<BedTariff> bedTariffs=new List<BedTariff>();
        public ComplexPricing(Map map):base(map){}
        public override void ExposeData()
        {
            base.ExposeData();
            if(Scribe.mode==LoadSaveMode.Saving)Clean();
            Scribe_Values.Look(ref salePercent,"TMM_salePercent",55);Scribe_Collections.Look(ref bedTariffs,"TMM_bedTariffs",LookMode.Deep);
            if(Scribe.mode==LoadSaveMode.PostLoadInit){if(bedTariffs==null)bedTariffs=new List<BedTariff>();Clean();}
        }
        private void Clean(){salePercent=Math.Max(1,Math.Min(500,salePercent));bedTariffs.RemoveAll(t=>t==null||t.bed==null||t.bed.Destroyed);}
        public static int Percent(Thing item){var map=item==null?null:item.MapHeld;return map==null?55:Math.Max(1,Math.Min(500,map.GetComponent<ComplexPricing>().salePercent));}
        public BedTariff Override(Building_Bed bed){return bedTariffs.FirstOrDefault(t=>t!=null&&t.bed==bed);}
        public int Rate(Building_Bed bed,int fallback){var custom=Override(bed);return Math.Max(0,Math.Min(10000,custom==null?fallback:custom.rate));}
        public void Set(Building_Bed bed,int? rate)
        {
            if(bed==null)return;bedTariffs.RemoveAll(t=>t==null||t.bed==bed);
            if(rate.HasValue)bedTariffs.Add(new BedTariff{bed=bed,rate=Math.Max(0,Math.Min(10000,rate.Value))});
            var desk=ComplexLayout.Desk(map);if(desk!=null)desk.ApplyServiceAreas();
        }
        public void Transfer(Building_Bed before,Building_Bed after)
        {
            if(before==after||after==null)return;var entry=Override(before);if(entry==null)return;
            bedTariffs.RemoveAll(t=>t!=entry&&t!=null&&t.bed==after);entry.bed=after;
        }
        public IEnumerable<Building_Bed> Beds(Building_ComplexDesk desk)
        {
            return map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>().Where(b=>ComplexLayout.IsGuest(b)&&!b.Medical&&!b.ForPrisoners&&desk.area[b.Position]&&!ComplexLayout.StaffBed(desk,b)).OrderBy(b=>b.Position.z).ThenBy(b=>b.Position.x);
        }
        public void ApplyRoom(Building_ComplexDesk desk,Building_Bed source)
        {
            var room=source.GetRoom();if(room==null||room.UsesOutdoorTemperature)return;int rate=Rate(source,desk.roomRate);
            foreach(var bed in Beds(desk).Where(b=>b.GetRoom()==room).ToArray())Set(bed,rate);
        }
    }
    public sealed class Dialog_ComplexPrices : Window
    {
        private readonly Building_ComplexDesk desk;
        private readonly ComplexPricing prices;
        private Vector2 scroll;
        private string percentBuffer,baseBuffer;
        private readonly Dictionary<Building_Bed,string> buffers=new Dictionary<Building_Bed,string>();
        public Dialog_ComplexPrices(Building_ComplexDesk desk)
        {
            this.desk=desk;prices=desk.Map.GetComponent<ComplexPricing>();percentBuffer=prices.salePercent.ToString();baseBuffer=desk.roomRate.ToString();
            forcePause=true;doCloseX=true;doCloseButton=true;absorbInputAroundWindow=true;
        }
        public override Vector2 InitialSize {get{return new Vector2(1040,790);}}
        public override void DoWindowContents(Rect rect)
        {
            if(desk==null||!desk.Spawned||!desk.Active){Close();return;}
            Text.Font=GameFont.Medium;Widgets.Label(new Rect(0,0,rect.width,36),"TMM.Prices.Title".Translate());Text.Font=GameFont.Small;
            Widgets.Label(new Rect(0,44,440,32),"TMM.Prices.Sales".Translate());int percent=prices.salePercent;
            Widgets.TextFieldNumeric<int>(new Rect(445,44,95,32),ref percent,ref percentBuffer,1,500);prices.salePercent=percent;
            Widgets.Label(new Rect(550,44,rect.width-550,32),"TMM.Prices.Markup".Translate(percent-100));
            Widgets.Label(new Rect(0,88,440,32),"TMM.Prices.BaseBed".Translate());int common=desk.roomRate;
            Widgets.TextFieldNumeric<int>(new Rect(445,88,95,32),ref common,ref baseBuffer,0,10000);if(common!=desk.roomRate){desk.roomRate=common;desk.ApplyServiceAreas();}
            Widgets.Label(new Rect(0,133,rect.width,100),"TMM.Prices.Help".Translate());
            var beds=prices.Beds(desk).ToArray();var body=new Rect(0,242,rect.width,rect.height-292);var inner=new Rect(0,0,body.width-22,Math.Max(body.height,beds.Length*68));
            Widgets.BeginScrollView(body,ref scroll,inner);
            for(int i=0;i<beds.Length;i++){
                var bed=beds[i];float y=i*68;var room=bed.GetRoom();string roomLabel=room==null?"":room.Role.LabelCap.ToString();
                Widgets.Label(new Rect(0,y,330,62),"TMM.Prices.Bed".Translate(bed.LabelCap,bed.SleepingSlotsCount,bed.Position.ToString(),roomLabel));
                int rate=prices.Rate(bed,desk.roomRate);string buffer;if(!buffers.TryGetValue(bed,out buffer))buffer=rate.ToString();int edited=rate;
                Widgets.TextFieldNumeric<int>(new Rect(340,y+10,85,32),ref edited,ref buffer,0,10000);buffers[bed]=buffer;if(edited!=rate)prices.Set(bed,edited);
                Widgets.Label(new Rect(435,y+10,90,32),(prices.Override(bed)==null?"TMM.Prices.Common":"TMM.Prices.Custom").Translate());
                if(Widgets.ButtonText(new Rect(530,y+10,110,32),"TMM.Prices.Reset".Translate())){prices.Set(bed,null);buffers.Remove(bed);}
                if(room!=null&&!room.UsesOutdoorTemperature&&Widgets.ButtonText(new Rect(650,y+10,110,32),"TMM.Prices.Room".Translate())){prices.ApplyRoom(desk,bed);buffers.Clear();}
                TooltipHandler.TipRegion(new Rect(650,y+10,110,32),"TMM.Prices.RoomHelp".Translate());
                if(Widgets.ButtonText(new Rect(770,y+10,110,32),"TMM.Dossier.Focus".Translate()))CameraJumper.TryJump(bed);
            }
            if(beds.Length==0)Widgets.Label(new Rect(0,0,inner.width,64),"TMM.Prices.NoBeds".Translate());Widgets.EndScrollView();
        }
    }
}
