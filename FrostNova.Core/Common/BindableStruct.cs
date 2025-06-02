using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Common
{

    public partial class BindableStruct<T> : BindableBase where T : struct
    {
        public BindableStruct()
        {
            _Value = default(T);
            RaisePropertyChanged(nameof(Value));
        }
        public BindableStruct(T value)
        {
            _Value = value;
            RaisePropertyChanged(nameof(Value));
        }

        [ObservableProperty]
        private T _Value;


    }
}
