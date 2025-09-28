using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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
            if (Keyboard.IsKeyDown(Key.Space))
            {
                OnSpace(e);
                e.Handled = true;
            }
        }

        private void OnSpace(KeyEventArgs e)
        {
            if (AssociatedObject.CurrentCell.Column is DataGridCheckBoxColumn checkBoxColumn && AssociatedObject.CurrentItem != null)
            {
                var binding = (checkBoxColumn.Binding as Binding);
                if (binding != null)
                {
                    var propertyName = binding.Path.Path;
                    var item = AssociatedObject.CurrentItem;
                    var property = item.GetType().GetProperty(propertyName);
                    if (property != null && property.PropertyType == typeof(bool))
                    {
                        var currentValue = (bool?)property.GetValue(item);
                        property.SetValue(item, !currentValue);
                        e.Handled = true;
                    }
                }
            }
        }

    }
}
