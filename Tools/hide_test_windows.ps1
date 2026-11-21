# Keeps test RimWorld windows out of the user's way: every second, any RimWorld process started by the test scripts
# (its command line contains this mod's TestRuns folder) gets its window moved off-screen and pushed to the bottom
# without being activated. The game keeps rendering there, so frame-based probes still work.
# A RimWorld the user starts themself is never touched.
# Usage (background): ./Tools/hide_test_windows.ps1 -Hours 10
param([double]$Hours = 10)
$ErrorActionPreference = 'Stop'
Add-Type -Namespace TmmWin -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr after, int x, int y, int cx, int cy, uint flags);
[DllImport("user32.dll")] public static extern bool GetWindowRect(System.IntPtr hWnd, out RECT r);
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
'@
$bottom = [IntPtr]1
$flags = 0x0001 -bor 0x0010  # SWP_NOSIZE | SWP_NOACTIVATE
$deadline = (Get-Date).AddHours($Hours)
$marker = 'Turbo-mega-mod-rimworld\TestRuns'
while ((Get-Date) -lt $deadline) {
    foreach ($proc in Get-CimInstance Win32_Process -Filter "Name='RimWorld.exe'" -ErrorAction SilentlyContinue) {
        if (!$proc.CommandLine -or $proc.CommandLine.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        $p = Get-Process -Id $proc.ProcessId -ErrorAction SilentlyContinue
        if (!$p -or $p.MainWindowHandle -eq [IntPtr]::Zero) { continue }
        $r = New-Object TmmWin.Native+RECT
        [void][TmmWin.Native]::GetWindowRect($p.MainWindowHandle, [ref]$r)
        if ($r.Left -gt -3000) { [void][TmmWin.Native]::SetWindowPos($p.MainWindowHandle, $bottom, -4000, -4000, 0, 0, $flags) }
    }
    Start-Sleep -Milliseconds 700
}
