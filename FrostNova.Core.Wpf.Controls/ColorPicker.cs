using FrostNova.Core.Wpf.Controls.Dialogs;
using FrostNova.Core.Wpf.Controls.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Controls
{
    public class ColorPicker : Control
    {
        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        /// <summary>
        /// selectedColor
        /// </summary>
        public Color? SelectedColor
        {
            get { return (Color?)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        public static DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(ColorPicker), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        /// <summary>
        /// string
        /// </summary>
        public Color? CurrentPopupColor
        {
            get { return (Color?)GetValue(CurrentPopupColorProperty); }
            internal set { SetValue(CurrentPopupColorProperty, value); }
        }
        public static DependencyProperty CurrentPopupColorProperty = DependencyProperty.Register(nameof(CurrentPopupColor), typeof(Color?), typeof(ColorPicker));

        /// <summary>
        /// string
        /// </summary>
        public Color? OldPopupColor
        {
            get { return (Color?)GetValue(OldPopupColorProperty); }
            internal set { SetValue(OldPopupColorProperty, value); }
        }
        public static DependencyProperty OldPopupColorProperty = DependencyProperty.Register(nameof(OldPopupColor), typeof(Color?), typeof(ColorPicker));
        /// <summary>
        /// max height of popup
        /// </summary>
        public double MaxPopupHeight
        {
            get { return (double)GetValue(MaxPopupHeightProperty); }
            set { SetValue(MaxPopupHeightProperty, value); }
        }
        public static DependencyProperty MaxPopupHeightProperty = DependencyProperty.Register(nameof(MaxPopupHeight), typeof(double), typeof(ColorPicker), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>
        /// min height of popup
        /// </summary>
        public double MinPopupHeight
        {
            get { return (double)GetValue(MinPopupHeightProperty); }
            set { SetValue(MinPopupHeightProperty, value); }
        }
        public static DependencyProperty MinPopupHeightProperty = DependencyProperty.Register(nameof(MinPopupHeight), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0D));

        /// <summary>
        /// height of popup
        /// </summary>
        public double PopupHeight
        {
            get { return (double)GetValue(PopupHeightProperty); }
            set { SetValue(PopupHeightProperty, value); }
        }
        public static DependencyProperty PopupHeightProperty = DependencyProperty.Register(nameof(PopupHeight), typeof(double), typeof(ColorPicker), new PropertyMetadata(double.NaN));

        /// <summary>
        /// max width of popup
        /// </summary>
        public double MaxPopupWidth
        {
            get { return (double)GetValue(MaxPopupWidthProperty); }
            set { SetValue(MaxPopupWidthProperty, value); }
        }
        public static DependencyProperty MaxPopupWidthProperty = DependencyProperty.Register(nameof(MaxPopupWidth), typeof(double), typeof(ColorPicker), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>
        /// min width of popup
        /// </summary>
        public double MinPopupWidth
        {
            get { return (double)GetValue(MinPopupWidthProperty); }
            set { SetValue(MinPopupWidthProperty, value); }
        }
        public static DependencyProperty MinPopupWidthProperty = DependencyProperty.Register(nameof(MinPopupWidth), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0D));

        /// <summary>
        /// width of popup
        /// </summary>
        public double PopupWidth
        {
            get { return (double)GetValue(PopupWidthProperty); }
            set { SetValue(PopupWidthProperty, value); }
        }
        public static DependencyProperty PopupWidthProperty = DependencyProperty.Register(nameof(PopupWidth), typeof(double), typeof(ColorPicker), new PropertyMetadata(double.NaN));

        /// <summary>
        /// PlacementTarget
        /// </summary>
        public UIElement PlacementTarget
        {
            get { return (UIElement)GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }
        public static DependencyProperty PlacementTargetProperty = DependencyProperty.Register(nameof(PlacementTarget), typeof(UIElement), typeof(ColorPicker));

        /// <summary>
        /// Placement
        /// </summary>
        public PlacementMode Placement
        {
            get { return (PlacementMode)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }
        public static DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(ColorPicker), new PropertyMetadata(PlacementMode.Bottom));

        /// <summary>
        /// IsDialogMode
        /// </summary>
        public bool IsDialogMode
        {
            get { return (bool)GetValue(IsDialogModeProperty); }
            set { SetValue(IsDialogModeProperty, value); }
        }
        public static DependencyProperty IsDialogModeProperty = DependencyProperty.Register(nameof(IsDialogMode), typeof(bool), typeof(ColorPicker), new PropertyMetadata(false));


        public Func<IList<Color>> SetRecentlyUsedColorsFunc
        {
            get { return (Func<IList<Color>>)GetValue(SetRecentlyUsedColorsFuncProperty); }
            set { SetValue(SetRecentlyUsedColorsFuncProperty, value); }
        }
        public static DependencyProperty SetRecentlyUsedColorsFuncProperty = DependencyProperty.Register(nameof(SetRecentlyUsedColorsFunc), typeof(Func<IList<Color>>), typeof(ColorPicker));

        public IList<Color> RecentlyUsedColors
        {
            get { return (IList<Color>)GetValue(RecentlyUsedColorsProperty); }
            set { SetValue(RecentlyUsedColorsProperty, value); }
        }
        public static DependencyProperty RecentlyUsedColorsProperty = DependencyProperty.Register(nameof(RecentlyUsedColors), typeof(IList<Color>), typeof(ColorPicker));

        private static void SetColor(ColorPicker picker)
        {
            picker.SelectedColor = picker.CurrentPopupColor;
        }

        private static void SetPopupColor(ColorPicker picker)
        {
            picker.CurrentPopupColor = picker.SelectedColor;
            picker.OldPopupColor = picker.CurrentPopupColor;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();


            var toggleButton = GetTemplateChild("PART_BUTTON") as ToggleButton;
            var okButton = GetTemplateChild("PART_OK") as Button;
            if (toggleButton != null && okButton != null)
            {
                okButton.Click += (sender, e) =>
                {

                    //保存

                    SetColor(this);
                    toggleButton.IsChecked = false;
                    if (SelectedColor != null)
                    {
                        RecentlyUsedColors = SetRecentlyUsedColorsFunc?.Invoke() ?? ColorSelectDialogViewModel.ReadColor();
                        RecentlyUsedColors.Insert(0, SelectedColor.Value);
                        ColorSelectDialogViewModel.SaveColor(RecentlyUsedColors.Distinct().ToList());
                    }
                };
                toggleButton.Checked += (sender, e) =>
                {
                    if (IsDialogMode)
                    {
                        var resultColor = ColorSelectDialog.Show(SelectedColor);
                        if (resultColor != null)
                        {
                            SelectedColor = resultColor;

                        }
                        toggleButton.IsChecked = false;
                    }
                    else
                    {
                        RecentlyUsedColors = ColorSelectDialogViewModel.ReadColor();
                        SetPopupColor(this);
                    }
                };

            }
        }


    }
}
