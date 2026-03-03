using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrostNova.Core.Common;

namespace FrostNova.Core.Wpf.Models.DataGrdExtCols;

public interface ICustomDataGridColumnItem
{
    ObservableDictionary<string, object> CustomItems { get; }
}
