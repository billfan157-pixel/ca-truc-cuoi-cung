function Show-CtccPlayerWindow([Diagnostics.Process]$Player) {
    if (!('CtccPlayerWindow' -as [type])) {
        Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class CtccPlayerWindow
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, IntPtr process);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint caller, uint target, bool attach);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr order, int x, int y, int width, int height, uint flags);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    public static void Show(IntPtr window)
    {
        uint caller = GetCurrentThreadId();
        uint foreground = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        bool attached = foreground != 0 && foreground != caller && AttachThreadInput(caller, foreground, true);
        try
        {
            // Keep this short-lived benchmark window visible across repeated launches without resizing it.
            SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 0x0043);
            SetForegroundWindow(window);
        }
        finally { if (attached) AttachThreadInput(caller, foreground, false); }
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
            if ([CtccPlayerWindow]::GetForegroundWindow() -eq $Player.MainWindowHandle) { return }
        }
        Start-Sleep -Milliseconds 200
    }
}
