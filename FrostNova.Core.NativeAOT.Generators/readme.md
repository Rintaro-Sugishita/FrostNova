# FrostNova.Core.NativeAOT.Generators

A collection of high-performance C# Source Generators specifically optimized for **NativeAOT** environments. This package provides automatic implementation of deep cloning logic and strongly-typed JSON settings management without relying on heavy runtime reflection.

## Features

* **DeepCloneGenerator**: Automatically generates efficient deep-copy logic for complex object graphs, including collections, dictionaries, and tuples.
* **JsonSettingsGenerator**: Generates boilerplate for JSON serialization and file I/O (Load/Save), designed to work seamlessly with `System.Text.Json` source generators.

---

## 1. DeepCloneGenerator

This generator implements the `IDeepCloneable<T>` interface and provides a `DeepClone()` method. It avoids reflection by generating explicit assignment code for every property.

### How to use

1. Mark your class as `partial`.
2. Annotate it with the `[GenerateDeepClone]` attribute.

```csharp
using FrostNova.Core.NativeAOT;

[GenerateDeepClone]
public partial class PlayerProfile
{
    public string Name { get; set; }
    public List<int> Scores { get; set; }
    public Dictionary<string, Item> Inventory { get; set; }
}

// Usage
var cloned = original.DeepClone();

```

### Supported Types

* **Collections**: Automatically handles `List<T>`, `Dictionary<TKey, TValue>`, and Arrays (including multi-dimensional).
* **Tuples**: Supports recursive cloning of `ValueTuple` elements.
* **Reference Preservation**: Provides `CopyTo(target)` to copy values into an existing instance, allowing for object reuse.
* **Ignore Property**: Use `[DeepCloneIgnore]` to skip specific properties.

---

## 2. JsonSettingsGenerator

Simplifies the creation of configuration and settings classes by generating methods for serialization and file I/O.

### How to use

```csharp
using FrostNova.Core.NativeAOT;

[GenerateJsonSettings]
public partial class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public int FontSize { get; set; } = 14;

    // Optional: Define a default save path
    [JsonSettingPath]
    public static string GetFilePath() => "settings.json";
}

// Usage
var settings = AppSettings.LoadSettings(); 
settings.Theme = "Light";
settings.SaveSettings(); 

```

### NativeAOT & JsonSerializerContext

To ensure full NativeAOT compatibility, you can associate a `JsonSerializerContext` via the attribute:

```csharp
[GenerateJsonSettings(typeof(MyJsonContext))]
public partial class AppSettings { ... }

```

### Generated Methods

* `FromJson(string json)` / `ToJson(bool indented)`.
* `LoadFromFile(string path)` / `SaveToFile(string path)`.
* `LoadFromFileAsync(string path)` / `SaveToFileAsync(string path)`.
* **Collection Extensions**: Generates `ToClassNameList()` and `ToClassNameArray()` for JSON strings.

---

## Lifecycle Hooks

The **JsonSettingsGenerator** supports `partial` methods to inject custom logic:
* `partial void BeforeSerialize()`: Called before the serialization process starts.
* `partial void AfterDeserialize()`: Called after the object is populated from JSON.
* 
## Requirements

* .NET 7.0 or higher (Optimized for .NET 8/9+ NativeAOT).
* C# 11.0+ (Required for `static virtual` members and `partial` method features).

