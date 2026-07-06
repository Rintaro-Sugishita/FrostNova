using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridHeaderClickSelectBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            //AssociatedObject.LoadingRow += AssociatedObject_LoadingRow;
            //AssociatedObject.UnloadingRow += AssociatedObject_UnloadingRow;

            AssociatedObject.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(RowHeader_Click));

        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            //AssociatedObject.LoadingRow -= AssociatedObject_LoadingRow;
            //AssociatedObject.UnloadingRow -= AssociatedObject_UnloadingRow;
            AssociatedObject.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(RowHeader_Click));

            // OnLoaded で各列ヘッダーに張った Click を解除する
            foreach (var header in AssociatedObject
                     .FindVisualChildren<DataGridColumnHeader>())
            {
                header.Click -= OnHeaderClick;
            }
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var header in AssociatedObject
                     .FindVisualChildren<DataGridColumnHeader>())
            {
                // Loaded は複数回発火しうるため、二重購読を防いでから購読する
                header.Click -= OnHeaderClick;
                header.Click += OnHeaderClick;
            }
        }

        private void AssociatedObject_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            var rowHeader = DataGridHelper.GetRowHeader(e.Row);
            if (rowHeader != null)
                rowHeader.Click += RowHeader_Click;

        }
        private void AssociatedObject_UnloadingRow(object? sender, DataGridRowEventArgs e)
        {
            var rowHeader = DataGridHelper.GetRowHeader(e.Row);
            if (rowHeader != null)
                rowHeader.Click -= RowHeader_Click;
        }


        private void RowHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not DataGridRowHeader clickedHeader)
                return;

            //// すべてのヘッダーの選択解除
            //foreach (var header in AssociatedObject
            //         .FindVisualChildren<DataGridRowHeader>())
            //{
            //    DataGridRowHeaderEx.SetIsAllSelectedHeader(header, false);
            //    DataGridRowHeaderEx.SetIsSelectedHeader(header, false);
            //}

            //// クリックされた列を選択
            //DataGridRowHeaderEx.SetIsAllSelectedHeader(clickedHeader, true);

            //実際に選択されたことにする
            var row = DataGridHelper.GetRow(clickedHeader);
            if (row == null) return;
            var grid = AssociatedObject;

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            // 既に選択されているか（ヘッダーに付けているAttachedPropertyを参照しても良い）
            bool alreadySelected = DataGridRowHeaderEx.GetIsAllSelectedHeader(clickedHeader);

            if (!ctrl)
            {
                // Ctrlなし: 他の列選択を解除してこの列だけ選ぶ（トグル）
                DataGridHelper.ClearAllColumnSelectionFlagsAndSelectedCells(grid);

                if (!alreadySelected)
                {
                    DataGridHelper.SelectWholeRow(grid, row);
                    DataGridRowHeaderEx.SetIsAllSelectedHeader(clickedHeader, true);
                }
            }
            else
            {
                // Ctrlあり: 既存選択に追加/削除（トグル）
                if (alreadySelected)
                {
                    // 選択解除
                    DataGridHelper.SelectWholeRow(grid, row);
                    DataGridRowHeaderEx.SetIsAllSelectedHeader(clickedHeader, false);
                }
                else
                {
                    DataGridHelper.SelectWholeRow(grid, row);
                    DataGridRowHeaderEx.SetIsAllSelectedHeader(clickedHeader, true);
                }
            }
            DataGridRowHeaderEx.SetIsSelectedHeader(clickedHeader, false);

            // カレントセルをその列の最初の行に移す（フォーカス/キーボード操作のため）
            var firstItem = grid.Items.Cast<object?>().FirstOrDefault(i => i != CollectionView.NewItemPlaceholder);
            var firstColumn = grid.Columns.FirstOrDefault();
            if (firstColumn != null)
            {
                grid.CurrentCell = new DataGridCellInfo(row, firstColumn);
                grid.Focus();
            }


        }

        //private void OnHeaderClick(object sender, RoutedEventArgs e)
        //{
        //    if (sender is not DataGridColumnHeader clickedHeader)
        //        return;

        //    // すべてのヘッダーの選択解除
        //    foreach (var header in AssociatedObject
        //             .FindVisualChildren<DataGridColumnHeader>())
        //    {
        //       DataGridColumnHeaderEx .SetIsAllSelectedHeader(header, false);
        //       DataGridColumnHeaderEx .SetIsSelectedHeader(header, false);
        //    }

        //    // クリックされた列を選択
        //    DataGridColumnHeaderEx.SetIsAllSelectedHeader(clickedHeader, true);
        //    DataGridHelper.SelectWholeColumn(AssociatedObject, clickedHeader.Column);
        //}
        private void OnHeaderClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not DataGridColumnHeader clickedHeader || AssociatedObject == null)
                return;

            var grid = AssociatedObject;
            var column = clickedHeader.Column;
            if (column == null) return;

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            // 既に選択されているか（ヘッダーに付けているAttachedPropertyを参照しても良い）
            bool alreadySelected = DataGridColumnHeaderEx.GetIsAllSelectedHeader(clickedHeader);

            if (!ctrl)
            {
                // Ctrlなし: 他の列選択を解除してこの列だけ選ぶ（トグル）
                DataGridHelper.ClearAllColumnSelectionFlagsAndSelectedCells(grid);

                if (!alreadySelected)
                {
                    DataGridHelper.SelectWholeColumn(grid, column);
                    DataGridColumnHeaderEx.SetIsAllSelectedHeader(clickedHeader, true);
                }
            }
            else
            {
                // Ctrlあり: 既存選択に追加/削除（トグル）
                if (alreadySelected)
                {
                    // 選択解除
                    DataGridHelper.DeselectWholeColumn(grid, column);
                    DataGridColumnHeaderEx.SetIsAllSelectedHeader(clickedHeader, false);
                }
                else
                {
                    DataGridHelper.SelectWholeColumn(grid, column);
                    DataGridColumnHeaderEx.SetIsAllSelectedHeader(clickedHeader, true);
                }
            }
            DataGridColumnHeaderEx.SetIsSelectedHeader(clickedHeader, false);

            // カレントセルをその列の最初の行に移す（フォーカス/キーボード操作のため）
            var firstItem = grid.Items.Cast<object?>().FirstOrDefault(i => i != CollectionView.NewItemPlaceholder);
            if (firstItem != null)
            {
                grid.CurrentCell = new DataGridCellInfo(firstItem, column);
                grid.Focus();
            }
        }

    }

}
