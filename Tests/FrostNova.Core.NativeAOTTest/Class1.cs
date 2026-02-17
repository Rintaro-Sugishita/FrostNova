using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FrostNova.Core.NativeAOT;

namespace FrostNova.Core.NativeAOTTest;

[GenerateDeepClone]
internal partial class Class1
{
    
    public string? Text1 { get; set; } = "";
    public string Text2 { get; set; } = "";

    public int? Num1 { get; set; }
    public int Num2 { get; set; }

    public DateTime? Date1 { get; set; }
    public DateTime Date2 { get; set; }

    public decimal? DecValue1 { get; set; }
    public decimal DecValue2 { get; set; }

    public long? longValue1 { get; set; }
    public long longValue2 { get; set; }

    public System.ConsoleColor Color { get; set; }
    public System.ConsoleColor? NullableColor { get; set; }

    public System.Uri? Uri { get; set; }

    public System.Version? Version { get; set; }
    public System.Version Version2 { get; set; } = new Version(0, 0, 0);

    public object Object { get; set; } = new DeepCloneableClass();
    public object? NullableObject { get; set; } = new DeepCloneableClass();


    public ImplementICloneableClass CloneableClass { get; set; } = new();
    public List<ImplementICloneableClass> CloneableClassList { get; set; } = new();
    public HashSet<ImplementICloneableClass> CloneableClassHashSet { get; set; } = new();
    public ReadOnlyCollection<ImplementICloneableClass> CloneableReadOnlyClassList { get; set; } = new ReadOnlyCollection<ImplementICloneableClass>([]);
    public ImplementICloneableClass[] CloneableClassArray { get; set; } = [];


    public (int a, ImplementICloneableClass b) CloneableTuple { get; set; }
    public (int a, ImplementICloneableClass b)? CloneableNullableTuple { get; set; }
    public (int a, List<ImplementICloneableClass> b) CloneableListTuple { get; set; }
    public List<(int a, ImplementICloneableClass b)> CloneableTupleList { get; set; } = [];
    
    public List<(int a, ImplementICloneableClass b)?> CloneableNullableTupleList { get; set; } = [];

    
    public Dictionary<DeepCloneableClass, ImplementICloneableClass> CloneableDictionary { get; set; } = [];
    public List<Dictionary<int, ImplementICloneableClass>> CloneableNestedList { get; set; } = [];

    public ImplementICloneableClass[,] CloneableDimension { get; set; } = new ImplementICloneableClass[,] { };

    public Queue<ImplementICloneableClass> QueueStuff { get; set; } = new();
    public Stack<ImplementICloneableClass> StackStuff { get; set; } = new();

    public DeepCloneableClass DeepCloneableClass { get; set; } = new();
    public List<DeepCloneableClass> DeepCloneableClassList { get; set; } = new();
    public HashSet<DeepCloneableClass> DeepCloneableClassHashSet { get; set; } = new();
    public DeepCloneableClass[] DeepCloneableClassArray { get; set; } = [];


    public (int a, DeepCloneableClass b) DeepCloneableTuple { get; set; }

    public Dictionary<int, DeepCloneableClass> DeepCloneableDictionary { get; set; } = [];
    public List<Dictionary<int, DeepCloneableClass>> DeepCloneableNestedList { get; set; } = [];
    public List<List<Dictionary<int, DeepCloneableClass>>> ThreeDeepCloneableNestedList { get; set; } = [];
    public List<List<Dictionary<int, DeepCloneableClass>>>? ThreeDeepCloneableNestedNullableList { get; set; } = [];
    public DeepCloneableClass[,] DeepCloneableDimension { get; set; } = new DeepCloneableClass[,] { };
    public DeepCloneableClass[,]? DeepCloneableNullableDimension { get; set; } = new DeepCloneableClass[,] { };
    
}
