using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

        public static readonly DependencyProperty IsClearButtonShowProperty =
            DependencyProperty.RegisterAttached(
                "IsClearButtonShow",
                typeof(bool),
                typeof(TextBoxEx),
                new PropertyMetadata(false, OnIsClearButtonShowChanged));

        public static bool GetIsClearButtonShow(TextBox element) => (bool)element.GetValue(IsClearButtonShowProperty);

        public static void SetIsClearButtonShow(TextBox element, bool value) => element.SetValue(IsClearButtonShowProperty, value);

        private static void OnIsClearButtonShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ctrl)
            {

            }
        }


        public static readonly DependencyProperty ClearValueProperty =
            DependencyProperty.RegisterAttached(
                "ClearValue",
                typeof(object),
                typeof(TextBoxEx)
               );

        public static object GetClearValue(TextBox element) => (object)element.GetValue(ClearValueProperty);

        public static void SetClearValue(TextBox element, object value) => element.SetValue(ClearValueProperty, value);

    }
}
