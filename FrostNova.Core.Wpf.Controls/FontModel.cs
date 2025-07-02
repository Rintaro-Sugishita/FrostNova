using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Controls
{
    public class FontModel
    {
        public FontFamily FontFamily { get; set; }  //フォントファミリー
        public string LocalFontName { get; set; }   //フォント名

        public FontModel(FontFamily family, string n)
        {
            FontFamily = family;
            LocalFontName = n;
        }
    }
}
