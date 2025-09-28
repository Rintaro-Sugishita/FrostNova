using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.VisualHelpers
{
    internal class DataGridHelper
    {

        internal static DataGridCell? GetCell(DataGrid dataGrid, DataGridCellInfo cellInfo)
        {
            var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(cellInfo.Item);
            if (row == null) return null;

            var presenter = GeneralHelper.FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null) return null;

            var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(cellInfo.Column.DisplayIndex);
            return cell;
        }

    }
}
