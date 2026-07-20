using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    /// <summary>
    /// Manages Windows auto-start entries under
    /// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>.
    /// </summary>
    public static class StartupManager
    {
        private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Registers the application to start automatically at Windows logon.
        /// </summary>
        /// <param name="appName">The registry value name used to identify the entry.</param>
        /// <param name="appPath">Absolute path to the executable. Defaults to the current process executable.</param>
        /// <param name="arguments">Optional command-line arguments appended after the executable path.</param>
        public static void RegisterStartup(string appName, string appPath = "", string arguments = "")
        {

            if (appPath == "")
            {
                appPath = System.Diagnostics.Process.GetCurrentProcess()?.MainModule?.FileName ?? "";
            }

            // パスをクォートで囲み、引数があれば後ろに付加する。
            string command = $"\"{appPath}\"";
            if (arguments != "")
            {
                command = $"\"{appPath}\" {arguments}";
            }

            using (RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true))
            {
                if (key == null) throw new InvalidOperationException("レジストリキーにアクセスできません。");
                key.SetValue(appName, command);
            }
        }

        /// <summary>Removes the auto-start entry for <paramref name="appName"/>. No-op if the entry does not exist.</summary>
        public static void UnregisterStartup(string appName)
        {
            using (RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true))
            {
                if (key == null) throw new InvalidOperationException("レジストリキーにアクセスできません。");
                key.DeleteValue(appName, throwOnMissingValue: false);
            }
        }

        /// <summary>Returns <see langword="true"/> if an auto-start entry with the given name exists.</summary>
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
