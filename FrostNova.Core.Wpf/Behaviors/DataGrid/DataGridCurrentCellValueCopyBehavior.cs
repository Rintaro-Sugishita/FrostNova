using FrostNova.Core.Wpf.AttachedProperties;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Behaviors
{
    public class DataGridCurrentCellValueCopyBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += AssociatedObject_KeyDown;
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= AssociatedObject_KeyDown;

        }

        private void AssociatedObject_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            var grid = sender as DataGrid;
            if (grid == null) { return; }
            if (e.Key != Key.C) { return; }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                string key = DataGridColumnEx.GetKey(grid.CurrentCell.Column);

                object? targetObject = null;
                object item = grid.CurrentCell.Item;
                if (item is DataRow row)
                {
                    targetObject = row[key];
                }
                else if (item is DataRowView rowView)
                {
                    targetObject = rowView[key];
                }
                else
                {
                    var itemType = item.GetType();
                    if (itemType.IsClass)
                    {
                        var p = itemType.GetProperty(key);
                        if (p != null)
                        {
                            targetObject = p.GetValue(item, null);
                        }
                    }
                    else
                    {

                    }
                }

                if (targetObject != null)
                {
                    Clipboard.SetText(targetObject.ToString());
                    e.Handled = true;
                }



            }

        }

    }
}
