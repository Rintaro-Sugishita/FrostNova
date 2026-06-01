using FrostNova.Core.Wpf.AttachedProperties;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridIsSelectedBehavior : Behavior<DataGrid>
    {

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectedCellsChanged += AssociatedObject_SelectedCellsChanged;

        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectedCellsChanged -= AssociatedObject_SelectedCellsChanged;
        }

        private void AssociatedObject_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            Debug.WriteLine("selected cells changed");
            var selectedList = AssociatedObject.SelectedCells.Select(x => x.Item).Distinct().ToList();
            try
            {
                //isSettingSelectedItems = true;

                //cell単体を選択しているときはrow自体が選択状態とはみなされない
                DataGridEx.SetSelectedItems(AssociatedObject, selectedList);
            }
            finally
            {
                //isSettingSelectedItems = false;
            }

        }
        //private bool isSettingSelectedItems = false;

        
        private static void OnSelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {

            var behavior = sender as DataGridIsSelectedBehavior;


            //if (behavior.AssociatedObject is DataGrid grid && behavior.isSettingSelectedItems == false && e.NewValue is IList list)
            //{
            //    grid.SelectedCells.Clear();
            //    foreach (var item in list)
            //    {
            //        //全行選択にする
            //        //カレントセルは指定がなければ最初のやつにする
            //        foreach (var column in grid.Columns)
            //        {

            //            var info = new DataGridCellInfo(item, column);
            //            grid.SelectedCells.Add(info);
            //            var key = (string)column.GetValue(DataGridColumnEx.KeyProperty);
            //            if (string.IsNullOrEmpty(behavior.FocusKey) == false && key == behavior.FocusKey)
            //            {
            //                FocusCell(grid, info);
            //            }
            //            else if (string.IsNullOrEmpty(behavior.FocusKey) && column.DisplayIndex == 0)
            //            {
            //                FocusCell(grid, info);
            //            }

            //        }
            //        //grid.SelectedItems.Add(item);
            //    }
            //}
        }



    }
}
