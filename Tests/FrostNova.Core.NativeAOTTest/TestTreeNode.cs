using System;
using System.Collections.Generic;
using System.Text;
using FrostNova.Core.NativeAOT;

namespace FrostNova.Core.NativeAOTTest;

[GenerateDeepClone]
internal partial class TestTreeNode
{
    [DeepCloneIgnore]
    public TestTreeNode? Parent { get; set; }

    public List<TestTreeNode> Children { get; set; } = [];
}
