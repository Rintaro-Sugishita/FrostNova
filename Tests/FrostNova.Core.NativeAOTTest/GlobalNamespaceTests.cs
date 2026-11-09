using Xunit;

namespace FrostNova.Core.NativeAOTTest;

/// <summary>
/// グローバル名前空間（名前空間宣言なし）の型に対してジェネレータが
/// 正しいコードを出力することの回帰テスト。
/// </summary>
public class GlobalNamespaceTests
{
    [Fact]
    public void DeepCloneWorksForGlobalNamespaceType()
    {
        var source = new GlobalNamespaceDeepCloneModel { Name = "original", Value = 42 };

        var clone = source.DeepClone();

        Assert.NotSame(source, clone);
        Assert.Equal("original", clone.Name);
        Assert.Equal(42, clone.Value);
    }

    [Fact]
    public void GeneratedCloneMethodWorksForGlobalNamespaceType()
    {
        var source = new GlobalNamespaceDeepCloneModel { Name = "method", Value = 7 };

        var clone = GlobalNamespaceCloneHelper.CloneIt(source);

        Assert.NotSame(source, clone);
        Assert.Equal("method", clone.Name);
        Assert.Equal(7, clone.Value);
    }

    [Fact]
    public void JsonRoundTripWorksForGlobalNamespaceType()
    {
        var model = new GlobalNamespaceJsonModel { Name = "json", Value = 123 };

        var json = model.ToJson();
        var restored = GlobalNamespaceJsonModel.FromJson(json) ?? new GlobalNamespaceJsonModel();

        Assert.Equal("json", restored.Name);
        Assert.Equal(123, restored.Value);
    }
}
