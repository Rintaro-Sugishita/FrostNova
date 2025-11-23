using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class ScrollViewerEx
    {
        public static readonly DependencyProperty IsTransparentBarProperty =
            DependencyProperty.RegisterAttached(
                "IsTransparentBar",
                typeof(bool),
                typeof(ScrollViewerEx),
                new PropertyMetadata(false, OnIsTransparentBarChanged));

        public static bool GetIsTransparentBar(ScrollViewer element) => (bool)element.GetValue(IsTransparentBarProperty);

        public static void SetIsTransparentBar(ScrollViewer element, bool value) => element.SetValue(IsTransparentBarProperty, value);

        private static void OnIsTransparentBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ctrl)
            {

            }
        }








    }
}
