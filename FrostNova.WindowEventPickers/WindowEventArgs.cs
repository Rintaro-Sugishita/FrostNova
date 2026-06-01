using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.WindowEventPickers
{
    public class WindowEventArgs
    {
        public Process Process { get; }

        public string WindowTitle { get; }

        public DateTime LastChangedTime { get; }

        public Process? PreviousProcess { get; }

        public string PreviousWindowTitle { get; }

        //public string BrowserUrl { get; }


        public WindowEventArgs(Process process, string windowTitle, Process? previoudProcess, string previousWindowTitle)
        {
            Process = process;
            WindowTitle = windowTitle;
            PreviousProcess = previoudProcess;
            PreviousWindowTitle = previousWindowTitle;
        }

    }
}
