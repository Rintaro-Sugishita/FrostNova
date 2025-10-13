using FrostNova.Core.Wpf.AttachedProperties;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.VisualHelpers
{
    internal class DataGridHelper
    {

        internal static DataGridCell? GetCell(DataGrid dataGrid, DataGridCellInfo cellInfo)
        {
            var row = GetDataGridRow(dataGrid,cellInfo.Item);
            if (row == null) return null;

            var presenter = GeneralHelper.FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null) return null;

            var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(cellInfo.Column.DisplayIndex);
            return cell;
        }

        internal static DataGridRow? GetDataGridRow(DataGrid dataGrid, object item)
        {
          var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item);
            if (row is DataGridRow rowObject)
            {
                return rowObject;
            }
            return null;
        }
        internal static DataGridRowHeader? GetRowHeader(DataGrid dataGrid, object item)
        {
            return GetRowHeader(GetDataGridRow(dataGrid, item));
        }


        internal static DataGridRowHeader? GetRowHeader(DataGrid dataGrid, DataGridCellInfo cellInfo)
        {
            return GetRowHeader(GetDataGridRow(dataGrid, cellInfo.Item)); 
        }

        internal static DataGridRowHeader? GetRowHeader(DataGridRow? row)
        {
            if (row==null) return null;
            return GeneralHelper.FindVisualChild<DataGridRowHeader>(row);
        }
        internal static DataGridRow? GetRow(DataGridRowHeader? header)
        {
            if (header == null) return null;

            DependencyObject parent = header;
            while (parent != null && parent is not DataGridRow)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as DataGridRow;
        }
        internal static DataGridRow? GetRow(DataGrid dataGrid, DataGridCellInfo cellInfo)
        {
            return GetDataGridRow(dataGrid, cellInfo.Item);
        }
        internal static void ClearAllColumnSelectionFlagsAndSelectedCells(DataGrid grid)
        {
            // ヘッダー上のAttachedPropertyを全部 false に（見た目用）
            foreach (var header in grid.FindVisualChildren<DataGridColumnHeader>())
            {
                DataGridColumnHeaderEx.SetIsSelectedHeader(header, false);
                DataGridColumnHeaderEx.SetIsAllSelectedHeader(header, false);
            }

            // DataGridの選択セルをクリア
            grid.SelectedCells.Clear();
        }
        internal static void SelectWholeColumn(DataGrid grid, DataGridColumn column)
        {
            // Itemsをループして、各行の DataGridCellInfo を SelectedCells に追加
            var toAdd = new List<DataGridCellInfo>();
            foreach (var item in grid.Items)
            {
                // NewItemPlaceholder を除外（CanUserAddRows=true のとき）
                if (item == CollectionView.NewItemPlaceholder) continue;
                toAdd.Add(new DataGridCellInfo(item, column));
            }

            // 追加（SelectedCells は IList<DataGridCellInfo>）
            foreach (var ci in toAdd)
            {
                if (grid.SelectedCells.Contains(ci) == false)
                {
                    grid.SelectedCells.Add(ci);
                }
            }
        }
        internal static void SelectWholeRow(DataGrid grid, DataGridRow row)
        {
            // Itemsをループして、各行の DataGridCellInfo を SelectedCells に追加
            var toAdd = new List<DataGridCellInfo>();

            foreach (var column in grid.Columns)
            {
                // NewItemPlaceholder を除外（CanUserAddRows=true のとき）
                if (column == CollectionView.NewItemPlaceholder) continue;
                toAdd.Add(new DataGridCellInfo(row.Item, column));
            }

            // 追加（SelectedCells は IList<DataGridCellInfo>）
            foreach (var ci in toAdd)
            {
                if (grid.SelectedCells.Contains(ci) == false)
                {
                    grid.SelectedCells.Add(ci);
                }
            }
        }

        internal static void DeselectWholeColumn(DataGrid grid, DataGridColumn column)
        {
            // SelectedCells から同じ列のものを取り除く
            var toRemove = grid.SelectedCells
                               .Where(ci => ci.Column == column)
                               .ToList();

            foreach (var ci in toRemove)
                grid.SelectedCells.Remove(ci);
        }
    }
}
