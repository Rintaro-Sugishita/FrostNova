using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Core.Wpf.Controls.Converters;
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
    ///     <MyNamespace:ColorSelector/>
    ///
    /// </summary>
    [ObservableObject]
    public partial class ColorSelector : Control
    {

        static ColorSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorSelector), new FrameworkPropertyMetadata(typeof(ColorSelector)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();



        }

        /// <summary>
        /// selected color
        /// </summary>
        public Color? SelectedColor
        {
            get { return (Color?)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        public static DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(ColorSelector), new PropertyMetadata(Color.FromRgb(255,0,0)));

        /// <summary>
        /// old color
        /// </summary>
        public Color? OldColor
        {
            get { return (Color?)GetValue(OldColorProperty); }
            set { SetValue(OldColorProperty, value); }
        }
        public static DependencyProperty OldColorProperty = DependencyProperty.Register(nameof(OldColor), typeof(Color?), typeof(ColorSelector), new(null));








        public byte SelectedRed
        {
            get
            {
                return SelectedColor.HasValue ? (byte)SelectedColor.Value.R : (byte)0;
            }
            set
            {
                if (SelectedColor != null)
                {
                    SelectedColor = Color.FromArgb(SelectedColor.Value.A, value, SelectedColor.Value.G, SelectedColor.Value.B);
                }
                else
                {
                    SelectedColor = Color.FromArgb(0xFF, value, 0, 0);
                }

                SetHsl();
                SetHueGradient();
                SetSaturationGradient();
                SetLightnessGradient();

                OnColorChanged();

            }
        }

        public byte SelectedGreen
        {
            get
            {
                return SelectedColor.HasValue ? (byte)SelectedColor.Value.G : (byte)0;
            }
            set
            {
                if (SelectedColor != null)
                {
                    SelectedColor = Color.FromArgb(SelectedColor.Value.A, SelectedColor.Value.R, value, SelectedColor.Value.B);
                }
                else
                {
                    SelectedColor = Color.FromArgb(0xFF, 0, value, 0);
                }

                SetHsl();
                SetHueGradient();
                SetSaturationGradient();
                SetLightnessGradient();

                OnColorChanged();

            }
        }
        public byte SelectedBlue
        {
            get
            {
                return SelectedColor.HasValue ? (byte)SelectedColor.Value.B : (byte)0;
            }
            set
            {
                if (SelectedColor != null)
                {
                    SelectedColor = Color.FromArgb(SelectedColor.Value.A, SelectedColor.Value.R, SelectedColor.Value.G, value);
                }
                else
                {
                    SelectedColor = Color.FromArgb(0xFF, 0, 0, value);
                }
                SetHsl();
                SetHueGradient();
                SetSaturationGradient();
                SetLightnessGradient();

                OnColorChanged();
            }
        }

        public byte SelectedA
        {
            get
            {
                return SelectedColor.HasValue ? (byte)SelectedColor.Value.A : (byte)0xFF;
            }
            set
            {
                if (SelectedColor != null)
                {
                    SelectedColor = Color.FromArgb(value, SelectedColor.Value.R, SelectedColor.Value.G, SelectedColor.Value.B);
                }
                else
                {
                    SelectedColor = Color.FromArgb(value, 0, 0, 0);
                }
                OnColorChanged();
                OnPropertyChanged(nameof(SelectedA));
            }
        }

        [ObservableProperty]
        private float _SelectedHue;

        partial void OnSelectedHueChanged(float value)
        {
            var a = ColorHslConverter.HslToRgb(value, SelectedSaturation, SelectedLightness);

            SelectedColor = Color.FromArgb(SelectedColor.HasValue ? SelectedColor.Value.A : (byte)0xFF, a.R, a.G, a.B);
            _SelectedHue = value;
            SetLightnessGradient();
            SetSaturationGradient();
            OnColorChanged();
        }

        [ObservableProperty]
        private float _SelectedSaturation = 1.0F;

        partial void OnSelectedSaturationChanged(float value)
        {
            var a = ColorHslConverter.HslToRgb(SelectedHue, value, SelectedLightness);

            SelectedColor = Color.FromArgb(SelectedColor.HasValue ? SelectedColor.Value.A : (byte)0xFF, a.R, a.G, a.B);
            _SelectedSaturation = value;

            SetHueGradient();
            SetLightnessGradient();

            //SetBrightnessGradient();

            OnColorChanged();
        }


        [ObservableProperty]
        private float _SelectedLightness = 0.5F;

        partial void OnSelectedLightnessChanged(float value)
        {
            var a = ColorHslConverter.HslToRgb(SelectedHue, SelectedSaturation, value);
            SelectedColor = Color.FromArgb(SelectedColor.HasValue ? SelectedColor.Value.A : (byte)0xFF, a.R, a.G, a.B);
            //_SelectedSaturation = value;
            _SelectedLightness = value;
            SetHueGradient();
            SetSaturationGradient();

            OnColorChanged();
        }

        public string HexColorText
        {
            get
            {
                return _HexColorText;
            }
            set
            {
                _HexColorText = value;
                OnPropertyChanged(nameof(HexColorText));

                var v = ColorHslConverter.TextToColor(value);
                if (v != null)
                {
                    SelectedColor = v.Value;
                }
            }
        }
        private string _HexColorText = "";

        private void SetText()
        {
            _HexColorText = ColorHslConverter.ColorToText(SelectedColor);
            OnPropertyChanged(nameof(HexColorText));
        }

        private void OnColorChanged()
        {

            OnPropertyChanged(nameof(SelectedColor));

            OnPropertyChanged(nameof(SelectedRed));
            OnPropertyChanged(nameof(SelectedGreen));
            OnPropertyChanged(nameof(SelectedBlue));

            OnPropertyChanged(nameof(SelectedHue));
            OnPropertyChanged(nameof(SelectedSaturation));
            OnPropertyChanged(nameof(SelectedLightness));
            SetText();

        }


        #region グラデーション

        public Color ColorFF0000Brush
        {
            get => _ColorFF0000Brush;
            set
            {
                _ColorFF0000Brush = value;
                OnPropertyChanged(nameof(ColorFF0000Brush));
            }
        }
        private Color _ColorFF0000Brush = new Color() { A = 255, R = 255, G = 0, B = 0 };

        public Color ColorFFFF00Brush
        {
            get => _ColorFFFF00Brush;
            set
            {
                _ColorFFFF00Brush = value;
                OnPropertyChanged(nameof(ColorFFFF00Brush));
            }
        }
        private Color _ColorFFFF00Brush = new Color() { A = 255, R = 255, G = 255, B = 0 };

        public Color Color00FF00Brush
        {
            get => _Color00FF00Brush;
            set
            {
                _Color00FF00Brush = value;
                OnPropertyChanged(nameof(Color00FF00Brush));
            }
        }
        private Color _Color00FF00Brush = new Color() { A = 255, R = 0, G = 255, B = 0 };

        public Color Color00FFFFBrush
        {
            get => _Color00FFFFBrush;
            set
            {
                _Color00FFFFBrush = value;
                OnPropertyChanged(nameof(Color00FFFFBrush));
            }
        }
        private Color _Color00FFFFBrush = new Color() { A = 255, R = 0, G = 255, B = 255 };

        public Color Color0000FFBrush
        {
            get => _Color0000FFBrush;
            set
            {
                _Color0000FFBrush = value;
                OnPropertyChanged(nameof(Color0000FFBrush));
            }
        }
        private Color _Color0000FFBrush = new Color() { A = 255, R = 0, G = 0, B = 255 };

        public Color ColorFF00FFBrush
        {
            get => _ColorFF00FFBrush;
            set
            {
                _ColorFF00FFBrush = value;
                OnPropertyChanged(nameof(ColorFF00FFBrush));
            }
        }
        private Color _ColorFF00FFBrush = new Color() { A = 255, R = 255, G = 0, B = 255 };

        public Color ColorFF0000Brush2
        {
            get => _ColorFF0000Brush2;
            set
            {
                _ColorFF0000Brush2 = value;
                OnPropertyChanged(nameof(ColorFF0000Brush2));
            }
        }
        private Color _ColorFF0000Brush2 = new Color() { A = 255, R = 255, G = 0, B = 0 };


        public static double MAX_GRADIENT_SLIDER { get; } = 360;

        public static double GradientOffsetFF0000 { get; } = 0;
        public static double GradientOffsetFFFF00 { get; } = 60 / MAX_GRADIENT_SLIDER;
        public static double GradientOffset00FF00 { get; } = 120 / MAX_GRADIENT_SLIDER;
        public static double GradientOffset00FFFF { get; } = 180 / MAX_GRADIENT_SLIDER;
        public static double GradientOffset0000FF { get; } = 240 / MAX_GRADIENT_SLIDER;
        public static double GradientOffsetFF00FF { get; } = 300 / MAX_GRADIENT_SLIDER;
        public static double GradientOffsetFF00002 { get; } = 360 / MAX_GRADIENT_SLIDER;


        public Color ColorMaxBrightness
        {
            get => _ColorMaxBrightness;
            set
            {
                _ColorMaxBrightness = value;
                OnPropertyChanged(nameof(ColorMaxBrightness));
            }
        }
        private Color _ColorMaxBrightness = new Color() { A = 255, R = 255, G = 255, B = 255 };

        public Color ColorMediumBrightness
        {
            get => _ColorMediumBrightness;
            set
            {
                _ColorMediumBrightness = value;
                OnPropertyChanged(nameof(ColorMediumBrightness));
            }
        }
        private Color _ColorMediumBrightness = new Color() { A = 255, R = 255, G = 0, B = 0 };
        public Color ColorMinBrightness
        {
            get => _ColorMinBrightness;
            set
            {
                _ColorMinBrightness = value;
                OnPropertyChanged(nameof(ColorMinBrightness));
            }
        }
        private Color _ColorMinBrightness = new Color() { A = 255, R = 0, G = 0, B = 0 };




        public Color ColorMaxSaturation
        {
            get => _ColorMaxSaturation;
            set
            {
                _ColorMaxSaturation = value;
                OnPropertyChanged(nameof(ColorMaxSaturation));
            }
        }
        private Color _ColorMaxSaturation = new Color() { A = 255, R = 255, G = 255, B = 255 };

        public Color ColorMediumSaturation
        {
            get => _ColorMediumSaturation;
            set
            {
                _ColorMediumSaturation = value;
                OnPropertyChanged(nameof(ColorMediumSaturation));
            }
        }
        private Color _ColorMediumSaturation = new Color() { A = 255, R = 255, G = 0, B = 0 };
        public Color ColorMinSaturation
        {
            get => _ColorMinSaturation;
            set
            {
                _ColorMinSaturation = value;
                OnPropertyChanged(nameof(ColorMinSaturation));
            }
        }
        private Color _ColorMinSaturation = new Color() { A = 255, R = 0, G = 0, B = 0 };


        #endregion
        public ColorSelector()
        {
            SelectedColor = new Color() { A = 255, R = 255, G = 0, B = 0 };
            SetHsl();

            OnColorChanged();

            SetHueGradient();
            SetSaturationGradient();
            SetLightnessGradient();
        }



        private void SetHsl()
        {
            if (SelectedColor != null)
            {

                var ii = ColorHslConverter.ColorToHsl(SelectedColor.Value);

                SelectedHue = ii.hue;
                SelectedSaturation = ii.saturation;
                SelectedLightness = ii.lightness;
                OnPropertyChanged(nameof(SelectedHue));
                OnPropertyChanged(nameof(SelectedSaturation));
                OnPropertyChanged(nameof(SelectedLightness));
            }
        }


        private void SetHueGradient()
        {
            ColorFF0000Brush = ColorHslConverter.HslToRgb(0, SelectedSaturation, SelectedLightness);
            ColorFFFF00Brush = ColorHslConverter.HslToRgb(60, SelectedSaturation, SelectedLightness);
            Color00FF00Brush = ColorHslConverter.HslToRgb(120, SelectedSaturation, SelectedLightness);
            Color00FFFFBrush = ColorHslConverter.HslToRgb(180, SelectedSaturation, SelectedLightness);
            Color0000FFBrush = ColorHslConverter.HslToRgb(240, SelectedSaturation, SelectedLightness);
            ColorFF00FFBrush = ColorHslConverter.HslToRgb(300, SelectedSaturation, SelectedLightness);
            ColorFF0000Brush2 = ColorHslConverter.HslToRgb(360, SelectedSaturation, SelectedLightness);
        }
        private void SetSaturationGradient()
        {
            ColorMaxSaturation = ColorHslConverter.HslToRgb(SelectedHue, 1.0F, SelectedLightness);
            ColorMediumSaturation = ColorHslConverter.HslToRgb(SelectedHue, 0.5F, SelectedLightness);
            ColorMinSaturation = ColorHslConverter.HslToRgb(SelectedHue, 0.0F, SelectedLightness);
        }


        private void SetLightnessGradient()
        {
            ColorMediumBrightness = ColorHslConverter.HslToRgb(SelectedHue, _SelectedSaturation, 0.5F);
            ColorMaxBrightness = ColorHslConverter.HslToRgb(SelectedHue, _SelectedSaturation, 1.0F);
            ColorMinBrightness = ColorHslConverter.HslToRgb(SelectedHue, _SelectedSaturation, 0.0F);


        }


    }
}
