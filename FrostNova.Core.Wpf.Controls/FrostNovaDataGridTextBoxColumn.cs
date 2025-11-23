using FrostNova.Core.Wpf.Behaviors;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Controls
{
    public class FrostNovaDataGridTextBoxColumn : DataGridPasteTemplateColumn
    {

        private static Style? _style;

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var element = new TextBox();

            if (element is TextBox textBox)
            {

                if (_style == null)
                {
                    _style = Application.Current.TryFindResource("datagridTextBoxStyle") as Style;
                }


                // 編集時に必要な TextBox 設定
                textBox.Style = _style;

                //textBox.Style.Setters.Add(new Setter() { Property = TextBox.PaddingProperty, Value = 0 });

                var binding = new Binding(nameof(IsMultiLine));
                //binding.Path = ;
                binding.Source = this;

                element.SetBinding(TextBoxBehaviors.EnableTextBoxMultiLineProperty, binding);

            }
            return element;
        }
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            if (IsEditOnEnter)
            {
                return GenerateEditingElement(cell, dataItem);
            }
            else
            {
                return base.GenerateElement(cell, dataItem);
            }
        }
        /// <summary>
        /// string
        /// </summary>
        public bool IsMultiLine
        {
            get { return (bool)GetValue(IsMultiLineProperty); }
            set { SetValue(IsMultiLineProperty, value); }
        }
        public static DependencyProperty IsMultiLineProperty = DependencyProperty.Register(
            nameof(IsMultiLine),
            typeof(bool),
            typeof(FrostNovaDataGridTextBoxColumn),
            new PropertyMetadata(false)
            );

        /// <summary>
        /// string
        /// </summary>
        public bool IsEditOnEnter
        {
            get { return (bool)GetValue(IsEditOnEnterProperty); }
            set { SetValue(IsEditOnEnterProperty, value); }
        }
        public static DependencyProperty IsEditOnEnterProperty = DependencyProperty.Register(
            nameof(IsEditOnEnter), typeof(bool), typeof(FrostNovaDataGridTextBoxColumn),
            new PropertyMetadata(false)
            );

        public BindingBase Binding
        {
            get { return (BindingBase)GetValue(BindingProperty); }
            set { SetValue(BindingProperty, value); }
        }
        public static DependencyProperty BindingProperty = DependencyProperty.Register(nameof(Binding), typeof(BindingBase), typeof(FrostNovaDataGridTextBoxColumn));



    }
}
