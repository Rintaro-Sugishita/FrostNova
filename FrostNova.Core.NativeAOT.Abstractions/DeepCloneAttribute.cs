using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT;

[AttributeUsage(AttributeTargets.Property)]
public class DeepCloneAttribute : Attribute
{
    public DeepCloneMode Mode { get; }
    public DeepCloneAttribute(DeepCloneMode mode) => Mode = mode;
}
