using System;

namespace TurboMegaMod
{
    public static class GuestProfileRules
    {
        public const int MinimumStayTicks=15000,FirstBedDelay=250,BedRetryDelay=500;
        public static bool RollVip(int percent,double roll){return percent>0&&roll>=0&&roll<Math.Min(100,percent)/100.0;}
        public static bool Regular(int completed,int good){return completed>=3&&good>=2;}
        public static bool Qualifies(string reason,bool medicalOnly,int ticks){return reason=="departed"&&!medicalOnly&&ticks>=MinimumStayTicks;}
        public static bool Good(float score){return !float.IsNaN(score)&&score>=0.6f;}
        public static int CoinsToAdd(int money,int target,double freeSpace,double coinMass)
        {
            if(money<0||target<=money||double.IsNaN(freeSpace)||double.IsInfinity(freeSpace)||double.IsNaN(coinMass)||double.IsInfinity(coinMass)||coinMass<=0)return 0;
            double capacity=Math.Floor(Math.Max(0,freeSpace-3)/coinMass);
            return (int)Math.Min(Math.Min(int.MaxValue,capacity),(long)target-money);
        }
    }
}
