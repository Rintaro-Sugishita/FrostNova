using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    /// <summary>
    /// Registers file type associations under <c>Software\Classes</c> so that files with a given
    /// extension open with the specified application.
    /// </summary>
    /// <remarks>
    /// The plain methods (<see cref="Register"/> / <see cref="Unregister"/> / <see cref="IsRegistered"/>)
    /// operate **per-user** (<c>HKCU\Software\Classes</c>) and need no administrator rights. The
    /// <c>...ForAllUsers</c> variants write machine-wide (<c>HKLM\Software\Classes</c>) and require elevation.
    /// </remarks>
    public static class FileAssociationManager
    {
        // ---- current user (no administrator rights) ----

        /// <summary>
        /// Associates <paramref name="extension"/> with <paramref name="path"/> for the current user,
        /// then notifies the shell so icons refresh immediately.
        /// </summary>
        /// <param name="extension">The file extension including the leading dot, e.g. <c>.myext</c>.</param>
        /// <param name="id">A unique ProgID used as the intermediate key, e.g. <c>MyApp.Document</c>.</param>
        /// <param name="path">Absolute path to the executable that handles the file type.</param>
        /// <param name="description">Human-readable description shown in the Windows shell.</param>
        public static void Register(string extension, string id, string path, string description)
            => Register(extension, id, path, description, RegistrationScope.CurrentUser);

        /// <summary>
        /// Removes the current-user association created by <see cref="Register"/>. No-op if absent.
        /// </summary>
        public static void Unregister(string extension, string id)
            => Unregister(extension, id, RegistrationScope.CurrentUser);

        /// <summary>Returns whether the current-user association exists.</summary>
        public static bool IsRegistered(string extension, string id)
            => IsRegistered(extension, id, RegistrationScope.CurrentUser);

        // ---- all users / machine-wide (requires administrator rights) ----

        /// <summary>
        /// Machine-wide (all users) version of <see cref="Register"/>. **Requires administrator rights**;
        /// throws <see cref="UnauthorizedAccessException"/> when run without elevation.
        /// </summary>
        public static void RegisterForAllUsers(string extension, string id, string path, string description)
            => Register(extension, id, path, description, RegistrationScope.LocalMachine);

        /// <summary>Machine-wide version of <see cref="Unregister"/>. Requires administrator rights.</summary>
        public static void UnregisterForAllUsers(string extension, string id)
            => Unregister(extension, id, RegistrationScope.LocalMachine);

        /// <summary>Machine-wide version of <see cref="IsRegistered"/>.</summary>
        public static bool IsRegisteredForAllUsers(string extension, string id)
            => IsRegistered(extension, id, RegistrationScope.LocalMachine);

        // ---- core ----

        private static void Register(string extension, string id, string path, string description, RegistrationScope scope)
        {
            using (RegistryKey classes = scope.OpenClassesForWrite())
            {
                using (RegistryKey extKey = classes.CreateSubKey(extension))
                    extKey.SetValue("", id);

                using (RegistryKey idKey = classes.CreateSubKey(id))
                {
                    idKey.SetValue("", description);
                    using (RegistryKey icon = idKey.CreateSubKey("DefaultIcon"))
                        icon.SetValue("", $"\"{path}\",0");
                    using (RegistryKey cmd = idKey.CreateSubKey(@"Shell\Open\Command"))
                        cmd.SetValue("", $"\"{path}\" \"%1\"");
                }
            }
            NotifyAssociationsChanged();
        }

        private static void Unregister(string extension, string id, RegistrationScope scope)
        {
            using (RegistryKey classes = scope.OpenClassesForWrite())
            {
                classes.DeleteSubKeyTree(id, throwOnMissingSubKey: false);

                using (RegistryKey? extKey = classes.OpenSubKey(extension, writable: true))
                {
                    // Only clear the extension if it still points to *our* ProgID, so we never
                    // clobber an association another application may have taken over.
                    if (extKey != null && (extKey.GetValue("") as string) == id)
                        extKey.SetValue("", "");
                }
            }
            NotifyAssociationsChanged();
        }

        private static bool IsRegistered(string extension, string id, RegistrationScope scope)
        {
            using (RegistryKey? classes = scope.OpenClassesForRead())
            {
                if (classes == null) return false;
                using (RegistryKey? extKey = classes.OpenSubKey(extension))
                using (RegistryKey? idKey = classes.OpenSubKey(id))
                {
                    if (extKey == null || idKey == null) return false;
                    return (extKey.GetValue("") as string) == id;
                }
            }
        }

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        /// <summary>
        /// Tells the Windows shell that file associations changed, so Explorer refreshes icons and
        /// handlers without waiting for a reboot. Called automatically by the register/unregister methods.
        /// </summary>
        public static void NotifyAssociationsChanged()
            => SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}
