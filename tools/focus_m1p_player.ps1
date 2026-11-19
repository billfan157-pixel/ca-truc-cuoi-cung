function Show-CtccPlayerWindow([Diagnostics.Process]$Player, [switch]$CalibrationClick) {
    if (!('CtccPlayerWindow' -as [type])) {
        Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class CtccPlayerWindow
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int x, y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int left, top, right, bottom; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint caller, uint target, bool attach);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr order, int x, int y, int width, int height, uint flags);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);

    public static bool IsForegroundProcess(int process)
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out uint owner);
        return owner == (uint)process;
    }

    public static void Show(IntPtr window)
    {
        uint caller = GetCurrentThreadId();
        uint foreground = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        bool attached = foreground != 0 && foreground != caller && AttachThreadInput(caller, foreground, true);
        try
        {
            // Keep this short-lived benchmark window visible across repeated launches without resizing it.
            SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 0x0043);
            SetForegroundWindow(window);
        }
        finally { if (attached) AttachThreadInput(caller, foreground, false); }
    }

    public static bool ClickBenchmarkWindow(IntPtr window, int process)
    {
        if (!GetWindowRect(window, out var rect) || !GetCursorPos(out var prior)) return false;
        // Direct foreground requests can be refused by Windows. Click only a pixel
        // currently owned by this freshly started benchmark player, never a covering app.
        foreach (var fraction in new[] { (17, 19), (83, 19), (25, 79), (80, 78) })
        {
            var point = new Point { x = rect.left + (rect.right - rect.left) * fraction.Item1 / 100,
                                    y = rect.top + (rect.bottom - rect.top) * fraction.Item2 / 100 };
            GetWindowThreadProcessId(WindowFromPoint(point), out uint owner);
            if (owner != (uint)process || !SetCursorPos(point.x, point.y)) continue;
            try
            {
                GetWindowThreadProcessId(WindowFromPoint(point), out owner);
                if (owner != (uint)process) continue;
                mouse_event(2, 0, 0, 0, UIntPtr.Zero);
                mouse_event(4, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(65);
                return true;
            }
            finally { SetCursorPos(prior.x, prior.y); }
        }
        return false;
    }
}
'@
    }
    $activator = New-Object -ComObject WScript.Shell
    for ($attempt = 0; $attempt -lt 16 -and !$Player.HasExited; $attempt++) {
        $Player.Refresh()
        if ($Player.MainWindowHandle -ne [IntPtr]::Zero) {
            [void]$activator.AppActivate($Player.Id)
            [CtccPlayerWindow]::Show($Player.MainWindowHandle)
            if ([CtccPlayerWindow]::IsForegroundProcess($Player.Id)) { return }
            if ($CalibrationClick -and [CtccPlayerWindow]::ClickBenchmarkWindow($Player.MainWindowHandle, $Player.Id)) {
                Start-Sleep -Milliseconds 125
                if ([CtccPlayerWindow]::IsForegroundProcess($Player.Id)) { return }
            }
        }
        Start-Sleep -Milliseconds 200
    }
}
