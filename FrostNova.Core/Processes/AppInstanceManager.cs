using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Processes
{
    public static class AppInstanceManager
    {
        public static bool IsExecuting(bool hasWindowProcess)
        {
            var p = System.Diagnostics.Process.GetCurrentProcess();

            var list = System.Diagnostics.Process.GetProcessesByName(p.ProcessName);

            if (list != null)
            {
                var p2 = list.FirstOrDefault(x => x.Id != p.Id);
                if (p2 == null) { return false; }

                var windowHandle = p2.MainWindowHandle;
                if (windowHandle != IntPtr.Zero)
                {
                    ActivateWindow(windowHandle);
                }
                return true;
            }

            return false;
        }


        private static void ActivateWindow(IntPtr hWnd)
        {
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, (int)WindowState.SW_NORAML);
            }
            SetForegroundWindow(hWnd);
        }



        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        private enum WindowState
        {
            SW_HIDE = 0,
            SW_NORAML = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,

        }

    }
}
