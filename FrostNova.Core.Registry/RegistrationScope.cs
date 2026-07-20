using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    /// <summary>
    /// Determines where shell-integration entries (file associations, context menus) are written.
    /// Implementation detail — the public API exposes per-user methods and separate
    /// <c>...ForAllUsers</c> methods instead of a scope parameter.
    /// </summary>
    internal enum RegistrationScope
    {
        /// <summary>
        /// Per-user, under <c>HKEY_CURRENT_USER\Software\Classes</c>. **No administrator rights required.**
        /// This is the recommended default for application-level integration.
        /// </summary>
        CurrentUser,

        /// <summary>
        /// All users, under <c>HKEY_LOCAL_MACHINE\Software\Classes</c>. Requires administrator rights.
        /// </summary>
        LocalMachine,
    }

    internal static class RegistrationScopeExtensions
    {
        /// <summary>Opens (creating if needed) the <c>Software\Classes</c> root for the scope, writable.</summary>
        public static RegistryKey OpenClassesForWrite(this RegistrationScope scope)
        {
            RegistryKey hive = scope == RegistrationScope.LocalMachine
                ? Microsoft.Win32.Registry.LocalMachine
                : Microsoft.Win32.Registry.CurrentUser;
            return hive.CreateSubKey(@"Software\Classes", writable: true);
        }

        /// <summary>Opens the <c>Software\Classes</c> root for the scope for reading, or <see langword="null"/> if absent.</summary>
        public static RegistryKey? OpenClassesForRead(this RegistrationScope scope)
        {
            RegistryKey hive = scope == RegistrationScope.LocalMachine
                ? Microsoft.Win32.Registry.LocalMachine
                : Microsoft.Win32.Registry.CurrentUser;
            return hive.OpenSubKey(@"Software\Classes");
        }
    }
}
