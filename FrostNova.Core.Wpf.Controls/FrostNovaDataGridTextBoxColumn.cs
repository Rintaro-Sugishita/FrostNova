using FrostNova.Core.Wpf.Behaviors;
using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.VisualHelpers;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Controls
{
    public class FrostNovaDataGridTextBoxColumn : DataGridTextColumn
    {
        private static Style? _editingTextBoxStyle;

        private readonly DependencyPropertyDescriptor _textAlignmentDescriptor;

        public FrostNovaDataGridTextBoxColumn()
        {
            _textAlignmentDescriptor = DependencyPropertyDescriptor.FromProperty(
                TextBlock.TextAlignmentProperty,
                typeof(TextBlock));
            _textAlignmentDescriptor.AddValueChanged(this, OnColumnTextAlignmentChanged);
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var element = base.GenerateEditingElement(cell, dataItem);

            if (element is TextBox textBox)
            {
                _editingTextBoxStyle ??= Application.Current.TryFindResource("dataGridTextBoxStyle") as Style;
                if (_editingTextBoxStyle != null)
                {
                    textBox.Style = _editingTextBoxStyle;
                }

                var binding = new Binding(nameof(CanLineBreak))
                {
                    Source = this
                };
                element.SetBinding(TextBoxBehaviors.EnableLineBreakProperty, binding);
            }

            SyncTextAlignment(element);
            return element;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var element = base.GenerateElement(cell, dataItem);
            SyncTextAlignment(element);

            //if (cell != null)
            //{
            //    cell.GotFocus -= OnDataGridCellGotFocus;
            //    cell.GotFocus += OnDataGridCellGotFocus;
            //}

            return element;
        }

        private void OnColumnTextAlignmentChanged(object? sender, EventArgs e)
        {
            RefreshTextAlignmentInRealizedCells();
        }

        private void RefreshTextAlignmentInRealizedCells()
        {
            var grid = DataGridOwner;
            if (grid == null)
            {
                return;
            }

            var displayIndex = DisplayIndex;
            for (var i = 0; i < grid.Items.Count; i++)
            {
                if (grid.Items[i] == CollectionView.NewItemPlaceholder)
                {
                    continue;
                }

                if (grid.ItemContainerGenerator.ContainerFromIndex(i) is not DataGridRow row)
                {
                    continue;
                }

                var presenter = GeneralHelper.FindVisualChild<DataGridCellsPresenter>(row);
                if (presenter == null)
                {
                    continue;
                }

                if (presenter.ItemContainerGenerator.ContainerFromIndex(displayIndex) is not DataGridCell cell
                    || cell.Column != this
                    || cell.Content is not FrameworkElement content)
                {
                    continue;
                }

                SyncTextAlignment(content);
            }
        }

        private void SyncTextAlignment(FrameworkElement? element)
        {
            switch (element)
            {
                case TextBlock textBlock:
                    SyncColumnTextAlignment(this, textBlock, TextBlock.TextAlignmentProperty);
                    break;
                case TextBox textBox:
                    SyncColumnTextAlignment(this, textBox, TextBox.TextAlignmentProperty);
                    break;
            }
        }

        /// <summary>
        /// Mirrors <c>DataGridHelper.SyncColumnProperty</c> for <see cref="TextBlock.TextAlignmentProperty"/> on the column.
        /// </summary>
        private static void SyncColumnTextAlignment(
            DependencyObject column,
            FrameworkElement element,
            DependencyProperty targetProperty)
        {
            if (column.ReadLocalValue(TextBlock.TextAlignmentProperty) != DependencyProperty.UnsetValue)
            {
                element.SetValue(targetProperty, column.GetValue(TextBlock.TextAlignmentProperty));
            }
            else
            {
                element.ClearValue(targetProperty);
            }
        }


        public bool CanLineBreak
        {
            get => (bool)GetValue(CanLineBreakProperty);
            set => SetValue(CanLineBreakProperty, value);
        }

        public static readonly DependencyProperty CanLineBreakProperty = DependencyProperty.Register(
            nameof(CanLineBreak),
            typeof(bool),
            typeof(FrostNovaDataGridTextBoxColumn),
            new PropertyMetadata(false));

        //public bool IsEditOnEnter
        //{
        //    get => (bool)GetValue(IsEditOnEnterProperty);
        //    set => SetValue(IsEditOnEnterProperty, value);
        //}

        //public static readonly DependencyProperty IsEditOnEnterProperty = DependencyProperty.Register(
        //    nameof(IsEditOnEnter),
        //    typeof(bool),
        //    typeof(FrostNovaDataGridTextBoxColumn),
        //    new PropertyMetadata(false));
    }
}
