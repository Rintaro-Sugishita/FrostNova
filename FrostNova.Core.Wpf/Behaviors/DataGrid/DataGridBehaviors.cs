using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FrostNova.Core.Wpf.VisualHelpers;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridBehaviors
    {


        public static readonly DependencyProperty EnableCellClickProperty =
            DependencyProperty.RegisterAttached(
                "EnableCellClick",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableCellClickChanged));

        public static bool? GetEnableCellClick(DataGrid element) => (bool?)element.GetValue(EnableCellClickProperty);

        public static void SetEnableCellClick(DataGrid element, bool? value) => element.SetValue(EnableCellClickProperty, value);

        private static void OnEnableCellClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridCellClickEventHandleBehavior>(ctrl, e);
            }
        }



        public static readonly DependencyProperty EnableCheckBoxSwitchProperty =
            DependencyProperty.RegisterAttached(
                "EnableCheckBoxSwitch",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableCheckBoxSwitchChanged));

        public static bool? GetEnableCheckBoxSwitch(DataGrid element) => (bool?)element.GetValue(EnableCheckBoxSwitchProperty);

        public static void SetEnableCheckBoxSwitch(UIElement element, bool? value) => element.SetValue(EnableCheckBoxSwitchProperty, value);

        private static void OnEnableCheckBoxSwitchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ctrl)
            {
                GeneralHelper.SetBehavior<DataGridCheckBoxBehavior>(ctrl,e);   
            }
        }

        public static readonly DependencyProperty EnableColumnSizeSavingProperty =
            DependencyProperty.RegisterAttached(
                "EnableColumnSizeSaving",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableColumnSizeSavingChanged));

        public static bool? GetEnableColumnSizeSaving(DataGrid element) => (bool?)element.GetValue(EnableColumnSizeSavingProperty);

        public static void SetEnableColumnSizeSaving(DataGrid element, bool? value) => element.SetValue(EnableColumnSizeSavingProperty, value);

        private static void OnEnableColumnSizeSavingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridColumnSizeSavingBehavior>(ctrl, e);
            }
        }





        public static readonly DependencyProperty EnableCurrentCellValueCopyProperty =
            DependencyProperty.RegisterAttached(
                "EnableCurrentCellValueCopy",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableCurrentCellValueCopyChanged));

        public static bool? GetEnableCurrentCellValueCopy(DataGrid element) => (bool?)element.GetValue(EnableCurrentCellValueCopyProperty);

        public static void SetEnableCurrentCellValueCopy(DataGrid element, bool? value) => element.SetValue(EnableCurrentCellValueCopyProperty, value);

        private static void OnEnableCurrentCellValueCopyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridCurrentCellValueCopyBehavior>(ctrl, e);
            }
        }


        public static readonly DependencyProperty EnableDragRowProperty =
            DependencyProperty.RegisterAttached(
                "EnableDragRow",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableDragRowChanged));

        public static bool? GetEnableDragRow(DataGrid element) => (bool?)element.GetValue(EnableDragRowProperty);

        public static void SetEnableDragRow(DataGrid element, bool? value) => element.SetValue(EnableDragRowProperty, value);

        private static void OnEnableDragRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridDragRowBehavior>(ctrl, e);
            }
        }


        public static readonly DependencyProperty EnableRowNumberDisplayProperty =
                    DependencyProperty.RegisterAttached(
                        "EnableRowNumberDisplay",
                        typeof(bool?),
                        typeof(DataGridBehaviors),
                        new PropertyMetadata(null, OnEnableRowNumberDisplayChanged));

        public static bool? GetEnableRowNumberDisplay(DataGrid element) => (bool?)element.GetValue(EnableRowNumberDisplayProperty);

        public static void SetEnableRowNumberDisplay(DataGrid element, bool? value) => element.SetValue(EnableRowNumberDisplayProperty, value);

        private static void OnEnableRowNumberDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridRowNumberDisplayBehavior>(ctrl, e);
            }
        }

        public static readonly DependencyProperty EnableTablePasteProperty =
            DependencyProperty.RegisterAttached(
                "EnableTablePaste",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableTablePasteChanged));

        public static bool? GetEnableTablePaste(DataGrid element) => (bool?)element.GetValue(EnableTablePasteProperty);

        public static void SetEnableTablePaste(DataGrid element, bool? value) => element.SetValue(EnableTablePasteProperty, value);

        private static void OnEnableTablePasteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridTablePasteBehavior>(ctrl, e);
            }
        }

        public static readonly DependencyProperty EnableIsSelectedProperty =
            DependencyProperty.RegisterAttached(
                "EnableIsSelected",
                typeof(bool?),
                typeof(DataGridBehaviors),
                new PropertyMetadata(null, OnEnableIsSelectedChanged));

        public static bool? GetEnableIsSelected(DataGrid element) => (bool?)element.GetValue(EnableIsSelectedProperty);

        public static void SetEnableIsSelected(DataGrid element, bool? value) => element.SetValue(EnableIsSelectedProperty, value);

        private static void OnEnableIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridIsSelectedBehavior>(ctrl, e);
            }
        }




        public static readonly DependencyProperty DataGridColumnIsSelectProperty =
                    DependencyProperty.RegisterAttached(
                        "DataGridColumnIsSelect",
                        typeof(bool),
                        typeof(DataGridBehaviors),
                        new PropertyMetadata(false, OnChanged));

        public static bool GetDataGridColumnIsSelect(DataGrid element) => (bool)element.GetValue(DataGridColumnIsSelectProperty);

        public static void SetDataGridColumnIsSelect(DataGrid element, bool value) => element.SetValue(DataGridColumnIsSelectProperty, value);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridColumnIsSelectBehavior>(ctrl, e);
            }
        }


        public static readonly DependencyProperty DataGridHeaderClickSelectProperty =
            DependencyProperty.RegisterAttached(
                "DataGridHeaderClickSelect",
                typeof(bool),
                typeof(DataGridBehaviors),
                new PropertyMetadata(false, OnDataGridHeaderClickSelectChanged));

        public static bool GetDataGridHeaderClickSelect(DataGrid element) => (bool)element.GetValue(DataGridHeaderClickSelectProperty);

        public static void SetDataGridHeaderClickSelect(DataGrid element, bool value) => element.SetValue(DataGridHeaderClickSelectProperty, value);

        private static void OnDataGridHeaderClickSelectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<DataGridHeaderClickSelectBehavior>(ctrl, e);
            }
        }


        public static readonly DependencyProperty ColumnVisibilityMenuProperty =
            DependencyProperty.RegisterAttached(
                "ColumnVisibilityMenu",
                typeof(bool),
                typeof(DataGridBehaviors),
                new PropertyMetadata(false, OnColumnVisibilityMenuChanged));

        public static bool GetColumnVisibilityMenu(DataGrid element) => (bool)element.GetValue(ColumnVisibilityMenuProperty);

        public static void SetColumnVisibilityMenu(DataGrid element, bool value) => element.SetValue(ColumnVisibilityMenuProperty, value);

        private static void OnColumnVisibilityMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid ctrl)
            {
                GeneralHelper.SetBehavior<ColumnVisibilityMenuBehavior>(ctrl, e);
            }
        }








    }
}
