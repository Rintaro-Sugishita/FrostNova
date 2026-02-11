using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// Provides a source generator that creates strongly-typed JSON serialization and settings file I/O methods for
    /// classes annotated with a specific attribute.
    /// </summary>
    /// <remarks><para> <b>JsonSettingsGenerator</b> scans for classes marked with the
    /// <c>GenerateJsonSettingsAttribute</c> and generates partial class code that includes: </para> <list
    /// type="bullet">   <item>JSON serialization and deserialization methods using 
    /// System.Text.Json" </item><item>Convenience methods for reading from and writing to settings files,
    /// both synchronously and asynchronously.</item>   <item>Support for custom default file paths via a static method
    /// annotated with <c>JsonSettingPathAttribute</c> (if present).</item> </list> <para> The generated code is intended to
    /// simplify working with settings or configuration objects in .NET projects, especially when targeting NativeAOT or
    /// requiring source-generated JSON contexts. </para> <para> To use, annotate a class with
    /// <c>GenerateJsonSettingsAttribute</c>. Optionally, provide a static method with <c>JsonSettingPathAttribute</c> to
    /// specify a default file path for settings persistence. </para> <para> This generator is designed for use with C#
    /// source generators and requires the <see cref="Microsoft.CodeAnalysis"/> infrastructure. </para></remarks>
    [Generator(LanguageNames.CSharp)]
    public class JsonSettingsGenerator : IIncrementalGenerator
    {
        private const string ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.GenerateJsonSettingsAttribute";
        private const string PATH_ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.JsonSettingPathAttribute";

        #region Diagnostic Rules

        private static readonly DiagnosticDescriptor StaticMethodRule = new DiagnosticDescriptor(
            id: "FNJS001",
            title: "GetSavePath method must be static",
            messageFormat: "The method '{0}' marked with [GetSavePath] must be static",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor NoParametersRule = new DiagnosticDescriptor(
            id: "FNJS002",
            title: "GetSavePath method must have no parameters",
            messageFormat: "The method '{0}' marked with [GetSavePath] must not have any parameters",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ReturnTypeRule = new DiagnosticDescriptor(
            id: "FNJS003",
            title: "GetSavePath method must return string",
            messageFormat: "The method '{0}' marked with [GetSavePath] must return 'string'",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        #endregion

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            System.Diagnostics.Trace.WriteLine("JsonSettingsGenerator is called");

            var version = context.AnalyzerConfigOptionsProvider.Select((options, _) =>
            {
                var assembly = typeof(JsonSettingsGenerator).Assembly;
                string v = assembly.GetName().Version?.ToString() ?? "1.0.0";
                return v ?? "1.0.0"; // フォールバック
            });


            // [GenerateJsonSettings] が付与されたクラスを抽出
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, _) => GetSemanticTarget(ctx))
                .Where(m => m.Symbol is not null);

            var combined = classDeclarations.Combine(version);

            context.RegisterSourceOutput(combined, (spc, source) => Execute(spc, source.Left, source.Right));
        }

        private static (INamedTypeSymbol? Symbol, bool isNullable, string? ContextTypeName, string[] customConverters) GetSemanticTarget(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (symbol != null)
            {
                var attr = symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == ATTRIBUTE_NAME);
                if (attr != null)
                {
                    // 引数がある場合のみ取得、なければ null
                    var contextTypeSymbol = attr.ConstructorArguments.Length > 0
                        ? attr.ConstructorArguments[0].Value as INamedTypeSymbol
                        : null;


                    var converters = new List<string>();
                    if (attr.ConstructorArguments.Length > 1)
                    {
                        var typedConstants = attr.ConstructorArguments[1].Values;
                        foreach (var constant in typedConstants)
                        {
                            if (constant.Value is INamedTypeSymbol converterSymbol)
                            {
                                // 完全修飾名（global::つき）で取得
                                converters.Add(converterSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                            }
                        }
                    }
                    var compilation = context.SemanticModel.Compilation;

                    // まずプロジェクト設定を取得
                    var projectNullable = compilation.Options.NullableContextOptions;

                    // クラスが定義されている「その場所」のコンテキストを取得
                    // これにより、ファイル先頭の #nullable enable 等が全て反映された結果が取れます
                    var nullableContextAtLocation = context.SemanticModel.GetNullableContext(classDeclaration.GetLocation().SourceSpan.Start);

                    // 最終判定：アノテーション（'?'）が有効な状態か
                    bool useNullable = nullableContextAtLocation.HasFlag(NullableContext.AnnotationsEnabled) ||
                                       projectNullable.HasFlag(NullableContextOptions.Annotations);
                    return (symbol, useNullable, contextTypeSymbol?.ToDisplayString(), converters.ToArray());
                }
            }
            return (null, false, null, []);
        }
        private static void Execute(SourceProductionContext context, (INamedTypeSymbol? Symbol, bool isNullable, string? ContextTypeName, string[] customConverters) target, string version)
        {
            //if (targets.IsDefaultOrEmpty) return;

            //foreach (var target in targets.Where(t => t.Symbol is not null))
            //{
            var classSymbol = target.Symbol!;
            var contextTypeName = target.ContextTypeName;
            // [GetSavePath] が付与されたメソッドを探す
            //var getPathMethod = classSymbol.GetMembers()
            //    .OfType<IMethodSymbol>()
            //    .FirstOrDefault(m => m.IsStatic && m.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == PATH_ATTRIBUTE_NAME));

            var allPathMethods = classSymbol.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(m => m.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == PATH_ATTRIBUTE_NAME))
        .ToList();

            IMethodSymbol? validMethod = null;
            foreach (var method in allPathMethods)
            {
                bool hasError = false;

                // 1. static チェック
                if (!method.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(StaticMethodRule, method.Locations[0], method.Name));
                    hasError = true;
                }

                // 2. 引数なしチェック
                if (method.Parameters.Length > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NoParametersRule, method.Locations[0], method.Name));
                    hasError = true;
                }

                // 3. 戻り値 string チェック
                if (method.ReturnType.SpecialType != SpecialType.System_String)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ReturnTypeRule, method.Locations[0], method.Name));
                    hasError = true;
                }
                if (!hasError)
                {
                    validMethod = method;
                    break; // 最初の有効なメソッドを採用
                }
            }

            var source = ProcessClass(classSymbol, target.isNullable, contextTypeName, validMethod?.Name, target.customConverters, version);
            var hintName = classSymbol.ToDisplayString().Replace(".", "_") + ".JsonSettings.g.cs";
            context.AddSource(hintName, source);
            System.Diagnostics.Trace.WriteLine($"{hintName} source is output by JsonSettingsGenerator");


            //}
        }

        private static string ProcessClass(INamedTypeSymbol classSymbol, bool isNullable, string? contextTypeName, string? getPathMethodName, string[] customConverters, string version)
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var contextName = $"{className}JsonContext";

            var nullableAnnotation = isNullable ? "?" : string.Empty;

            var generateAttText = $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"FrostNova.Core.NativeAOT.JsonSettingsGenerator\", \"{version}\")]";

            var accessibility = GetAccessibilityString(classSymbol.DeclaredAccessibility);
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Diagnostics;");
            sb.AppendLine();
            if (isNullable) sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    partial class {className}");
            sb.AppendLine("    {");


            string converterInstances = string.Join(", ", customConverters.Select(c => $"new {c}()"));
            if (!string.IsNullOrEmpty(converterInstances))
            {
                converterInstances = ", " + converterInstances; // カンマで繋ぐ
            }

            if (contextTypeName != null)
            {

                // --- JSON Options & Contexts ---
                // ユーザー指定のコンテキスト型を使用してインスタンスを作成
                sb.AppendLine($"         {generateAttText}");
                sb.AppendLine($@"        private static readonly JsonSerializerOptions _indentOptions = new JsonSerializerOptions 
        {{ 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = {{ new JsonStringEnumConverter(){converterInstances} }}
        }};
        {generateAttText}
        private static readonly {contextTypeName} _indentedContext = new {contextTypeName}(_indentOptions);
        {generateAttText}
        private static readonly {contextTypeName} _defaultContext = {contextTypeName}.Default;");

            }
            else
            {
                // リフレクション用: インデント用と1行に出す版を用意する
                sb.AppendLine($@"
        {generateAttText}
        private static readonly JsonSerializerOptions _indentedOptions =
            new JsonSerializerOptions {{ WriteIndented = true,
                                         Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                         Converters = {{ new JsonStringEnumConverter(){converterInstances} }}
                }};
        {generateAttText}
        private static readonly JsonSerializerOptions _defaultOptions =
            new JsonSerializerOptions {{ WriteIndented = false,
                                         Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                         Converters = {{ new JsonStringEnumConverter(){converterInstances} }}
                }};");
            }


            // --- String IO ---
            if (contextTypeName != null)
            {
                // NativeAOT
                sb.AppendLine($@"
        /// <summary>JSON文字列からインスタンスを生成します。失敗した場合はnullを返します。</summary>
        {generateAttText}
        public static {className}{nullableAnnotation} FromJson(string json) 
        {{
            if (string.IsNullOrWhiteSpace(json)) return null;
            try 
            {{
                return JsonSerializer.Deserialize(json, _indentedContext.{className});
            }}
            catch 
            {{
                return null;
            }}
        }}

        /// <summary>現在のインスタンスをJSON文字列に変換します（日本語非エスケープ・インデント・Enum文字列化）。</summary>
        {generateAttText}
        public string ToJson(bool isIndented = true) => JsonSerializer.Serialize<{className}>(this, isIndented ?  _indentedContext.{className} :  _defaultContext.{className} );
        ");
            }
            else
            {
                // Reflection
                sb.AppendLine($@"
        /// <summary>JSON文字列からインスタンスを生成します。失敗した場合はnullを返します。</summary>
        {generateAttText}
        public static {className}{nullableAnnotation} FromJson(string json) 
        {{
            if (string.IsNullOrWhiteSpace(json)) return null;
            try 
            {{
                return JsonSerializer.Deserialize<{className}>(json, _defaultOptions);
            }}
            catch 
            {{
                return null;
            }}
        }}


        {generateAttText}
        public string ToJson(bool isIndented = true) => JsonSerializer.Serialize(this, isIndented ?  _indentedOptions : _defaultOptions);");
            }

            // --- File IO ---
            GenerateFileIOMethods(sb, nullableAnnotation, className, contextTypeName, generateAttText);

            // --- Settings Methods ---
            if (getPathMethodName != null)
            {
                GenerateSettingsMethods(sb, className, getPathMethodName, generateAttText);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GenerateFileIOMethods(StringBuilder sb, string nullableAnnotation, string className, string? contextTypeName, string generateAttText)
        {
            // Sync Load

            sb.AppendLine($@"

        /// <summary>
        /// ファイルから設定を読み込みます。 / Loads settings from a file.
        /// </summary>
        {generateAttText}
        public static {className}{nullableAnnotation} LoadFromFile(string path)
        {{
            if (!File.Exists(path)) return new {className}();
            try {{
                string json = File.ReadAllText(path);
                return FromJson(json);
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
                return null;
            }}
        }}");

            // Sync Save
            sb.AppendLine($@"        /// <summary>
        /// 設定をファイルに保存します。 / Saves settings to a file.
        /// </summary>
        {generateAttText}
        public void SaveToFile(string path)
        {{
            try {{
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = ToJson();
                File.WriteAllText(path, json);
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
            }}
        }}");

            string? loadAsyncText;
            if (contextTypeName != null)
            {
                loadAsyncText = $@"
                return await JsonSerializer.DeserializeAsync(stream, _indentedContext.{className});";
            }
            else
            {
                loadAsyncText = $@"
                return await JsonSerializer.DeserializeAsync<{className}>(stream, _indentedOptions);";
            }

            // Async Load
            sb.AppendLine($@"        /// <summary>
        /// ファイルから設定を非同期で読み込みます。 / Loads settings from a file asynchronously.
        /// </summary>
        {generateAttText}
        public static async Task<{className}{nullableAnnotation}> LoadFromFileAsync(string path)
        {{
            if (!File.Exists(path)) return new {className}();
            try {{
                using var stream = File.OpenRead(path);
                {loadAsyncText}
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
                return null;
            }}
        }}");


            string? saveAsyncText;
            if (contextTypeName != null)
            {
                saveAsyncText = $@"
                await JsonSerializer.SerializeAsync(stream, this, _indentedContext.{className});";
            }
            else
            {
                saveAsyncText = $@"
                await JsonSerializer.SerializeAsync(stream, this, _indentedOptions);";
            }
            // Async Save
            sb.AppendLine($@"        /// <summary>
        /// 設定をファイルに非同期で保存します。 / Saves settings to a file asynchronously.
        /// </summary>
        {generateAttText}
        public async Task SaveToFileAsync(string path)
        {{
            try {{
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using var stream = File.Create(path);
                {saveAsyncText}
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
            }}
        }}");





        }


        private static void GenerateSettingsMethods(StringBuilder sb, string className, string methodName, string generateAttText)
        {
            sb.AppendLine($@"
        /// <summary>
        /// 規定のパスから設定を読み込みます。 / Loads settings from the default path.
        /// </summary>
        {generateAttText}
        public static {className} LoadSettings() => {className}.LoadFromFile({methodName}()) ?? new {className}();

        /// <summary>
        /// 規定のパスに設定を保存します。 / Saves settings to the default path.
        /// </summary>
        {generateAttText}
        public void SaveSettings() => SaveToFile({methodName}());

        /// <summary>
        /// 規定のパスから設定を非同期で読み込みます。 / Loads settings from the default path asynchronously.
        /// </summary>
        {generateAttText}
        public static async Task<{className}> LoadSettingsAsync()
        {{
             return await {className}.LoadFromFileAsync({methodName}()) ?? new {className}();
        }}
        /// <summary>
        /// 規定のパスに設定を非同期で保存します。 / Saves settings to the default path asynchronously.
        /// </summary>
        {generateAttText}
        public Task SaveSettingsAsync() => SaveToFileAsync({methodName}());");
        }

        private static string GetAccessibilityString(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Protected => "protected",
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                _ => "internal" // フォールバック
            };
        }
    }
}
