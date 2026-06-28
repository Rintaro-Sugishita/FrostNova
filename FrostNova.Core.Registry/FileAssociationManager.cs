using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    /// <summary>
    /// Registers file type associations in <c>HKEY_CLASSES_ROOT</c> so that files with a given
    /// extension open with the specified application.
    /// </summary>
    public static class FileAssociationManager
    {
        private const string StartupKeyPath = @"HKEY_CLASSES_ROOT";

        /// <summary>
        /// Associates <paramref name="extension"/> with <paramref name="path"/> in the Windows shell.
        /// </summary>
        /// <param name="extension">The file extension including the leading dot, e.g. <c>.myext</c>.</param>
        /// <param name="id">A unique ProgID string used as the intermediate registry key, e.g. <c>MyApp.Document</c>.</param>
        /// <param name="path">Absolute path to the executable that handles the file type.</param>
        /// <param name="description">Human-readable description shown in the Windows shell.</param>
        public static void Register(string extension, string id, string path, string description)
        {

            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{extension}", "", id);
            
            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{id}", "", description);
            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{id}\DefaultIcon", "", $"\"{path}\",0");
            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{id}\Shell\Open\Command", "", $"\"{path}\" \"%1\"");

        }

    }
}
