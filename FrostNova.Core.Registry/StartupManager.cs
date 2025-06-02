using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    public static class StartupManager
    {
        private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void RegisterStartup(string appName, string appPath = "", string arguments = "")
        {

            if (appPath == "")
            {
                appPath = System.Diagnostics.Process.GetCurrentProcess()?.MainModule?.FileName ?? "";
            }

            string command = $"\"{appPath}\""; 
            if (arguments!= "")
            {
                command = " {arguments}"; // パスをクォートで囲み、引数を追加
            }

            using (RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true))
            {
                if (key == null) throw new InvalidOperationException("レジストリキーにアクセスできません。");
                key.SetValue(appName, command);
            }
        }

        public static void UnregisterStartup(string appName)
        {
            using (RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true))
            {
                if (key == null) throw new InvalidOperationException("レジストリキーにアクセスできません。");
                key.DeleteValue(appName, throwOnMissingValue: false);
            }
        }

        public static bool IsRegistered(string appName)
        {
            using (RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKeyPath))
            {
                if (key == null) return false;
                return key.GetValue(appName) != null;
            }
        }
    }
}
