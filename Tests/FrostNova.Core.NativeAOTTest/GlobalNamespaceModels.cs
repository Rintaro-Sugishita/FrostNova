using FrostNova.Core.NativeAOT;

// 名前空間を宣言しない（グローバル名前空間の）型でもジェネレータが動作することの回帰テスト。
// 以前は "namespace <global namespace>" が出力されビルドエラーになっていた。

[GenerateDeepClone]
public partial class GlobalNamespaceDeepCloneModel
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

[GenerateJsonSettings]
public partial class GlobalNamespaceJsonModel
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

public static partial class GlobalNamespaceCloneHelper
{
    [GenerateDeepClone]
    public static partial GlobalNamespaceDeepCloneModel CloneIt(GlobalNamespaceDeepCloneModel source);
}
