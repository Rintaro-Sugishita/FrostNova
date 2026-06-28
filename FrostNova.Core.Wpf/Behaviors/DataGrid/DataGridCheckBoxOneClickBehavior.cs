using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Behaviors
{
    /// <summary>
    /// Behavior that toggles a <c>CheckBox</c> inside a <c>DataGridTemplateColumn</c> with a single click.
    /// Resolves the two-click requirement that arises when combined with <c>DataGridEditOnEnterBehavior</c>.
    /// </summary>
    public class DataGridCheckBoxOneClickBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            base.OnDetaching();
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hitElement = e.MouseDevice.DirectlyOver as DependencyObject;
            if (hitElement == null) return;

            var checkBox = FindAncestorOrSelf<CheckBox>(hitElement);
            if (checkBox == null) return;

            // ヘッダー内のチェックボックスは対象外
            if (FindAncestorOrSelf<DataGridColumnHeader>(checkBox) != null) return;

            var cell = FindAncestorOrSelf<DataGridCell>(checkBox);
            if (cell == null) return;

            if (!checkBox.IsEnabled) return;

            // カレントセルを更新してから CheckBox をトグル
            try
            {
                AssociatedObject.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
            }
            catch { }

            checkBox.IsChecked = !(checkBox.IsChecked ?? false);
            e.Handled = true;
        }

        private static T? FindAncestorOrSelf<T>(DependencyObject? element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T target) return target;
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }
    }
}
