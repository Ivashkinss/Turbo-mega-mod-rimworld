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
                if (string.Equals(args[i], "-tmm-story-test", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[i], "-tmm-legendary-probe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[i], "-tmm-traffic-interaction-probe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[i], "-tmm-faction-events-probe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[i], "-tmm-vehicle-raid-probe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[i], "-tmm-institute-relay-probe", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Logs an exception caught by a rollback path. Exceptions deliberately injected by probes
        /// (message starts with "Simulated", test mode only) are logged as warnings so they do not
        /// count as runtime errors; everything else is a real error.
        /// </summary>
        public static void ReportException(string message, Exception ex)
        {
            if (s_enabled && ex != null && ex.Message != null && ex.Message.StartsWith("Simulated", StringComparison.Ordinal))
            {
                Verse.Log.Warning(message + "(simulated) " + ex.Message);
                return;
            }
            Verse.Log.Error(message + ex);
        }
    }
}
