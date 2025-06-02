using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Processes
{
    public static class ExplorerHelper
    {
        public static void OpenExplorer(string filePath, bool isDirectoryPath = true)
        {
            if (isDirectoryPath)
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", filePath);
            }
            else
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", System.IO.Path.GetDirectoryName(filePath));
            }
        }

    }
}
