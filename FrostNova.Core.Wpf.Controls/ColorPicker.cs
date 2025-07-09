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
        public static DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(ColorPicker));


        /// <summary>
        /// string
        /// </summary>
        public Color? CurrentPopupColor
        {
            get { return (Color?)GetValue(CurrentPopupColorProperty); }
         internal   set { SetValue(CurrentPopupColorProperty, value); }
        }
        public static DependencyProperty CurrentPopupColorProperty = DependencyProperty.Register(nameof(CurrentPopupColor), typeof(Color?), typeof(ColorPicker));

        /// <summary>
        /// string
        /// </summary>
        public Color? OldPopupColor
        {
            get { return (Color?)GetValue(OldPopupColorProperty); }
          internal  set { SetValue(OldPopupColorProperty, value); }
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
        /// string
        /// </summary>
        public PlacementMode Placement
        {
            get { return (PlacementMode)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }
        public static DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(ColorPicker), new PropertyMetadata(PlacementMode.Bottom));


        private static void SetColor(ColorPicker picker)
        {
            picker.SelectedColor = picker.CurrentPopupColor;
        }

        private static void SetPopupColor(ColorPicker picker)
        {
            picker.CurrentPopupColor = picker.SelectedColor;
            picker.OldPopupColor = picker.CurrentPopupColor;
            //if (picker.CurrentPopupColor == Colors.Transparent || picker.CurrentPopupColor == null)
            //{
            //    picker.CurrentPopupColor = Colors.Red;
            //}
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();


            var toggleButton = GetTemplateChild("PART_BUTTON") as ToggleButton;
            var okButton = GetTemplateChild("PART_OK") as Button;
            if (toggleButton != null && okButton !=null)
            {
                okButton.Click += (sender, e) =>
                {
                    SetColor(this);
                    toggleButton.IsChecked = false;
                };
                toggleButton.Checked += (sender, e) =>
                {
                    SetPopupColor(this);
                };
            }
        }


    }
}
