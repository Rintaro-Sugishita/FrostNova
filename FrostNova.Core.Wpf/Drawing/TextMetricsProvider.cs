using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace FrostNova.Core.Wpf.Drawing
{
   public static class TextMetricsProvider
    {
        public static double Measure(string? text, double? fontSize = 12)
        {
            if (text == null) { return 0; }
            if (VisualControl == null)
            {
                VisualControl = new TextBlock();
            }

            if (VisualControl != null)
            {
                var formattedText = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(VisualControl.FontFamily ?? new FontFamily(), VisualControl.FontStyle, VisualControl.FontWeight, VisualControl.FontStretch),
                fontSize ?? VisualControl.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(VisualControl).PixelsPerDip);
                return formattedText.Width;
            }
            else { return 0; }
        }


        public static TextBlock? VisualControl { get; set; } = null;


    }
}
