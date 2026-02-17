using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.NativeAOT;


/// <summary>
/// <para xml:lang="ja">NativeAOT に最適化された DeepClone 処理を自動生成する Incremental Generator です。</para>
/// <para xml:lang="en">An Incremental Generator that automatically generates DeepClone logic optimized for NativeAOT.</para>
/// </summary>
/// <remarks>
/// <para xml:lang="ja">
/// このジェネレーターは以下の 3 つのソースから情報を収集し、コピーロジックを構成します：
/// 1. [GenerateDeepClone] が付与された自プロジェクトのクラス。
/// 2. [GenerateDeepClone] または [GenerateCopyProperty] が付与された partial メソッド（外部型用）。
/// 3. 他プロジェクトで既に IDeepCloneable インターフェースを実装している型。
/// </para>
/// <para xml:lang="en">
/// This generator collects information from three sources to construct the cloning logic:
/// 1. Classes in the current project marked with [GenerateDeepClone].
/// 2. Partial methods marked with [GenerateDeepClone] or [GenerateCopyProperty] (for external types).
/// 3. Types in other projects that already implement the IDeepCloneable interface.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class DeepCloneGenerator : IIncrementalGenerator
{

    const string CLONE_ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.GenerateDeepCloneAttribute";
    const string COPY_ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.GenerateCopyPropertyAttribute";
    const string INTERFACE_NAME = "FrostNova.Core.NativeAOT.IDeepCloneable";
    const string DOTNET_INTERFACE_NAME = "System.ICloneable";
    const string IGNORE_ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.DeepCloneIgnoreAttribute";

    /// <summary>
    /// Represents metadata describing an external clone method for a specific type, including its target type,
    /// method name, container, and whether it performs a deep clone.
    /// </summary>
    /// <remarks>This record is typically used to associate a type with an external cloning method,
    /// such as for code generation or runtime invocation scenarios. The <see cref="IsDeepClone"/> property
    /// indicates whether the clone operation copies nested objects recursively (<see langword="true"/>) or performs
    /// a shallow copy (<see langword="false"/>).</remarks>
    /// <param name="TargetType"></param>
    /// <param name="MethodName"></param>
    /// <param name="ContainerName"></param>
    /// <param name="IsDeepClone"></param>
    public record ExternalCloneMap(ITypeSymbol TargetType, string MethodName, string ContainerName, bool IsDeepClone);

    /// <summary>
    /// Initializes the incremental generator by configuring syntax providers and registering source output
    /// generation.
    /// </summary>
    /// <remarks>This method sets up syntax providers to extract class and method declarations from
    /// the compilation and combines their results. It then registers a source output action that executes code
    /// generation based on the extracted syntax nodes. Call this method from the generator's <c>Initialize</c>
    /// entry point to enable incremental source generation.</remarks>
    /// <param name="context">The <see cref="IncrementalGeneratorInitializationContext"/> used to set up syntax providers and register
    /// source output actions for the generator.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();

        var version = context.AnalyzerConfigOptionsProvider.Select((options, _) =>
        {
            var assembly = typeof(JsonSettingsGenerator).Assembly;
            string v = assembly.GetName().Version?.ToString() ?? "1.0.0";
            return v ?? "1.0.0"; // フォールバック
        });

        //クラスの抽出
        var classDeclarations = context.SyntaxProvider
                    .CreateSyntaxProvider(
                        predicate: (s, _) => IsSyntaxTargetForGeneration(s), // 高速な構文チェック
                        transform: (ctx, _) => GetSemanticTargetForGeneration(ctx)) // 詳細な型情報取得
                    .Where(m => m is not null);

        //メソッドの抽出
        var methodDeclarations = context.SyntaxProvider
                    .CreateSyntaxProvider(
                        predicate: (s, _) => IsMethodTarget(s),
                        transform: (ctx, _) => GetMethodSemanticTarget(ctx))
                    .Where(m => m is not null);

        var combined = classDeclarations.Collect().Combine(methodDeclarations.Collect()).Combine(version);

        context.RegisterSourceOutput(combined, (spc, source) => Execute(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static ITypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // InitializeでのPredicateにより、nodeはClassDeclarationSyntaxであることが確定済み
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // クラスのシンボル情報を取得
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (symbol is not INamedTypeSymbol classSymbol) return null;

        // 属性をチェック（完全修飾名での比較が確実）
        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == CLONE_ATTRIBUTE_NAME)
            {
                return classSymbol;
            }
        }

        return null;
    }
    private static bool IsMethodTarget(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
    private static IMethodSymbol? GetMethodSemanticTarget(GeneratorSyntaxContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (symbol is not IMethodSymbol methodSymbol) return null;

        // 属性チェック
        if (methodSymbol.GetAttributes().Any(ad =>
            ad.AttributeClass?.ToDisplayString() == CLONE_ATTRIBUTE_NAME || ad.AttributeClass?.ToDisplayString() == COPY_ATTRIBUTE_NAME))
        {
            return methodSymbol;
        }
        return null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<ITypeSymbol?> types, ImmutableArray<IMethodSymbol?> methods, string version)
    {
        var methodMap = methods
            .Where(m => m is not null)
            .Cast<IMethodSymbol>()
            .Select(m => new ExternalCloneMap(
                IsDeepClone(m) ? m.ReturnType : m.Parameters[1].Type, // 戻り値または第2引数から型を特定
                m.Name,
                m.ContainingType.ToDisplayString(),
                IsDeepClone(m)
            ))
            .ToImmutableDictionary<ExternalCloneMap, ITypeSymbol, ExternalCloneMap>(x => (ITypeSymbol)x.TargetType, x => x, SymbolEqualityComparer.Default);

        var generateAttText = $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"FrostNova.Core.NativeAOT.DeepCloneGenerator\", \"{version}\")]";

        // クラスの生成処理
        foreach (var typeSymbol in types.Where(t => t is not null).Cast<INamedTypeSymbol>().Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
        {
            var source = ProcessClass(typeSymbol, methodMap, version, generateAttText);
            if (source is not null) AddSource(context, typeSymbol, source, "Class");
        }

        // メソッドの生成処理
        foreach (var methodSymbol in methods.Where(m => m is not null).Cast<IMethodSymbol>())
        {
            var source = ProcessMethod(methodSymbol, methodMap, generateAttText);
            if (source is not null) AddSource(context, methodSymbol.ContainingType, source, methodSymbol.Name);
        }

    }

    private static void AddSource(SourceProductionContext context, INamedTypeSymbol type, string source, string suffix = "Class")
    {
        var hintName = type.ToDisplayString().Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
        context.AddSource($"{hintName}.{suffix}.DeepClone.g.cs", source);
    }


    private static string? ProcessClass(INamedTypeSymbol classSymbol, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap, string version, string generateAttText)
    {
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
        {
            return null;
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var interfaceName = $"{INTERFACE_NAME}<{className}>";
        var interfaceName2 = $"{INTERFACE_NAME}";


        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {className} : {interfaceName}, {interfaceName2}");
        sb.AppendLine("    {");

        // 1. static な CopyProperty (外部から source と target を受け取る)
        sb.AppendLine($"       {generateAttText}");
        sb.AppendLine($"        public static void CopyProperty({className} source, {className} target, bool updateReference = true)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (source == null || target == null) return;");

        // 基底クラスが属性を持っているなら、その静的メソッドを呼ぶ
        if (classSymbol.BaseType != null && HasDeepCloneAttribute(classSymbol.BaseType))
        {
            sb.AppendLine($"            {classSymbol.BaseType.ToDisplayString()}.CopyProperty(source, target, updateReference);");
        }

        // 全プロパティのコピー実行
        sb.AppendLine(GeneratePropertiesCopyCode(classSymbol, "source", "target", methodMap));
        sb.AppendLine("        }");

        // 2. インスタンスメソッド CopyTo (IDeepCloneable の実装用)
        sb.AppendLine();
        sb.AppendLine($"       {generateAttText}");
        sb.AppendLine($"        public void CopyTo({className} target)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {className}.CopyProperty(this, target, false);");
        sb.AppendLine("        }");

        // 3. DeepClone (新しいインスタンスを作って返す)
        if (HasPublicParameterlessConstructor(classSymbol, false))
        {
            sb.AppendLine();
            sb.AppendLine($"       {generateAttText}");
            sb.AppendLine($"        public {className} DeepClone()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var clone = new {className}();");
            sb.AppendLine($"            {className}.CopyProperty(this, clone, true);");
            sb.AppendLine("            return clone;");
            sb.AppendLine("        }");

            sb.AppendLine();
            sb.AppendLine($@"
    /// <summary>
    /// Explicit interface implementation for the non-generic IDeepCloneable.
    /// </summary>
    object {interfaceName2}.DeepClone()
    {{
        return this.DeepClone();
    }}
");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    private static string? ProcessMethod(IMethodSymbol methodSymbol, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap, string generateAttText)
    {
        var attributes = methodSymbol.GetAttributes();
        var isDeepClone = attributes.Any(ad => ad.AttributeClass?.ToDisplayString() == CLONE_ATTRIBUTE_NAME);
        var isCopyTo = attributes.Any(ad => ad.AttributeClass?.ToDisplayString() == COPY_ATTRIBUTE_NAME);

        var type = isDeepClone ? methodSymbol.ReturnType : methodSymbol.Parameters.ElementAtOrDefault(1)?.Type;
        var sourceType = methodSymbol.Parameters.FirstOrDefault()?.Type;
        if (type == null || sourceType == null) return null;

        var targetTypeName = type.ToDisplayString();
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {methodSymbol.ContainingType.ContainingNamespace.ToDisplayString()}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {methodSymbol.ContainingType.Name}");
        sb.AppendLine("    {");
        if (isDeepClone)
        {
            // T DeepClone(T source) の実装
            sb.AppendLine($"       {generateAttText}");
            sb.AppendLine($"        public static partial {targetTypeName} {methodSymbol.Name}({targetTypeName} source)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (source == null) return null;");
            if (HasPublicParameterlessConstructor(type, false))
            {
                sb.AppendLine($"            var target = new {targetTypeName}();");
                sb.AppendLine(GeneratePropertiesCopyCode(type, "source", "target", methodMap));
                sb.AppendLine("            return target;");
            }
            else
            {
                sb.AppendLine("            throw new InvalidOperationException(\"No public parameterless constructor found.\");");
            }
        }
        else if (isCopyTo)
        {
            // void CopyTo(T source, T target) の実装
            sb.AppendLine($"       {generateAttText}");
            sb.AppendLine($"        public static partial void {methodSymbol.Name}({targetTypeName} source, {targetTypeName} target)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (source == null || target == null) return;");
            sb.AppendLine(GeneratePropertiesCopyCode(type, "source", "target", methodMap));
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }


    private static string GeneratePropertiesCopyCode(ITypeSymbol type, string sourceName, string targetName, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap)
    {
        var sb = new StringBuilder();
        // プロパティを列挙して BuildAssignment を呼ぶ
        foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsReadOnly || member.IsStatic || HasIgnoreAttribute(member)) continue;

            // BuildAssignment を調整し、source/target の変数名を動的に流し込めるようにすると汎用性が上がります
            // ここでは簡易的に文字列置換、または引数追加で対応
            var assignment = BuildAssignment(member, methodMap)
                                .Replace("source.", $"{sourceName}.")
                                .Replace("target.", $"{targetName}.");
            sb.AppendLine($"            {assignment}");
        }
        return sb.ToString();
    }

    private static bool HasPublicParameterlessConstructor(ITypeSymbol type, bool isPartial)
    {
        if (type is not INamedTypeSymbol namedType) return false;

        return namedType.InstanceConstructors.Any(ctor =>
            ctor.Parameters.IsEmpty &&
           ((!isPartial && ctor.DeclaredAccessibility == Accessibility.Public) || isPartial));
    }
    private static bool HasIgnoreAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(ad =>
            ad.AttributeClass?.ToDisplayString() == IGNORE_ATTRIBUTE_NAME);
    }
    private static bool IsDictionary(ITypeSymbol? type)
    {
        if (type == null) return false;
        return type.AllInterfaces.Any(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IDictionary<TKey, TValue>");
    }
    //private static string BuildAssignment(IPropertySymbol prop, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap)
    //{
    //    var name = prop.Name;
    //    var type = prop.Type;
    //    var sourceExpr = $"source.{name}";
    //    var targetExpr = $"target.{name}";

    //    if (IsImmutableOrValueType(type))
    //    {
    //        return $"{targetExpr} = {sourceExpr};";

    //    }


    //    //Discitonaryの特殊処理
    //    if (type is INamedTypeSymbol dicType && dicType.AllInterfaces.Any(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IDictionary<TKey, TValue>"))
    //    {
    //        var keyType = dicType.TypeArguments[0];
    //        var valueType = dicType.TypeArguments[1];
    //        var keyClone = GetCloneExpression(keyType, "x.Key", methodMap);
    //        var valueClone = GetCloneExpression(valueType, "x.Value", methodMap);

    //        return $@"if (updateReference || {targetExpr} == null) {{ {targetExpr} = {sourceExpr}?.ToDictionary(x => {keyClone}, x => {valueClone}); }}
    //        else {{ {targetExpr}.Clear(); if ({sourceExpr} != null) foreach(var x in {sourceExpr}) {targetExpr}.Add({keyClone}, {valueClone}); }}";
    //    }


    //    // Tuple (ValueTuple) の特殊処理
    //    if (type.IsTupleType && type is INamedTypeSymbol tupleType)
    //    {
    //        var elements = tupleType.TupleElements.Select(e => GetCloneExpression(e.Type, $"{sourceExpr}.{e.Name}", methodMap));
    //        return $"{targetExpr} = ({string.Join(", ", elements)});";
    //    }

    //    if (type is INamedTypeSymbol namedType && IsEnumerable(namedType))
    //    {
    //        var elementType = GetElementType(namedType);
    //        if (elementType != null)
    //        {
    //            var elementCopy = GetCloneExpression(elementType, "x", methodMap);
    //            var isArray = type is IArrayTypeSymbol;

    //            // 1. インスタンス維持モード (updateReference == false)
    //            // Clear() & Add() が使えるのは、Array ではなく、かつ Clear メソッドを持っている場合
    //            bool canClear = !isArray && namedType.AllInterfaces.Any(i => i.Name == "ICollection");
    //            if (!isArray)
    //            {
    //                // 新規作成時の処理：型名を使って new する
    //                // target.HashSet = source.HashSet?.Select(x => ...).ToHashSet(); 
    //                // としたいところですが、.ToHashSet() は .NET のバージョンに依存するので、
    //                // 安全に new HashSet<T>(IEnumerable) を使う形式にします。

    //                var typeName = namedType.ToDisplayString();
    //                var createNewExpression = $"{sourceExpr} == null ? null : new {typeName}({sourceExpr}.Select(x => {elementCopy}))";

    //                if (canClear)
    //                {
    //                    return $@"if (updateReference || {targetExpr} == null) {{ {targetExpr} = {createNewExpression}; }}
    //            else {{ {targetExpr}.Clear(); if ({sourceExpr} != null) foreach(var x in {sourceExpr}) {targetExpr}.Add({elementCopy}); }}";
    //                }
    //                else
    //                {
    //                    // Clear できないコレクション（ReadOnlyなど）は常に参照更新
    //                    return $"{targetExpr} = {createNewExpression};";
    //                }
    //            }
    //            else
    //            {
    //                // Array の場合
    //                return $"{targetExpr} = {sourceExpr}?.Select(x => {elementCopy}).ToArray();";
    //            }
    //        }
    //    }

    //    // 子クラスの DeepClone / CopyTo 呼び出し

    //    if (HasDeepCloneAttribute(type) || IsDeepCloneable(type))
    //    {
    //        // 参照更新なら DeepClone()、維持なら target側があれば CopyTo()
    //        return $@"if (updateReference || {targetExpr} == null) {{ {targetExpr} = {sourceExpr}?.DeepClone(); }}
    //            else {{ {sourceExpr}?.CopyTo({targetExpr}); }}";
    //    }

    //    if (IsCloneable(type))
    //    {
    //        return $"{targetExpr} = {sourceExpr}?.Clone() as {type.ToDisplayString()};";
    //    }

    //    // 外部メソッドマップにある場合
    //    if (methodMap.TryGetValue(type, out var map) && !map.IsDeepClone)
    //    {
    //        if (map.IsDeepClone)
    //            return $"{targetExpr} = {map.ContainerName}.{map.MethodName}({sourceExpr});";
    //        else
    //            return $@"if (updateReference || {targetExpr} == null) {{ {targetExpr} = {sourceExpr} == null ? null : new {type.ToDisplayString()}(); if ({targetExpr} != null) {map.ContainerName}.{map.MethodName}({sourceExpr}, {targetExpr}); }}
    //            else {{ {map.ContainerName}.{map.MethodName}({sourceExpr}, {targetExpr}); }}";
    //    }
    //    // 通常のプロパティコピー
    //    var cloneExpr = GetCloneExpression(type, sourceExpr, methodMap);
    //    return $"{targetExpr} = {cloneExpr};";

    //    //return $"{targetExpr} = {sourceExpr};";
    //}


    private static string BuildAssignment(IPropertySymbol prop, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap)
    {


        var attr = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "DeepCloneAttribute");

        // Modeを取得（デフォルトは Default）
        var mode = attr != null ? (int)attr.ConstructorArguments[0].Value : 0;

        if (mode == 2) // Ignore
            return "// Ignored: " + prop.Name;

        if (mode == 1) // Shallow
            return $"target.{prop.Name} = source.{prop.Name};";

        var type = prop.Type;
        var name = prop.Name;
        var sourceExpr = $"source.{name}";
        var targetExpr = $"target.{name}";
        var counter = new VariableCounter();
        // コレクション以外、または再利用不可な場合は GetCloneExpression の結果を直接代入
        //return $"{targetExpr} = {GetCloneExpression(type, sourceExpr, methodMap, targetExpr)};";
        return GetCloneExpression(type, sourceExpr, methodMap, counter, targetExpr);
    }

    /*
    private static string GetCloneExpression(ITypeSymbol? type, string sourcePath, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap, string? targetPath = null)
    {
        if (type == null || type.Kind == SymbolKind.ErrorType)
            return targetPath != null ? $"{targetPath} = {sourcePath};" : sourcePath;

        // --- A. 再利用(Clear & Add)ロジックの生成 ---
        // これが必要なのは targetPath がある（トップレベル）ときだけ
        bool isReusable = targetPath != null &&
                          type.TypeKind != TypeKind.Array &&
                          type.AllInterfaces.Any(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.ICollection<T>") &&
                          !type.ToDisplayString().Contains("ReadOnly");

        if (isReusable && targetPath != null)
        {
            var namedType = (INamedTypeSymbol)type;
            string addLogic;
            if (IsDictionary(namedType))
            {
                var k = GetCloneExpression(namedType.TypeArguments[0], "x.Key", methodMap, null);
                var v = GetCloneExpression(namedType.TypeArguments[1], "x.Value", methodMap, null);
                addLogic = $"{targetPath}.Add({k}, {v});";
            }
            else
            {
                var e = GetCloneExpression(GetElementType(namedType), "x", methodMap, null);
                addLogic = $"{targetPath}.Add({e});";
            }

            // 新規作成が必要な場合の「式」を取得
            var createNewExpr = GetCloneExpression(type, sourcePath, methodMap, null);

            // ★ ここで完結した if 文を返す
            return $@"
            if (updateReference || {targetPath} == null) {{ {targetPath} = {createNewExpr}; }}
                else {{ {targetPath}.Clear(); if ({sourcePath} != null) foreach(var x in {sourcePath}) {{ {addLogic} }} }}";
        }

        // --- B. 純粋な「式」の組み立て ---
        string resultExpr;

        if (IsImmutableOrValueType(type))
        {
            resultExpr = sourcePath;
        }
        else if (type is INamedTypeSymbol dicType && IsDictionary(dicType))
        {
            var k = GetCloneExpression(dicType.TypeArguments[0], "x.Key", methodMap, null);
            var v = GetCloneExpression(dicType.TypeArguments[1], "x.Value", methodMap, null);
            resultExpr = $"{sourcePath}?.ToDictionary(x => {k}, x => {v})";
        }
        else if (type is INamedTypeSymbol nt && IsEnumerable(nt) && type.SpecialType != SpecialType.System_String)
        {
            var e = GetCloneExpression(GetElementType(nt), "x", methodMap, null);
            resultExpr = type is IArrayTypeSymbol
                ? $"{sourcePath}?.Select(x => {e}).ToArray()"
                : $"{sourcePath} == null ? null : new {nt.ToDisplayString()}({sourcePath}.Select(x => {e}))";
        }
        else if (type.IsTupleType && type is INamedTypeSymbol tuple)
        {
            var elements = tuple.TupleElements.Select(e => GetCloneExpression(e.Type, $"{sourcePath}.{e.Name}", methodMap, null));
            resultExpr = $"({string.Join(", ", elements)})";
        }
        else if (type is IArrayTypeSymbol array && array.Rank > 1)
        {
            resultExpr = $"{sourcePath} == null ? null : ({array.ToDisplayString()}){sourcePath}.Clone()";
        }
        else if (HasDeepCloneAttribute(type) || IsDeepCloneable(type))
        {
            // CopyTo が使える特殊ケース (targetPathがある場合のみ)
            if (targetPath != null)
            {
                return $@"if (updateReference || {targetPath} == null) {{ {targetPath} = {sourcePath}?.DeepClone(); }}
            else {{ {sourcePath}?.CopyTo({targetPath}); }}";
            }
            resultExpr = $"{sourcePath}?.DeepClone()";
        }
        else if (IsCloneable(type))
        {
            var castType = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            resultExpr = $"{sourcePath}?.Clone() as {castType}";
        }
        else
        {
            resultExpr = sourcePath;
        }

        // --- C. 合流 ---
        // targetPath があれば代入文にする、なければ式のまま返す
        return targetPath != null ? $"{targetPath} = {resultExpr};" : resultExpr;
    }
    */

    private static string GetCloneExpression(ITypeSymbol? type, string sourcePath, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap, VariableCounter counter, string? targetPath = null)
    {
        if (type == null || type.Kind == SymbolKind.ErrorType)
            return targetPath != null ? $"{targetPath} = {sourcePath};" : sourcePath;

        var actualType = type;
        var isNullable = false;
        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            actualType = named.TypeArguments[0];
            isNullable = true;
        }


        string resultExpr;

        //Tupleの処理TupleはImmutableとして判断されるため優先度高
        //if (actualType.IsTupleType && actualType is INamedTypeSymbol tuple)
        if (actualType.IsTupleType && actualType is INamedTypeSymbol tuple)
        {
            //// 各要素を再帰的にクローンする式を生成
            int id = counter.NextValue();
            string vName = $"v{id}";
            var valName = isNullable ? vName : sourcePath;

            // 要素名を排除したクリーンな型名を取得 (例: (int, ImplementICloneableClass))
            // SymbolDisplayFormat.MinimallyQualifiedFormat などを活用
            var cleanTupleType = actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            // 要素の再帰呼び出し
            var elements = tuple.TupleElements.Select(e =>
                GetCloneExpression(e.Type, $"{valName}.{e.Name}", methodMap, counter, null));

            var tupleExpr = $"({string.Join(", ", elements)})";

            if (isNullable)
            {
                //// ここがポイント：
                //// (int a, Class b) ではなく、要素名を省いた (int, Class) で判定する
                //// これにより、変数 a, b がスコープに導入されるのを防ぎます
                //var typeWithoutNames = actualType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                //// 元の actualType.ToDisplayString() でも、要素名が含まれない形式を指定すればOK
                //string tupleType = actualType.ToDisplayString();

                //resultExpr = $"{sourcePath} is {tupleType} {vName} ? {tupleExpr} : ({tupleType}?)default";
                // Nullableの場合は is 判定で値を vName に取り出す
                // キャスト先も (int, ImplementICloneableClass)? のように要素名なしにする

                var nonNullableType = actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                resultExpr = $"{sourcePath} is {cleanTupleType} {vName} ? {tupleExpr} :({nonNullableType}?) default";
            }
            else
            {
                // resultExpr = $"{sourcePath} == default ? default : {tupleExpr}";
                // 非Nullableの場合は sourcePath をそのまま使う（valName経由）
                //var valName = sourcePath;
                var nonNullableElements = tuple.TupleElements.Select((e, idx) =>
                    GetCloneExpression(e.Type, $"{valName}.Item{idx + 1}", methodMap, counter, null));
                resultExpr = $"({string.Join(", ", nonNullableElements)})";
            }
            if (targetPath != null)
            {
                return $@"
            {{
                {targetPath} = {resultExpr};
            }}";
            }
            else
            {
                return resultExpr;
            }

            //return targetPath != null ? $"{targetPath} = {resultExpr};" : resultExpr;
        }


        // Tupleではない不変型・値型
        if (IsImmutableOrValueType(actualType))
        {
            return targetPath != null ? $"{targetPath} = {sourcePath};" : sourcePath;
        }

        // --- A. 再利用(Clear & Add)ロジックの生成 (targetPath があるトップレベルのみ) ---
        bool isAddable = !type.Name.Contains("Queue") && !type.Name.Contains("Stack");
        bool isReusable = targetPath != null &&
                          actualType.TypeKind != TypeKind.Array &&
                          isAddable &&
                          actualType.AllInterfaces.Any(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.ICollection<T>") &&
                          !actualType.ToDisplayString().Contains("ReadOnly");

        if (isReusable && targetPath != null)
        {
            var namedType = (INamedTypeSymbol)actualType;
            string addLogic;
            if (IsDictionary(namedType))
            {
                // 要素は「式」として取得 (targetPath: null)
                var k = GetCloneExpression(namedType.TypeArguments[0], "x.Key", methodMap, counter, null);
                var v = GetCloneExpression(namedType.TypeArguments[1], "x.Value", methodMap, counter, null);
                addLogic = $"{targetPath}.Add({k}, {v});";
            }
            else
            {
                var e = GetCloneExpression(GetElementType(namedType), "x", methodMap, counter, null);
                addLogic = $"{targetPath}.Add({e});";
            }

            // 新規作成が必要な場合の「式」を取得
            var createNewExpr = GetCloneExpression(type, sourcePath, methodMap, counter, null);

            // ★ ここで分岐を復活！
            return $@"
            if (updateReference || {targetPath} == null) {{ {targetPath} = {createNewExpr}; }}
                else {{ {targetPath}.Clear(); if ({sourcePath} != null) foreach(var x in {sourcePath}) {{ {addLogic} }} }}";
        }

        // --- B. 純粋な「式」の組み立て (再帰中、または再利用不可な型) ---
        // ここからは targetPath に依存せず、値を返す「式」を組み立てる

        // 2. 構造を持つ型 (再帰)
        if (actualType is INamedTypeSymbol dicType && IsDictionary(dicType))
        {
            var k = GetCloneExpression(dicType.TypeArguments[0], "x.Key", methodMap, counter, null);
            var v = GetCloneExpression(dicType.TypeArguments[1], "x.Value", methodMap, counter, null);
            resultExpr = $"{sourcePath}?.ToDictionary(x => {k}, x => {v})";
        }
        else if (actualType is IArrayTypeSymbol array && array.Rank == 1)
        {
            var e = GetCloneExpression(array.ElementType, "x", methodMap, counter, null);
            resultExpr = (e == "x") ? $"{sourcePath}?.ToArray()" : $"{sourcePath}?.Select(x => {e}).ToArray()";
        }
        else if (actualType is INamedTypeSymbol nt && IsEnumerable(nt) && actualType.SpecialType != SpecialType.System_String)
        {
            var e = GetCloneExpression(GetElementType(nt), "x", methodMap, counter, null);
            //resultExpr = (e == "x")
            //    ? $"{sourcePath} == null ? null : new {nt.ToDisplayString()}({sourcePath})"
            //    : $"{sourcePath} == null ? null : new {nt.ToDisplayString()}({sourcePath}.Select(x => {e}))";

            // new に使う型名は Nullable ではないもの (actualType) を使用する
            //string createTypeName = actualType.ToDisplayString();
            string createTypeName = actualType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();

            // ReadOnlyCollection は ToList() が必須
            string suffix = createTypeName.Contains("ReadOnlyCollection") ? ".ToList()" : "";

            resultExpr = (e == "x")
                ? $"{sourcePath} == null ? null : new {createTypeName}({sourcePath}{suffix})"
                : $"{sourcePath} == null ? null : new {createTypeName}({sourcePath}.Select(x => {e}){suffix})";

        }
        //else if (actualType is IArrayTypeSymbol multiArray && multiArray.Rank > 1)
        //{
        //    //2次元配列
        //    resultExpr = $"{sourcePath} == null ? null : ({multiArray.ToDisplayString()}){sourcePath}.Clone()";
        //    // [*,*] のような出力を避けるため、要素型名 + カンマで型名を組み立てる
        //    var commas = new string(',', multiArray.Rank - 1);
        //    var elementTypeName = multiArray.ElementType.ToDisplayString();
        //    var arrayTypeName = $"{elementTypeName}[{commas}]";

        //    resultExpr = $"{sourcePath} == null ? null : ({arrayTypeName}){sourcePath}.Clone()";

        //}
        else if (actualType is IArrayTypeSymbol multiArray && multiArray.Rank > 1)
        {
            var elementTypeName = multiArray.ElementType.ToDisplayString();
            var commas = new string(',', multiArray.Rank - 1);
            var arrayTypeName = $"{elementTypeName}[{commas}]";

            // 要素が不変型（intなど）なら Clone() でOK
            if (IsImmutableOrValueType(multiArray.ElementType))
            {
                resultExpr = $"{sourcePath} == null ? null : ({arrayTypeName}){sourcePath}.Clone()";
            }
            else
            {
                // 要素が参照型なら、再帰的にクローンするヘルパーを呼ぶか、その場でループを回す
                // 今回はシンプルに、要素ごとに DeepClone を呼ぶコードを生成します（2次元の例）
                if (multiArray.Rank == 2)
                {
                    int id = counter.NextValue();
                    string vName = $"arr{id}";
                    // 2次元配列を全走査してコピーする即席コード
                    resultExpr = $@"((Func<{arrayTypeName}, {arrayTypeName}?>)((src) => {{
                if (src == null) return null;
                var dest = new {elementTypeName}[src.GetLength(0), src.GetLength(1)];
                for (int i = 0; i < src.GetLength(0); i++)
                    for (int j = 0; j < src.GetLength(1); j++)
                        dest[i, j] = {GetCloneExpression(multiArray.ElementType, "src[i, j]", methodMap, counter, null)};
                return dest;
            }}))({sourcePath})";
                }
                else
                {
                    // 3次元以上は一旦 Clone() に逃げるか、汎用ヘルパーに飛ばす（今回は暫定でClone）
                    resultExpr = $"{sourcePath} == null ? null : ({arrayTypeName}){sourcePath}.Clone()";
                }
            }
        }
        // 3. 単体オブジェクトのクローン
        else if (HasDeepCloneAttribute(actualType) || IsDeepCloneable(actualType))
        {
            // CopyTo が使える特殊ケース (targetPathがある場合のみ)
            if (targetPath != null)
            {
                return $@"
            if (updateReference || {targetPath} == null) {{ {targetPath} = {sourcePath}?.DeepClone(); }}
                else {{ {sourcePath}?.CopyTo({targetPath}); }}";
            }
            resultExpr = $"{sourcePath}?.DeepClone()";
        }
        else if (IsCloneable(actualType))
        {
            var castType = actualType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            resultExpr = $"{sourcePath}?.Clone() as {castType}";
        }
        else if (actualType.SpecialType == SpecialType.System_Object ||
                    actualType.TypeKind == TypeKind.Interface ||
                    actualType.TypeKind == TypeKind.TypeParameter)
        {
            // 実行時に動的にチェックするコードを生成
            // actualType.ToDisplayString() が "T" や "object" になる
            var typeName = actualType.ToDisplayString();

            resultExpr = $@"{sourcePath} switch 
    {{ 
        {INTERFACE_NAME} d => d.DeepClone(), 
        System.ICloneable c => c.Clone(), 
        _ => {sourcePath} 
    }}";

            // キャストが必要な場合は追加 (object型なら (object)cast)
            resultExpr = $"({typeName})({resultExpr})";
        }
        else
        {
            resultExpr = sourcePath;
        }

        // 最終合流
        return targetPath != null ? $"{targetPath} = {resultExpr};" : resultExpr;
    }
    private static bool IsImmutableOrValueType(ITypeSymbol type)
    {
        var actualType = type;
        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            actualType = named.TypeArguments[0];
        }

        // 値型（struct, enum）は不変として扱う
        if (actualType.IsValueType) return true;

        //値型 (struct, enum, primitives) はすべてOK
        if (actualType.IsValueType) return true;

        //特殊型 (string) はOK
        if (actualType.SpecialType == SpecialType.System_String) return true;

        //参照型だが不変であることが保証されている標準型をホワイトリスト化
        var fullName = actualType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
        return fullName switch
        {
            "System.Uri" => true,
            "System.Version" => true,
            "System.Type" => true, // 型情報は不変
            _ => false
        };
    }
    private static bool IsDeepClone(IMethodSymbol type)
    {
        return type.GetAttributes().Any(ad =>
            ad.AttributeClass?.ToDisplayString() == CLONE_ATTRIBUTE_NAME);
    }
    private static bool IsDeepCloneable(ITypeSymbol type)
    {
        // IDeepCloneable<T> を実装しているか Symbol から判定
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == INTERFACE_NAME &&
            SymbolEqualityComparer.Default.Equals(i.TypeArguments.FirstOrDefault(), type));
    }
    private static bool IsCloneable(ITypeSymbol type)
    {
        // IDeepCloneable<T> を実装しているか Symbol から判定
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == DOTNET_INTERFACE_NAME);
    }
    private static bool HasDeepCloneAttribute(ITypeSymbol type)
    {
        return type.GetAttributes().Any(ad =>
            ad.AttributeClass?.ToDisplayString() == CLONE_ATTRIBUTE_NAME);
    }
    private static bool IsEnumerable(INamedTypeSymbol type)
    {
        // string自体がIEnumerable<char>なので、除外する必要がある
        if (type.SpecialType == SpecialType.System_String) return false;

        // 本人が IEnumerable<T> か、またはそれを実装しているか
        return type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
               type.AllInterfaces.Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    }

    private static ITypeSymbol? GetElementType(INamedTypeSymbol? type)
    {
        if (type == null) return null;
        // type自体が IEnumerable<T> の場合
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return type.TypeArguments.FirstOrDefault();
        }

        // 実装しているインターフェースの中から IEnumerable<T> を探す
        var enumerableInterface = type.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        return enumerableInterface?.TypeArguments.FirstOrDefault();
    }
    private class VariableCounter
    {
        private int Value = 0;

        public int NextValue()
        {
            Value = Value++;
            return Value;
        }
    }
}
