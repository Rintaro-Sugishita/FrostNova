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
        public static readonly DependencyProperty EnableLineBreakProperty =
            DependencyProperty.RegisterAttached(
                "EnableLineBreak",
                typeof(bool),
                typeof(TextBoxBehaviors),
                new PropertyMetadata(false, OnEnableEnableLineBreakChanged));

        public static bool GetEnableLineBreak(TextBox element) => (bool)element.GetValue(EnableLineBreakProperty);

        public static void SetEnableLineBreak(TextBox element, bool value) => element.SetValue(EnableLineBreakProperty, value);

        private static void OnEnableEnableLineBreakChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {
                GeneralHelper.SetBehavior<TextBoxLineBreakBehavior>(ctrl, e);
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

        public static readonly DependencyProperty EnableTextInputLimitProperty =
            DependencyProperty.RegisterAttached(
                "EnableTextInputLimit",
                typeof(bool),
                typeof(TextBoxBehaviors),
                new PropertyMetadata(false, OnEnableTextInputLimitChanged));

        public static bool GetEnableTextInputLimit(TextBox element) => (bool)element.GetValue(EnableTextInputLimitProperty);

        public static void SetEnableTextInputLimit(TextBox element, bool value) => element.SetValue(EnableTextInputLimitProperty, value);

        private static void OnEnableTextInputLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {
                GeneralHelper.SetBehavior<TextBoxInputLimitBehavior>(ctrl, e);
            }
        }





    }
}
