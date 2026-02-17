using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FrostNova.Core.NativeAOTTest;

public class DeepCloneTests
{
    [Fact]
    public void FullDeepCloneTest()
    {
        // 1. 準備
        var source = new Class1
        {
            
            Text1 = "Original",
            Version = new Version(1, 2, 3), // 不変型
            CloneableClass = new ImplementICloneableClass { Name = "Child1" }, // ICloneable
            CloneableClassList = new List<ImplementICloneableClass>
            {
                new() { Name = "ListElement1" }
            },
            CloneableNullableTupleList = new List<(int a, ImplementICloneableClass b)?>
            {
                (1, new ImplementICloneableClass { Name = "TupleElement1" }),
                null
            }
        };

        
        // 2. 実行
        var target = source.DeepClone();
        
        // 3. 検証
        // 文字列（値）が一致しているか
        Assert.Equal(source.Text1, target.Text1);

        // Version (不変型) は、メモリ節約のため同じ参照を指しているべき
        Assert.Same(source.Version, target.Version);

        // ICloneable なクラスは、中身は同じだが「別インスタンス」であるべき
        Assert.NotSame(source.CloneableClass, target.CloneableClass);
        Assert.Equal(source.CloneableClass.Name, target.CloneableClass.Name);

        // リスト自体が別インスタンスか
        Assert.NotSame(source.CloneableClassList, target.CloneableClassList);
        // リストの中身の要素も別インスタンスか
        Assert.NotSame(source.CloneableClassList[0], target.CloneableClassList[0]);
        Assert.Equal(source.CloneableClassList[0].Name, target.CloneableClassList[0].Name);
        
        // Nullable Tuple の中の参照型要素も別インスタンスか
        var sourceTupleElement = source.CloneableNullableTupleList[0]!.Value.b;
        var targetTupleElement = target.CloneableNullableTupleList[0]!.Value.b;
        Assert.NotSame(sourceTupleElement, targetTupleElement);
        Assert.Equal(sourceTupleElement.Name, targetTupleElement.Name);

        
        // 4. 独立性の確認 (source側を書き換えてもtargetに影響しない)
        source.CloneableClass.Name = "Modified";
        Assert.Equal("Child1", target.CloneableClass.Name);
         
    }
    
    [Fact]
    public void CollectionReuseTest()
    {
        // updateReference = false のときにインスタンスが再利用されるかのテスト
        var source = new Class1
        {
            CloneableClassList = new List<ImplementICloneableClass> { new() { Name = "New" } }
        };
        var target = new Class1
        {
            CloneableClassList = new List<ImplementICloneableClass> { new() { Name = "Old" } }
        };

        var originalListReference = target.CloneableClassList;

        // 実行
        Class1.CopyProperty(source, target, false);

        // 検証：List のインスタンス自体は「同じ」ままであるべき
        Assert.Same(originalListReference, target.CloneableClassList);
        // しかし、中身は source の値（のクローン）に置き換わっているべき
        Assert.Single(target.CloneableClassList);
        Assert.Equal("New", target.CloneableClassList[0].Name);
        Assert.NotSame(source.CloneableClassList[0], target.CloneableClassList[0]);
    }

    [Fact]
    public void ObjectProperty_DeepClone_Test()
    {
        var source = new Class1();
        // object型プロパティに、IDeepCloneableを実装したクラスを入れる
        source.Object = new DeepCloneableClass { Name = "Deep" };

        var target =(Class1) source.DeepClone();

        // 検証：インターフェース経由でちゃんとクローン（別インスタンス）されているか
        Assert.NotSame(source.Object, target.Object);
        Assert.Equal(((DeepCloneableClass)source.Object).Name, ((DeepCloneableClass)target.Object).Name);
        var a = source.Object as DeepCloneableClass;
        a.Name = "sharow";

        Assert.NotEqual(((DeepCloneableClass)source.Object).Name, ((DeepCloneableClass)target.Object).Name);

    }

    [Fact]
    public void ComplexStructure_DeepClone_Test()
    {
        // 1. 準備：かなり深いネストまでデータを入れる
        var source = new Class1
        {
            Text1 = "Original",
            Num1 = 100,
            Color = ConsoleColor.Cyan,
            Version = new Version(2, 0, 0),

            // 3段階ネスト: List -> List -> Dictionary
            ThreeDeepCloneableNestedList = new()
        {
            new() {
                new() { { 1, new DeepCloneableClass { Name = "DeepNested" } } }
            }
        },

            // 多次元配列
            DeepCloneableDimension = new DeepCloneableClass[,] {
            { new() { Name = "Pos00" } }
        },

            // Tuple内のList
            CloneableListTuple = (99, [new() { Name = "ListInTuple" }]),

            // Stack / Queue (再利用不可型)
            StackStuff = new Stack<ImplementICloneableClass>()
        };
        source.StackStuff.Push(new() { Name = "Top" });

        // 2. 実行
        var target = (Class1)source.DeepClone();

        // 3. 検証
        // 3段階ネストのクローン確認
        Assert.NotSame(source.ThreeDeepCloneableNestedList, target.ThreeDeepCloneableNestedList);
        var sourceElement = source.ThreeDeepCloneableNestedList[0][0][1];
        var targetElement = target.ThreeDeepCloneableNestedList[0][0][1];
        Assert.NotSame(sourceElement, targetElement);
        Assert.Equal("DeepNested", targetElement.Name);

        // 多次元配列のクローン確認
        Assert.NotSame(source.DeepCloneableDimension, target.DeepCloneableDimension);
        Assert.NotSame(source.DeepCloneableDimension[0, 0], target.DeepCloneableDimension[0, 0]);
        Assert.Equal("Pos00", target.DeepCloneableDimension[0, 0].Name);

        // Tuple内のListのクローン確認
        Assert.NotSame(source.CloneableListTuple.b, target.CloneableListTuple.b);
        Assert.NotSame(source.CloneableListTuple.b[0], target.CloneableListTuple.b[0]);

        // Stackのクローン確認
        Assert.NotSame(source.StackStuff, target.StackStuff);
        Assert.Equal("Top", target.StackStuff.Peek().Name);
    }

    [Fact]
    public void Collection_SpecialHandling_Test()
    {
        // 1. 準備
        var source = new Class1
        {
            CloneableClassHashSet = [new() { Name = "NewSetItem" }],
            DeepCloneableClassList = [new() { Name = "NewListItem" }]
        };

        var target = new Class1
        {
            // あらかじめインスタンスを生成しておく
            CloneableClassHashSet = [new() { Name = "OldSetItem" }],
            DeepCloneableClassList = [new() { Name = "OldListItem" }]
        };

        // 元の参照を保持
        var originalHashSetRef = target.CloneableClassHashSet;
        var originalListRef = target.DeepCloneableClassList;

        // 2. 実行: staticメソッドを直接呼び出し、updateReference を false に
        Class1.CopyProperty(source, target, updateReference: false);

        // 3. 検証
        // 参照が「同じ」であることを確認（再利用されている）
        Assert.Same(originalHashSetRef, target.CloneableClassHashSet);
        Assert.Same(originalListRef, target.DeepCloneableClassList);

        // 中身は source のものに書き換わっている（かつクローンされている）か
        Assert.Equal("NewSetItem", target.CloneableClassHashSet.First().Name);
        Assert.NotSame(source.CloneableClassHashSet.First(), target.CloneableClassHashSet.First());

        Assert.Equal("NewListItem", target.DeepCloneableClassList[0].Name);
        Assert.NotSame(source.DeepCloneableClassList[0], target.DeepCloneableClassList[0]);
    }
    [Fact]
    public void TreeStructure_Recursive_Test()
    {
        // 1. 準備: Parent -> Children (List) の構成
        var parent = new TestTreeNode();
        var child1 = new TestTreeNode { Parent = parent };
        var child2 = new TestTreeNode { Parent = parent };
        parent.Children.AddRange([child1, child2]);

        // 2. 実行
        var clonedParent = (TestTreeNode)parent.DeepClone();

        // 3. 検証
        Assert.NotSame(parent, clonedParent);
        Assert.Equal(2, clonedParent.Children.Count);

        // 子要素も別インスタンスか
        Assert.NotSame(parent.Children[0], clonedParent.Children[0]);

        // 注意: 現状のロジックだと Parent 参照も DeepClone されるため、
        // clonedParent.Children[0].Parent は clonedParent 自体ではなく、
        // parent をクローンした「別の親インスタンス」を指すことになります。
        // (循環参照解決を実装していない場合の正常な挙動)
        Assert.Null(clonedParent.Children[0].Parent);
        Assert.NotSame(parent, clonedParent.Children[0].Parent);
    }

    


    [Fact]
    public void Enum_And_Immutable_Test()
    {
        var source = new Class1
        {
            Color = ConsoleColor.Magenta,
            NullableColor = ConsoleColor.Yellow,
            Uri = new Uri("https://github.com"),
            longValue1 = 123456789012345,
            DecValue1 = 123.456m
        };

        var target = (Class1)source.DeepClone();

        Assert.Equal(ConsoleColor.Magenta, target.Color);
        Assert.Equal(ConsoleColor.Yellow, target.NullableColor);
        Assert.Same(source.Uri, target.Uri); // Uriは不変型なのでSame
        Assert.Equal(source.longValue1, target.longValue1);
        Assert.Equal(source.DecValue1, target.DecValue1);
    }
}
