using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.AttachedProperties
{
    public class TextBoxInput
    {

        public static InputFormatType GetFormatType(DependencyObject obj)
      => (InputFormatType)obj.GetValue(FormatTypeProperty);

        public static void SetFormatType(DependencyObject obj, InputFormatType value)
            => obj.SetValue(FormatTypeProperty, value);

        public static readonly DependencyProperty FormatTypeProperty =
            DependencyProperty.RegisterAttached(
                "FormatType",
                typeof(InputFormatType),
                typeof(TextBoxInput),
                new PropertyMetadata(InputFormatType.None));

        #region text properties

        public static readonly DependencyProperty AllowedCharTypesProperty =
        DependencyProperty.RegisterAttached(
            "AllowedCharTypes",
            typeof(AllowedCharTypes),
            typeof(TextBoxInput),
            new PropertyMetadata(AllowedCharTypes.All));

        public static void SetAllowedCharTypes(DependencyObject obj, AllowedCharTypes value)
            => obj.SetValue(AllowedCharTypesProperty, value);

        public static AllowedCharTypes GetAllowedCharTypes(DependencyObject obj)
            => (AllowedCharTypes)obj.GetValue(AllowedCharTypesProperty);


        //public static readonly DependencyProperty AllowedCharsProperty =
        //    DependencyProperty.RegisterAttached(
        //        "AllowedChars",
        //        typeof(string),
        //        typeof(TextBoxInput),
        //        new PropertyMetadata(string.Empty));


        //public static readonly DependencyProperty DeniedCharsProperty =
        //    DependencyProperty.RegisterAttached(
        //        "DeniedChars",
        //        typeof(string),
        //        typeof(TextBoxInput),
        //        new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AllowedCharsProperty =
            DependencyProperty.RegisterAttached(
                "AllowedChars",
                typeof(string),
                typeof(TextBoxInput),
                new PropertyMetadata(null, OnAllowedCharsChanged));

        public static string GetAllowedChars(TextBox element) => (string)element.GetValue(AllowedCharsProperty);

        public static void SetAllowedChars(TextBox element, string value) => element.SetValue(AllowedCharsProperty, value);

        private static void OnAllowedCharsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {

            }
        }

        public static readonly DependencyProperty DeniedCharsProperty =
            DependencyProperty.RegisterAttached(
                "DeniedChars",
                typeof(string),
                typeof(TextBoxInput),
                new PropertyMetadata(null, OnDeniedCharsChanged));

        public static string GetDeniedChars(TextBox element) => (string)element.GetValue(DeniedCharsProperty);

        public static void SetDeniedChars(TextBox element, string value) => element.SetValue(DeniedCharsProperty, value);

        private static void OnDeniedCharsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {

            }
        }




        #endregion

        #region numeric properties

        public static int GetIntegerDigits(DependencyObject obj)
    => (int)obj.GetValue(IntegerDigitsProperty);

        public static void SetIntegerDigits(DependencyObject obj, int value)
            => obj.SetValue(IntegerDigitsProperty, value);

        public static readonly DependencyProperty IntegerDigitsProperty =
            DependencyProperty.RegisterAttached(
                "IntegerDigits",
                typeof(int),
                typeof(TextBoxInput),
                new PropertyMetadata(9));


        public static int GetDecimalDigits(DependencyObject obj)
            => (int)obj.GetValue(DecimalDigitsProperty);

        public static void SetDecimalDigits(DependencyObject obj, int value)
            => obj.SetValue(DecimalDigitsProperty, value);

        public static readonly DependencyProperty DecimalDigitsProperty =
            DependencyProperty.RegisterAttached(
                "DecimalDigits",
                typeof(int),
                typeof(TextBoxInput),
                new PropertyMetadata(0));


        public static bool GetUseSeparator(DependencyObject obj)
            => (bool)obj.GetValue(UseSeparatorProperty);

        public static void SetUseSeparator(DependencyObject obj, bool value)
            => obj.SetValue(UseSeparatorProperty, value);

        public static readonly DependencyProperty UseSeparatorProperty =
            DependencyProperty.RegisterAttached(
                "UseSeparator",
                typeof(bool),
                typeof(TextBoxInput),
                new PropertyMetadata(false));

        #endregion

        #region datetime properties

        //    public static string GetDateTimeFormat(DependencyObject obj)
        //=> (string)obj.GetValue(DateTimeFormatProperty);

        //    public static void SetDateTimeFormat(DependencyObject obj, string value)
        //        => obj.SetValue(DateTimeFormatProperty, value);

        //    public static readonly DependencyProperty DateTimeFormatProperty =
        //        DependencyProperty.RegisterAttached(
        //            "DateTimeFormat",
        //            typeof(string),
        //            typeof(TextBoxInput),
        //            new PropertyMetadata("yyyy/MM/dd HH:mm:ss"));

        public static readonly DependencyProperty DateTimeFormatProperty =
            DependencyProperty.RegisterAttached(
                "DateTimeFormat",
                typeof(DateTimeFormatType),
                typeof(TextBoxInput),
                new PropertyMetadata(DateTimeFormatType.None, OnDateTimeFormatChanged));

        public static DateTimeFormatType GetDateTimeFormat(TextBox element) => (DateTimeFormatType)element.GetValue(DateTimeFormatProperty);

        public static void SetDateTimeFormat(TextBox element, DateTimeFormatType value) => element.SetValue(DateTimeFormatProperty, value);

        private static void OnDateTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ctrl)
            {

            }
        }

        public static readonly DependencyProperty EnableNowDateInputProperty =
            DependencyProperty.RegisterAttached(
                "EnableNowDateInput",
                typeof(bool),
                typeof(TextBoxInput),
                new PropertyMetadata(false, OnEnableNowDateInputChanged));

        public static bool GetEnableNowDateInput(TextBox element) => (bool)element.GetValue(EnableNowDateInputProperty);

        public static void SetEnableNowDateInput(TextBox element, bool value) => element.SetValue(EnableNowDateInputProperty, value);

        private static void OnEnableNowDateInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox ctrl)
            {

            }
        }





        #endregion

    }
}
