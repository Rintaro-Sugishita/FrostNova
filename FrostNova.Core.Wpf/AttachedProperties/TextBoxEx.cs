using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class TextBoxEx
    {

        public static readonly DependencyProperty NullPlaceHolderTextProperty = DependencyProperty.RegisterAttached("NullPlaceHolderText", typeof(string), typeof(TextBoxEx), new PropertyMetadata(""));

        public static string GetNullPlaceHolderText(DependencyObject target)
        {
            return (string)target.GetValue(NullPlaceHolderTextProperty);
        }

        public static void SetNullPlaceHolderText(DependencyObject target, string value)
        {
            target.SetValue(NullPlaceHolderTextProperty, value);
        }

    }
}
