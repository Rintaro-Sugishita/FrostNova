using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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


        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // 1. static な CopyProperty (外部から source と target を受け取る)
        sb.AppendLine($"       {generateAttText}");
        sb.AppendLine($"        public static void CopyProperty({className} source, {className} target)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (source == null || target == null) return;");

        // 基底クラスが属性を持っているなら、その静的メソッドを呼ぶ
        if (classSymbol.BaseType != null && HasDeepCloneAttribute(classSymbol.BaseType))
        {
            sb.AppendLine($"            {classSymbol.BaseType.ToDisplayString()}.CopyProperty(source, target);");
        }

        // 全プロパティのコピー実行
        sb.AppendLine(GeneratePropertiesCopyCode(classSymbol, "source", "target", methodMap));
        sb.AppendLine("        }");

        // 2. インスタンスメソッド CopyTo (IDeepCloneable の実装用)
        sb.AppendLine();
        sb.AppendLine($"       {generateAttText}");
        sb.AppendLine($"        public void CopyTo({className} target)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {className}.CopyProperty(this, target);");
        sb.AppendLine("        }");

        // 3. DeepClone (新しいインスタンスを作って返す)
        if (HasPublicParameterlessConstructor(classSymbol, false))
        {
            sb.AppendLine();
            sb.AppendLine($"       {generateAttText}");
            sb.AppendLine($"        public {className} DeepClone()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var clone = new {className}();");
            sb.AppendLine("            this.CopyTo(clone);");
            sb.AppendLine("            return clone;");
            sb.AppendLine("        }");
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
    private static string BuildAssignment(IPropertySymbol prop, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap)
    {
        var name = prop.Name;
        var type = prop.Type;
        var sourceExpr = $"source.{name}";
        var targetExpr = $"target.{name}";

        // コレクション型の特殊処理
        if (type is INamedTypeSymbol namedType && IsEnumerable(namedType))
        {
            var elementType = GetElementType(namedType);
            if (elementType != null)
            {
                // 要素のコピー式を取得 (変数は LINQ の 'x')
                var elementCopy = GetCloneExpression(elementType, "x", methodMap);
                var suffix = (type is IArrayTypeSymbol) ? "ToArray()" : "ToList()";

                // 要素コピーが単なる "x" なら Select は不要
                if (elementCopy == "x")
                {
                    return $"{targetExpr} = {sourceExpr}?.{suffix};";
                }
                return $"{targetExpr} = {sourceExpr}?.Select(x => {elementCopy}).{suffix};";
            }
        }

        // 通常のプロパティコピー
        var cloneExpr = GetCloneExpression(type, sourceExpr, methodMap);
        return $"{targetExpr} = {cloneExpr};";
    }

    private static string GetCloneExpression(ITypeSymbol type, string varName, ImmutableDictionary<ITypeSymbol, ExternalCloneMap> methodMap)
    {
        // 優先順位 1: 同一プロジェクト内の Helper メソッド (Partial)
        if (methodMap.TryGetValue(type, out var map))
        {
            if (map.IsDeepClone)
            {
                return $"{map.ContainerName}.{map.MethodName}({varName})";
            }
            else
            {
                // CopyTo型。コンストラクタがある前提で new して流し込む
                //return $"{varName} == null ? null : new {type.ToDisplayString()}() {{ var t = new {type.ToDisplayString()}(); {map.ContainerName}.{map.MethodName}({varName}, t); return t; }}()";

                return $"{varName} == null ? null : (({type.ToDisplayString()} target) => {{ {map.ContainerName}.{map.MethodName}({varName}, target); return target; }})(new {type.ToDisplayString()}())";

                // ※より安全にするなら、即時実行関数ではなく一時変数を使うコードをBuildAssignment側で組む
            }
        }

        // 優先順位 2: 自作属性クラス または IDeepCloneable 実装済み (他プロジェクト含む)
        if (HasDeepCloneAttribute(type) || IsDeepCloneable(type))
        {
            return $"{varName}?.DeepClone()";
        }

        // 優先順位 3: そのまま代入 (値型、文字列など)
        if (IsImmutableOrValueType(type))
        {
            return varName;
        }

        // 優先順位 4: フォールバック
        return varName;
    }

    private static bool IsImmutableOrValueType(ITypeSymbol type)
    {
        //値型 (struct, enum, primitives) はすべてOK
        if (type.IsValueType) return true;

        //特殊型 (string) はOK
        if (type.SpecialType == SpecialType.System_String) return true;

        //参照型だが不変であることが保証されている標準型をホワイトリスト化
        var fullName = type.ToDisplayString();
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

    private static ITypeSymbol? GetElementType(INamedTypeSymbol type)
    {
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
}
