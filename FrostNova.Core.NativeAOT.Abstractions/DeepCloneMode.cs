using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT;

public enum DeepCloneMode
{
    Default = 0, // 通常のディープクローン
    Shallow = 1, // 参照コピーのみ
    Ignore = 2   // 代入自体を行わない
}
