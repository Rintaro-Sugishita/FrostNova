using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Common
{
    public partial class BindableBase : ObservableObject
    {
        public void RaisePropertyChanged([CallerMemberName]string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        
        }        
    }
}
