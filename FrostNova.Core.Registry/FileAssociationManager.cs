using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace FrostNova.Core.Registry
{
    public static class FileAssociationManager
    {
        private const string StartupKeyPath = @"HKEY_CLASSES_ROOT";

        public static void Register(string extension, string id, string path, string description)
        {

            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{extension}", "", id);
            
            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{id}", "", description);
            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{id}\DefaultIcon", "", $"\"{path}\",0");
            Microsoft.Win32.Registry.SetValue($@"{StartupKeyPath}\{id}\Shell\Open\Command", "", $"\"{path}\" \"%1\"");

        }

    }
}
