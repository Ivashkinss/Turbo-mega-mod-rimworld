using System;

namespace TurboMegaMod
{
    public static class GuestDeathrestRules
    {
        public const float StartLevel=0.2f;
        public const int Day=60000,Allowance=500;
        // Current paid period must be valid. Count future charge boundaries,
        // including a small wake-up allowance; never debit money at admission.
        public static long Budget(int now,int nextDue,int duration,int rate)
        {
            if(now<0||nextDue<=now||duration<=0||rate<0)return -1;
            long end=(long)now+duration+Allowance;
            long charges=end<nextDue?0:(end-nextDue)/Day+1;
            return charges*rate;
        }
        public static bool Affordable(int silver,long budget){return silver>=0&&budget>=0&&silver>=budget;}
    }
}
