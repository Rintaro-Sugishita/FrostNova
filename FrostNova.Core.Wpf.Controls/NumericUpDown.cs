using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FrostNova.Core.Wpf.Controls
{
    /// <summary>
    /// このカスタム コントロールを XAML ファイルで使用するには、手順 1a または 1b の後、手順 2 に従います。
    ///
    /// 手順 1a) 現在のプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:FrostNova.Core.Wpf.Controls"
    ///
    ///
    /// 手順 1b) 異なるプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:FrostNova.Core.Wpf.Controls;assembly=FrostNova.Core.Wpf.Controls"
    ///
    /// また、XAML ファイルのあるプロジェクトからこのプロジェクトへのプロジェクト参照を追加し、
    /// リビルドして、コンパイル エラーを防ぐ必要があります:
    ///
    ///     ソリューション エクスプローラーで対象のプロジェクトを右クリックし、
    ///     [参照の追加] の [プロジェクト] を選択してから、このプロジェクトを参照し、選択します。
    ///
    ///
    /// 手順 2)
    /// コントロールを XAML ファイルで使用します。
    ///
    ///     <MyNamespace:NumericUpDown/>
    ///
    /// </summary>
    public class NumericUpDown : Control
    {
        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        private TextBox numTextBox;

        public override void EndInit()
        {
            base.EndInit();

            if (ButtonWidth != 0)
            {
                ButtonFontSize = ButtonWidth * 0.5;
            }
            else
            {
                ButtonFontSize = 20;
            }

            //numTextBox.RefleshFormattedText();
        }

        #region properties

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, value);
                //RaisePropertyChanged(nameof(Value));
                //RaisePropertyChanged(nameof(ValueText));
                //numTextBox.RefleshFormattedText();

            }
        }
        private static DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(decimal.Zero));

        public string ValueText
        {
            get
            {
                return Value.ToString();
            }

            set
            {
                if (decimal.TryParse(value, out decimal ttt))
                {
                    Value = ttt;
                    //RaisePropertyChanged(nameof(Value));
                    //RaisePropertyChanged(nameof(ValueText));

                }

            }
        }

        public decimal MaximumValue
        {
            get => (decimal)GetValue(MaximumValueProperty);
            set => SetValue(MaximumValueProperty, value);
        }
        private static DependencyProperty MaximumValueProperty = DependencyProperty.Register("MaximumValue", typeof(decimal), typeof(NumericUpDown),new PropertyMetadata(decimal.MaxValue));

        public decimal MinimumValue
        {
            get => (decimal)GetValue(MinimumValueProperty);
            set => SetValue(MinimumValueProperty, value);
        }
        private static DependencyProperty MinimumValueProperty = DependencyProperty.Register("MinimumValue", typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(decimal.MinValue));

        public decimal IncrementValue
        {
            get => (decimal)GetValue(IncrementValueProperty);
            set => SetValue(IncrementValueProperty, value);
        }
        private static DependencyProperty IncrementValueProperty = DependencyProperty.Register("IncrementValue", typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(1M));
        public int CtrlIncrement
        {
            get => (int)GetValue(CtrlIncrementProperty);
            set => SetValue(CtrlIncrementProperty, value);

        }
        public static readonly DependencyProperty CtrlIncrementProperty = DependencyProperty.Register(nameof(CtrlIncrement), typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(3M));

        public int ShiftIncrement
        {
            get => (int)GetValue(ShiftIncrementProperty);
            set => SetValue(ShiftIncrementProperty, value);
        }
        public static readonly DependencyProperty ShiftIncrementProperty = DependencyProperty.Register(nameof(ShiftIncrement), typeof(decimal), typeof(NumericUpDown), new PropertyMetadata(0M));


        public bool WrapValue
        {
            get => (bool)GetValue(WrapValueProperty);
            set => SetValue(WrapValueProperty, value);
        }
        private static DependencyProperty WrapValueProperty = DependencyProperty.Register("WrapValueProperty", typeof(bool), typeof(NumericUpDown));

        /// <summary>
        /// IntDigits
        /// </summary>
        public int IntDigits
        {
            get { return (int)GetValue(IntDigitsProperty); }
            set { SetValue(IntDigitsProperty, value); }
        }
        public static DependencyProperty IntDigitsProperty = DependencyProperty.Register(nameof(IntDigits), typeof(int), typeof(NumericUpDown), new PropertyMetadata(3));

        /// <summary>
        /// decDigits
        /// </summary>
        public int DecDigits
        {
            get { return (int)GetValue(DecDigitsProperty); }
            set { SetValue(DecDigitsProperty, value); }
        }
        public static DependencyProperty DecDigitsProperty = DependencyProperty.Register(nameof(DecDigits), typeof(int), typeof(NumericUpDown), new PropertyMetadata(2));


        public double ButtonWidth
        {
            get => (double)GetValue(ButtonWidthProperty);
            set
            {
                SetValue(ButtonWidthProperty, value);
                ////RaisePropertyChanged(nameof(ButtonWidth));
                ButtonFontSize = ButtonWidth * 0.5;
                //RaisePropertyChanged(nameof(ButtonFontSize));
            }
        }
        private static DependencyProperty ButtonWidthProperty = DependencyProperty.Register(nameof(ButtonWidth), typeof(double), typeof(NumericUpDown));

        public double ButtonFontSize
        {
            get
            {
                if (ButtonWidth > Height)
                {
                    return Height * 0.5;
                }
                else
                {
                    return ButtonWidth * 0.5;
                }

            }
            private set
            {
                //RaisePropertyChanged(nameof(ButtonFontSize));
            }
        }


        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
        public static DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NumericUpDown));

        public Style TextBoxStyle
        {
            get => (Style)GetValue(TextBoxStyleProperty);
            set => SetValue(TextBoxStyleProperty, value);
        }
        public static readonly DependencyProperty TextBoxStyleProperty =
            DependencyProperty.Register(nameof(TextBoxStyle), typeof(Style), typeof(NumericUpDown));

        public Style UpButtonStyle
        {
            get => (Style)GetValue(UpButtonStyleProperty);
            set => SetValue(UpButtonStyleProperty, value);
        }
        public static readonly DependencyProperty UpButtonStyleProperty =
            DependencyProperty.Register(nameof(UpButtonStyle), typeof(Style), typeof(NumericUpDown));

        public Style DownButtonStyle
        {
            get => (Style)GetValue(DownButtonStyleProperty);
            set => SetValue(DownButtonStyleProperty, value);
        }
        public static readonly DependencyProperty DownButtonStyleProperty =
            DependencyProperty.Register(nameof(DownButtonStyle), typeof(Style), typeof(NumericUpDown));


        //public delegate void OnMoveUpHandler<EventArgs>(EventArgs e);
        //public event OnMoveUpHandler<EventArgs> MoveUpHandler;


        //public delegate void OnMoveDownHandler<EventArgs>(EventArgs e);
        //public event OnMoveDownHandler<EventArgs> MoveDownHandler;

        #endregion

        #region

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var textBox = GetTemplateChild("PART_numTextBox") as TextBox;
            var upButton = GetTemplateChild("PART_upButton") as Button;
            var downButton = GetTemplateChild("PART_downButton") as Button;

            if (upButton != null) upButton.Click += UpButton_Click;
            if (downButton != null) downButton.Click += DownButton_Click;
            if (textBox != null)
            {
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            }

        }

        //private static void OnButtonWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{

        //    var button = (NumericUpDown)d;
        //    //button.RaisePropertyChanged(nameof(ButtonWidth));
        //    //button.RaisePropertyChanged(nameof(ButtonFontSize));
        //}

        //private static void OnHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{

        //    var button = (NumericUpDown)d;
        //    //button.RaisePropertyChanged("Height");
        //    //button.RaisePropertyChanged(nameof(ButtonWidth));
        //    //button.RaisePropertyChanged(nameof(ButtonFontSize));
        //}
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonClick(true);
        }
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonClick(false);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Right || e.Key == Key.Left || e.Key == Key.Home || e.Key == Key.End)
            {
                //カーソル移動は通す
                //削除も通す
                return;
            }
            if (e.Key == Key.Up)
            {
                ButtonClick(true);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                ButtonClick(false);
                e.Handled = true;
            }
            else
            {
                KeyDownExecute(e);
            }

        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
        }

        private void ButtonClick(bool up)
        {
            if (IsReadOnly) { return; }
            int sign = (up ? 1 : -1);

            decimal inc = IncrementValue;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                inc = CtrlIncrement;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                inc = ShiftIncrement;
            }

            if (up)
            {
                if (Value + inc <= MaximumValue)
                {
                    Value += inc;

                }
                else
                {
                    if (WrapValue)
                    {
                        Value = MinimumValue;

                    }
                }

            }
            else
            {
                if (MinimumValue <= Value - inc)
                {
                    Value -= inc;
                }
                else
                {
                    if (WrapValue)
                    {
                        Value = GetActualMaxValue(inc);
                    }
                }

            }

        }

        private decimal GetActualMaxValue(decimal inc)
        {
            decimal val = 0;
            if (val < MaximumValue)
            {
                val += inc;
            }

            val -= inc;

            return val;

        }

        private void KeyDownExecute(KeyEventArgs e)
        {
            var text = ValueText;

            var chars = text.ToCharArray();

            char inputText = InputKeyToText(e.Key);

            if (inputText == 'U')
            {
                ButtonClick(true);
            }
            else if (inputText == 'Y')
            {
                ButtonClick(false);
            }
            else if (inputText != ' ')
            {
                //var i = GetInputIndex(index);
                //if (i.inputIndex != -1)
                //{
                //    chars[i.inputIndex] = inputText;
                //    string newText = new string(chars);
                //    ValueText = newText;
                //    if (newText == ValueText)
                //    {
                //        timeTextBox.CaretIndex = i.nextCaretIndex;
                //    }
                //}
            }

        }


        private char InputKeyToText(Key e)
        {
            switch (e)
            {
                case Key.D0:
                case Key.NumPad0:
                    return '0';
                case Key.D1:
                case Key.NumPad1:
                    return '1';
                case Key.D2:
                case Key.NumPad2:
                    return '2';
                case Key.D3:
                case Key.NumPad3:
                    return '3';
                case Key.D4:
                case Key.NumPad4:
                    return '4';
                case Key.D5:
                case Key.NumPad5:
                    return '5';
                case Key.D6:
                case Key.NumPad6:
                    return '6';
                case Key.D7:
                case Key.NumPad7:
                    return '7';
                case Key.D8:
                case Key.NumPad8:
                    return '8';
                case Key.D9:
                case Key.NumPad9:
                    return '9';
                case Key.OemPeriod:
                    return '.';
                case Key.OemMinus:
                    return '-';
                case Key.OemPlus:
                    return '+';
                case Key.E:
                    return 'e';
                case Key.Back:
                    return 'B';

                case Key.Delete:
                    return 'D';

                case Key.Up:
                    return 'U';

                case Key.Down:
                    return 'Y';

                default:
                    return ' ';
            }


        }

        #endregion

    }
}
