using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Messages
{
    public class ButtonBindClass
    {
        public string ButtonContent { get; set; } = "";

        public MessageBoxResult MessageBoxResult { get; set; }

        public bool IsDefaultButton { get; set; }

        public bool IsCancelButton { get; set; }


    }
}
