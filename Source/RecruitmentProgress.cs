namespace TurboMegaMod
{
    public struct RecruitWorkState
    {
        public int waitingPenalty, adaptationBonus, firstAssignedTick;
    }

    public static class RecruitmentProgress
    {
        public const int DayTicks = 60000;

        public static RecruitWorkState Evaluate(bool joined, bool eligible, bool assigned, int now, int firstAssigned)
        {
            var result = new RecruitWorkState { firstAssignedTick = firstAssigned };
            if (!joined || !eligible) return result;
            if (!assigned) { result.waitingPenalty = -50; return result; }
            if (result.firstAssignedTick < 0) result.firstAssignedTick = now;
            long age = System.Math.Max(0L, (long)now - result.firstAssignedTick);
            result.adaptationBonus = age < 5L * DayTicks ? 15 : age < 10L * DayTicks ? 10 : age < 15L * DayTicks ? 5 : 0;
            return result;
        }
    }
}
