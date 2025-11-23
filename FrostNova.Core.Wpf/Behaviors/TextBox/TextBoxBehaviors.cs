using FrostNova.Core.Wpf.VisualHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class TextBoxBehaviors
    {
        public static readonly DependencyProperty EnableTextBoxMultiLineProperty =
            DependencyProperty.RegisterAttached(
                "EnableTextBoxMultiLine",
                typeof(bool),
                typeof(TextBoxBehaviors),
                new PropertyMetadata(false, OnEnableTextBoxMultiLineChanged));

        public static bool GetEnableTextBoxMultiLine(TextBox element) => (bool)element.GetValue(EnableTextBoxMultiLineProperty);

        public static void SetEnableTextBoxMultiLine(TextBox element, bool value) => element.SetValue(EnableTextBoxMultiLineProperty, value);

        private static void OnEnableTextBoxMultiLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {
                GeneralHelper.SetBehavior<TextBoxMultiLineBehavior>(ctrl, e);
            }
        }


        public static readonly DependencyProperty EnableSelectTextWhenFocusProperty =
            DependencyProperty.RegisterAttached(
                "EnableSelectTextWhenFocus",
                typeof(bool),
                typeof(TextBoxBehaviors),
                new PropertyMetadata(false, OnEnableSelectTextWhenFocusChanged));

        public static bool GetEnableSelectTextWhenFocus(TextBox element) => (bool)element.GetValue(EnableSelectTextWhenFocusProperty);

        public static void SetEnableSelectTextWhenFocus(TextBox element, bool value) => element.SetValue(EnableSelectTextWhenFocusProperty, value);

        private static void OnEnableSelectTextWhenFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {

            }
        }




    }
}
