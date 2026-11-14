using RimWorld;
using UnityEngine;
using Verse;
using Hospital.Utilities;

namespace Hospital.MainTab
{
    public class PawnColumnWorker_PatientRating : PawnColumnWorker_Text
    {
        protected internal float score;
        private float lastTimeCached;
        private HospitalMapComponent hospital;

        public override string GetTextFor(Pawn pawn)
        {
            if (Time.unscaledTime > lastTimeCached + 2 || Find.CurrentMap != hospital.map)
            {
                lastTimeCached = Time.unscaledTime;
                hospital = Find.CurrentMap.GetComponent<HospitalMapComponent>();
            }
            
            if (pawn.GetPatientRating(out score, hospital))
            {
                return Mathf.Clamp01(score).ToStringPercent();
            }

            return string.Empty;
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
        }
    }
}
