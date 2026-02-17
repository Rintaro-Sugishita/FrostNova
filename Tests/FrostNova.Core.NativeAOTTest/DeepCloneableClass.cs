using System;
using System.Collections.Generic;
using System.Text;
using FrostNova.Core.NativeAOT;

namespace FrostNova.Core.NativeAOTTest;

[GenerateDeepClone]
public partial class DeepCloneableClass
{
    public string Name { get; set; } = "";
}
