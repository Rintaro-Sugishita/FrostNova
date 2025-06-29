using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.Behaviors
{

    public class DragMoveEventArgs
    {

        public int NewRowIndex { get; private set; }
        public int OldRowIndex { get; private set; }

        public object MovedItem { get; private set; }

        internal EventArgs OriginalEventArgs { get; set; }

        internal DragMoveEventArgs(int newRowIndex, int oldRowIndex, object movedItem)
        {
            NewRowIndex = newRowIndex;
            OldRowIndex = oldRowIndex;
            MovedItem = movedItem;
        }
    }
}
