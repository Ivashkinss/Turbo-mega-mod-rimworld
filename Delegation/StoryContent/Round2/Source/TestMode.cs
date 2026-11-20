using System;

namespace TurboMegaMod
{
    /// <summary>
    /// Cached check for the -tmm-story-test launch argument.
    /// Every test-only hook across the mod MUST check TestMode.Enabled so test switches
    /// are completely inert during normal gameplay.
    /// </summary>
    public static class TestMode
    {
        private static readonly bool s_enabled = DetectTestFlag();

        public static bool Enabled
        {
            get { return s_enabled; }
        }

        private static bool DetectTestFlag()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-tmm-story-test", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
