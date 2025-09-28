using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostNova.Core.Wpf.Controls
{
    public class DataGridPasteTemplateColumn : DataGridTemplateColumn
    {

        public DataGridPasteTemplateColumn()
        {
            PastingCellClipboardContent += DataGridPasteTemplateColumn_PastingCellClipboardContent;
        }

        private void DataGridPasteTemplateColumn_PastingCellClipboardContent(object? sender, DataGridCellClipboardEventArgs e)
        {
            if (PasteFromClipboardCommand != null)
            {
                if (PasteFromClipboardCommand.CanExecute(e))
                {
                    PasteFromClipboardCommand.Execute(e);
                }
            }
        }

        public ICommand? PasteFromClipboardCommand
        {
            get { return (ICommand?)GetValue(PasteFromClipboardCommandProperty); }
            set { SetValue(PasteFromClipboardCommandProperty, value); }
        }
        public static DependencyProperty PasteFromClipboardCommandProperty = DependencyProperty.Register(nameof(PasteFromClipboardCommand), typeof(ICommand), typeof(DataGridPasteTemplateColumn), new PropertyMetadata(null));

        public new event EventHandler<DataGridCellClipboardEventArgs> PastingCellClipboardContent;

        public override void OnPastingCellClipboardContent(object item, object cellContent)
        {
            base.OnPastingCellClipboardContent(item, cellContent);

            //EventHandler<DataGridCellClipboardEventArgs> handler = base.pastin;
            PastingCellClipboardContent(this, new DataGridCellClipboardEventArgs(item, this, cellContent));

            //PastingCellClipboardContent.Raise(item, cellContent);
        }
        

    }
}
