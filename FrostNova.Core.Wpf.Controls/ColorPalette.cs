using CommunityToolkit.Mvvm.Input;
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
        public ICollection<Color> RecentlyUsedColors
        {
            get { return (ICollection<Color>)GetValue(RecentlyUsedColorsProperty); }
            set { SetValue(RecentlyUsedColorsProperty, value); }
        }
        public static DependencyProperty RecentlyUsedColorsProperty = DependencyProperty.Register(nameof(RecentlyUsedColors), typeof(ICollection<Color>), typeof(ColorPalette));


        /// <summary>
        /// string
        /// </summary>
        public ICommand SelectColorCommand
        {
            get { return (ICommand)GetValue(SelectColorCommandProperty); }
            set { SetValue(SelectColorCommandProperty, value); }
        }
        public static DependencyProperty SelectColorCommandProperty = DependencyProperty.Register(nameof(SelectColorCommand), typeof(ICommand), typeof(ColorPalette));



        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SelectColorCommand = new RelayCommand<Color>((c) => { SelectedColor = c; });
        }



    }
}
