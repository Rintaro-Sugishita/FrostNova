using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FrostNova.WindowEventPickers
{
    public class KeyEventData
    {
        public Key Key { get; }
        public KeyEventData(Key key)
        {
            Key = key;
            IsNumPadEnterKey = false;
        }

        public KeyEventData(Key key, bool isNumPadEnterKey)
        {
            Key = key;
            IsNumPadEnterKey = isNumPadEnterKey;
        }

        public bool IsNumPadEnterKey { get; }

    }
}
