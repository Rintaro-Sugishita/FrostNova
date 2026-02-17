using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOTTest;

internal class ImplementICloneableClass : ICloneable
{
    public string Name { get; set; } = "";

    public object Clone()
    {
        var a = new ImplementICloneableClass();
        a.Name = Name;
        return a;

    }

}
