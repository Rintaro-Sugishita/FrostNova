using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridEditOnEnterBehavior : Behavior<DataGrid>
    {

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.CurrentCellChanged += AssociatedObject_CurrentCellChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.CurrentCellChanged -= AssociatedObject_CurrentCellChanged;
        }

        private void AssociatedObject_CurrentCellChanged(object? sender, EventArgs e)
        {

            if (sender is DataGrid grid)
            {
                DataGridCell? cell = DataGridHelper.GetCell(grid, grid.CurrentCell);

                if (cell != null)
                {
                    EnterEdit(cell, grid);
                }
            }
        }


        public static void EnterEdit(DataGridCell gridCell, DataGrid grid)
        {
            if (gridCell != null && !gridCell.IsEditing)
            {


                // enables editing on single click  
                if (!gridCell.IsFocused)
                    gridCell.Focus();
                if (!gridCell.IsSelected && grid.SelectionUnit == DataGridSelectionUnit.Cell)
                    gridCell.IsSelected = true;
                grid.BeginEdit();




                //if (bFocus)
                //{
                //    gridCell.Focus();

                //}
            }
        }


    }
}
