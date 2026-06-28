using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Common
{
    /// <summary>
    /// Base class for observable view models. Extends CommunityToolkit <see cref="ObservableObject"/>
    /// with a public <see cref="RaisePropertyChanged"/> method for use from non-generated code.
    /// </summary>
    public partial class BindableBase : ObservableObject
    {
        public void RaisePropertyChanged([CallerMemberName]string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        
        }        
    }
}
