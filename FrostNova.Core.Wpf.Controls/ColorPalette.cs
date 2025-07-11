using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.PathHelpers;
using FrostNova.Core.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    ///     <MyNamespace:ColorPalette/>
    ///
    /// </summary>
    public class ColorPalette : Control
    {
        static ColorPalette()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPalette), new FrameworkPropertyMetadata(typeof(ColorPalette)));
        }


        /// <summary>
        /// selected color
        /// </summary>
        public Color? SelectedColor
        {
            get { return (Color?)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        public static DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(ColorPalette));


        /// <summary>
        /// string
        /// </summary>
        public IList<Color> RecentlyUsedColors
        {
            get { return (IList<Color>)GetValue(RecentlyUsedColorsProperty); }
            set { SetValue(RecentlyUsedColorsProperty, value); }
        }
        public static DependencyProperty RecentlyUsedColorsProperty = DependencyProperty.Register(nameof(RecentlyUsedColors), typeof(IList<Color>), typeof(ColorPalette));


        /// <summary>
        /// string
        /// </summary>
        public ICommand SelectColorCommand
        {
            get { return (ICommand)GetValue(SelectColorCommandProperty); }
            set { SetValue(SelectColorCommandProperty, value); }
        }
        public static DependencyProperty SelectColorCommandProperty = DependencyProperty.Register(nameof(SelectColorCommand), typeof(ICommand), typeof(ColorPalette));


        public Func<IList<Color>>? SetRecentlyUsedColorsFunc
        {
            get { return (Func<IList<Color>>?)GetValue(SetRecentlyUsedColorsFuncProperty); }
            set { SetValue(SetRecentlyUsedColorsFuncProperty, value); }
        }
        public static DependencyProperty SetRecentlyUsedColorsFuncProperty = DependencyProperty.Register(nameof(SetRecentlyUsedColorsFunc), typeof(Func<IList<Color>>), typeof(ColorPalette));


   

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SelectColorCommand = new RelayCommand<Color>((c) => { SelectedColor = c; });

        }


        public static IList<Color> ReadColor()
        {
            var path = GetSavePath();
            if (System.IO.File.Exists(path))
            {
                return System.IO.File.ReadAllLines(path).Select(x => ColorTextConverter.ConvertBack(x)).ToList();
            }
            return Array.Empty<Color>();
        }

        public static void SaveColor(IList<Color> colors)
        {
            var path = GetSavePath();
            var folder = System.IO.Path.GetDirectoryName(path)!;
            if (System.IO.Directory.Exists(folder) == false)
            {
                System.IO.Directory.CreateDirectory(folder);
            }

            System.IO.File.WriteAllLines(path, colors.Select(x => ColorTextConverter.Convert(x)));
        }

        public static string GetSavePath()
        {
            var folder = PathHelper.GetUserDataPath(Process.GetCurrentProcess().ProcessName);
            return System.IO.Path.Combine(folder,"usedColors.txt");
        }


    }
}
