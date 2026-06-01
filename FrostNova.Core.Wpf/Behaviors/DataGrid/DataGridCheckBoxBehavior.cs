using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
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
    internal class DataGridCheckBoxBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
        }

        private void AssociatedObject_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space)
            {
                return;
            }

            if (IsEditingTextCell())
            {
                return;
            }

            if (TryToggleCheckBoxColumn())
            {
                e.Handled = true;
            }
        }

        private bool IsEditingTextCell()
        {
            if (!AssociatedObject.CurrentCell.IsValid)
            {
                return false;
            }

            var cell = DataGridHelper.GetCell(AssociatedObject, AssociatedObject.CurrentCell);
            if (cell == null)
            {
                return false;
            }

            if (cell.IsEditing)
            {
                return true;
            }

            return Keyboard.FocusedElement is TextBoxBase;
        }

        private bool TryToggleCheckBoxColumn()
        {
            if (AssociatedObject.CurrentCell.Column is not DataGridCheckBoxColumn checkBoxColumn
                || AssociatedObject.CurrentItem == null)
            {
                return false;
            }

            if (checkBoxColumn.Binding is not Binding binding)
            {
                return false;
            }

            var propertyName = binding.Path.Path;
            var item = AssociatedObject.CurrentItem;
            var property = item.GetType().GetProperty(propertyName);
            if (property == null || property.PropertyType != typeof(bool))
            {
                return false;
            }

            var currentValue = (bool?)property.GetValue(item);
            property.SetValue(item, !currentValue);
            return true;
        }

    }
}
