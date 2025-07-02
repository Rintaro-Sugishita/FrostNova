using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using FrostNova.Core.Common;

namespace FrostNova.Core.Wpf.Controls
{
    public class FontComboBox : Control
    {
        static FontComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FontComboBox), new FrameworkPropertyMetadata(typeof(FontComboBox)));
        }
        public FontComboBox()
        {
            FontFamilies = new NotifyCollection<FontFamily>();

            FontFamilies.AddRange(GetFontList());

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

        }


        /// <summary>
        /// selected value
        /// </summary>
        public FontFamily SelectedFontFamily
        {
            get { return (FontFamily)GetValue(SelectedFontFamilyProperty); }
            set { SetValue(SelectedFontFamilyProperty, value); }
        }
        public static DependencyProperty SelectedFontFamilyProperty = DependencyProperty.Register(nameof(SelectedFontFamily), typeof(FontFamily), typeof(FontComboBox));

        /// <summary>
        /// fonts
        /// </summary>
        public NotifyCollection<FontFamily> FontFamilies
        {
            get { return (NotifyCollection<FontFamily>)GetValue(FontFamiliesProperty); }
            private set { SetValue(FontFamiliesProperty, value); }
        }
        public static DependencyProperty FontFamiliesProperty = DependencyProperty.Register(nameof(FontFamilies), typeof(NotifyCollection<FontFamily>), typeof(FontComboBox));

        /// <summary>
        /// Whether to display text with the font family applied
        /// </summary>
        public bool FontTextVisibility
        {
            get { return (bool)GetValue(FontTextVisibilityProperty); }
            set { SetValue(FontTextVisibilityProperty, value); }
        }
        public static DependencyProperty FontTextVisibilityProperty = DependencyProperty.Register(nameof(FontTextVisibility), typeof(bool), typeof(FontComboBox),new PropertyMetadata(true));

        /// <summary>
        /// Whether to display text without the font family applied
        /// </summary>
        public bool NormalTextVisibility
        {
            get { return (bool)GetValue(NormalTextVisibilityProperty); }
            set { SetValue(NormalTextVisibilityProperty, value); }
        }
        public static DependencyProperty NormalTextVisibilityProperty = DependencyProperty.Register(nameof(NormalTextVisibility), typeof(bool), typeof(FontComboBox), new PropertyMetadata(true));



        /// <summary>
        /// すべてのフォントを取得するメソッド
        /// </summary>
        /// <returns></returns>
        private ICollection<FontFamily> GetFontList()
        {
            return Fonts.SystemFontFamilies;
        }
    }
}
