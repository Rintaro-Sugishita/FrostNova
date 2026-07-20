using System;
using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    /// <summary>Where a shell context-menu entry should appear.</summary>
    public enum ContextMenuTarget
    {
        /// <summary>Right-click menu of any file (<c>Software\Classes\*</c>). Command receives the file path via <c>%1</c>.</summary>
        AllFiles,

        /// <summary>Right-click menu of a folder (<c>Software\Classes\Directory</c>). Command receives the folder path via <c>%1</c>.</summary>
        Directory,

        /// <summary>Right-click menu of the empty area inside a folder (<c>Software\Classes\Directory\Background</c>). Command receives the folder path via <c>%V</c>.</summary>
        DirectoryBackground,
    }

    /// <summary>
    /// Adds and removes Windows Explorer shell context-menu entries (verbs) such as "Open with MyApp".
    /// </summary>
    /// <remarks>
    /// The plain methods operate **per-user** (no administrator rights). The <c>...ForAllUsers</c> variants
    /// write machine-wide and require elevation. The command line's path placeholder is chosen
    /// automatically: <c>%1</c> for files/folders, <c>%V</c> for a folder background.
    /// </remarks>
    public static class ContextMenuManager
    {
        // ---- current user (no administrator rights) ----

        /// <summary>Registers a context-menu verb for the current user.</summary>
        /// <param name="target">Which shell object the entry appears on.</param>
        /// <param name="verb">A unique key name for the entry, e.g. <c>OpenWithMyApp</c>.</param>
        /// <param name="menuText">The label shown in the context menu.</param>
        /// <param name="exePath">Absolute path to the executable to launch.</param>
        /// <param name="arguments">
        /// Optional argument string appended after the quoted executable. If omitted, the target's
        /// path placeholder (<c>"%1"</c> or <c>"%V"</c>) is used so the clicked item is passed in.
        /// </param>
        /// <param name="iconPath">Optional icon shown next to the menu item (e.g. <c>"C:\App\app.exe,0"</c>).</param>
        public static void Register(ContextMenuTarget target, string verb, string menuText, string exePath,
            string? arguments = null, string? iconPath = null)
            => Register(target, verb, menuText, exePath, arguments, iconPath, RegistrationScope.CurrentUser);

        /// <summary>Removes the current-user context-menu verb. No-op if it does not exist.</summary>
        public static void Unregister(ContextMenuTarget target, string verb)
            => Unregister(target, verb, RegistrationScope.CurrentUser);

        /// <summary>Returns whether the current-user context-menu verb is registered.</summary>
        public static bool IsRegistered(ContextMenuTarget target, string verb)
            => IsRegistered(target, verb, RegistrationScope.CurrentUser);

        // ---- all users / machine-wide (requires administrator rights) ----

        /// <summary>
        /// Machine-wide (all users) version of <see cref="Register"/>. **Requires administrator rights.**
        /// </summary>
        public static void RegisterForAllUsers(ContextMenuTarget target, string verb, string menuText, string exePath,
            string? arguments = null, string? iconPath = null)
            => Register(target, verb, menuText, exePath, arguments, iconPath, RegistrationScope.LocalMachine);

        /// <summary>Machine-wide version of <see cref="Unregister"/>. Requires administrator rights.</summary>
        public static void UnregisterForAllUsers(ContextMenuTarget target, string verb)
            => Unregister(target, verb, RegistrationScope.LocalMachine);

        /// <summary>Machine-wide version of <see cref="IsRegistered"/>.</summary>
        public static bool IsRegisteredForAllUsers(ContextMenuTarget target, string verb)
            => IsRegistered(target, verb, RegistrationScope.LocalMachine);

        // ---- core ----

        private static void Register(ContextMenuTarget target, string verb, string menuText, string exePath,
            string? arguments, string? iconPath, RegistrationScope scope)
        {
            string placeholder = target == ContextMenuTarget.DirectoryBackground ? "%V" : "%1";
            string argPart = arguments ?? $"\"{placeholder}\"";
            string command = $"\"{exePath}\" {argPart}";

            using (RegistryKey classes = scope.OpenClassesForWrite())
            using (RegistryKey verbKey = classes.CreateSubKey($@"{SubjectPath(target)}\shell\{verb}"))
            {
                verbKey.SetValue("", menuText);
                if (!string.IsNullOrEmpty(iconPath))
                    verbKey.SetValue("Icon", iconPath);

                using (RegistryKey cmdKey = verbKey.CreateSubKey("command"))
                    cmdKey.SetValue("", command);
            }
        }

        private static void Unregister(ContextMenuTarget target, string verb, RegistrationScope scope)
        {
            using (RegistryKey classes = scope.OpenClassesForWrite())
                classes.DeleteSubKeyTree($@"{SubjectPath(target)}\shell\{verb}", throwOnMissingSubKey: false);
        }

        private static bool IsRegistered(ContextMenuTarget target, string verb, RegistrationScope scope)
        {
            using (RegistryKey? classes = scope.OpenClassesForRead())
            {
                if (classes == null) return false;
                using (RegistryKey? key = classes.OpenSubKey($@"{SubjectPath(target)}\shell\{verb}"))
                    return key != null;
            }
        }

        private static string SubjectPath(ContextMenuTarget target) => target switch
        {
            ContextMenuTarget.AllFiles => "*",
            ContextMenuTarget.Directory => "Directory",
            ContextMenuTarget.DirectoryBackground => @"Directory\Background",
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };
    }
}
