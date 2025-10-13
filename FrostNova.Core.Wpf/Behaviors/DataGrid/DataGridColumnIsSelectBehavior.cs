using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridColumnIsSelectBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectedCellsChanged += OnSelectedCellsChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectedCellsChanged -= OnSelectedCellsChanged;
        }

        private void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var grid = AssociatedObject;

            // すべての列のフラグをリセット
            foreach (var col in grid.Columns)
            {
                var header = GetColumnHeader(grid, col);
                if (header != null)
                {
                    DataGridColumnHeaderEx.SetIsSelectedHeader(header, false);
                    DataGridColumnHeaderEx.SetIsAllSelectedHeader(header, false);
                }
            }

            //すべての行のフラグをリセット
            foreach (var item in grid.Items)
            {
                var rowHeader = DataGridHelper.GetRowHeader(grid, item);
                if (rowHeader != null)
                {
                    DataGridRowHeaderEx.SetIsSelectedHeader(rowHeader, false);
                    DataGridRowHeaderEx.SetIsAllSelectedHeader(rowHeader, false);
                }
            }


            // 選択列にフラグを立てる
            var selectedCols = grid.SelectedCells.Select(x => x.Column).Distinct();
            foreach (var col in selectedCols)
            {
                var header = GetColumnHeader(grid, col);
                if (header != null)
                    DataGridColumnHeaderEx.SetIsSelectedHeader(header, true);
            }

            //var selectedCol = grid.SelectedCells.FirstOrDefault().Column;
            //if (selectedCol != null)
            //{
            //    var header = GetColumnHeader(grid, selectedCol);
            //    if (header != null)
            //        DataGridColumnHeaderEx.SetIsSelectedHeader(header, true);
            //}


            //選択行にフラグを立てる
            var selectedRows = grid.SelectedCells.Select(x => DataGridHelper.GetRowHeader(grid, x)).Distinct();
            foreach (var row in selectedRows)
            {
                DataGridRowHeaderEx.SetIsSelectedHeader(row, true);
            }

        }

        private DataGridColumnHeader? GetColumnHeader(DataGrid grid, DataGridColumn column)
        {
            return
               GeneralHelper.FindVisualChildren<DataGridColumnHeader>(grid)
                .FirstOrDefault(h => h.Column == column);
        }

        
    }
}
