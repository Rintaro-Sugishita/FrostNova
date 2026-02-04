using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

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
    /// annotated with <c>GetSavePathAttribute</c> (if present).</item> </list> <para> The generated code is intended to
    /// simplify working with settings or configuration objects in .NET projects, especially when targeting NativeAOT or
    /// requiring source-generated JSON contexts. </para> <para> To use, annotate a class with
    /// <c>GenerateJsonSettingsAttribute</c>. Optionally, provide a static method with <c>GetSavePathAttribute</c> to
    /// specify a default file path for settings persistence. </para> <para> This generator is designed for use with C#
    /// source generators and requires the <see cref="Microsoft.CodeAnalysis"/> infrastructure. </para></remarks>
    [Generator(LanguageNames.CSharp)]
    public class JsonSettingsGenerator : IIncrementalGenerator
    {
        private const string ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.GenerateJsonSettingsAttribute";
        private const string PATH_ATTRIBUTE_NAME = "FrostNova.Core.NativeAOT.GetSavePathAttribute";

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // [GenerateJsonSettings] が付与されたクラスを抽出
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, _) => GetSemanticTarget(ctx))
                .Where(m => m.Symbol is not null);

            context.RegisterSourceOutput(classDeclarations.Collect(), (spc, source) => Execute(spc, source));
        }

        private static (INamedTypeSymbol? Symbol, string? ContextTypeName) GetSemanticTarget(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (symbol != null)
            {
                var attr = symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == ATTRIBUTE_NAME);
                if (attr != null && attr.ConstructorArguments.Length > 0)
                {
                    // 属性の第1引数 typeof(T) から型名を取得
                    var contextTypeSymbol = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                    return (symbol, contextTypeSymbol?.ToDisplayString());
                }
            }
            return (null, null);
        }
        private static void Execute(SourceProductionContext context, ImmutableArray<(INamedTypeSymbol? Symbol, string? ContextTypeName)> targets)
        {
            if (targets.IsDefaultOrEmpty) return;

            foreach (var target in targets.Where(t => t.Symbol is not null))
            {
                var classSymbol = target.Symbol!;
                var contextTypeName = target.ContextTypeName ?? "UnknownContext";
                // [GetSavePath] が付与されたメソッドを探す
                var getPathMethod = classSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => m.IsStatic && m.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == PATH_ATTRIBUTE_NAME));

                var source = ProcessClass(classSymbol, contextTypeName, getPathMethod?.Name);
                var hintName = classSymbol.ToDisplayString().Replace(".", "_") + ".JsonSettings.g.cs";
                context.AddSource(hintName, source);
            }
        }

        private static string ProcessClass(INamedTypeSymbol classSymbol, string contextTypeName, string? getPathMethodName)
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var contextName = $"{className}JsonContext";

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
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    partial class {className}");
            sb.AppendLine("    {");

            // --- JSON Options & Contexts ---
            // ユーザー指定のコンテキスト型を使用してインスタンスを作成
            sb.AppendLine($@"
        private static readonly JsonSerializerOptions _indentOptions = new JsonSerializerOptions 
        {{ 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = {{ new JsonStringEnumConverter() }}
        }};
        private static readonly {contextTypeName} _indentedContext = new {contextTypeName}(_indentOptions);
        private static readonly {contextTypeName} _defaultContext = {contextTypeName}.Default;");

            // --- String IO ---
            sb.AppendLine($@"
        public static {className} FromJson(string json) => string.IsNullOrEmpty(json) ? new {className}() : JsonSerializer.Deserialize(json, _defaultContext.{className}) ?? new {className}();
        public string ToJson() => JsonSerializer.Serialize(this, _defaultContext.{className});");

            // --- File IO ---
            GenerateFileIOMethods(sb, className, contextTypeName);

            // --- Settings Methods ---
            if (getPathMethodName != null)
            {
                GenerateSettingsMethods(sb, className, getPathMethodName);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GenerateFileIOMethods(StringBuilder sb, string className, string contextTypeName)
        {
            // Sync Load
            sb.AppendLine($@"        /// <summary>
        /// ファイルから設定を読み込みます。 / Loads settings from a file.
        /// </summary>
        public static {className} LoadFromFile(string path)
        {{
            if (!File.Exists(path)) return new {className}();
            try {{
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize(json, _indentedContext.{className}) ?? new {className}();
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
                return new {className}();
            }}
        }}");

            // Sync Save
            sb.AppendLine($@"        /// <summary>
        /// 設定をファイルに保存します。 / Saves settings to a file.
        /// </summary>
        public void SaveToFile(string path)
        {{
            try {{
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(this, _indentedContext.{className});
                File.WriteAllText(path, json);
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
            }}
        }}");

            // Async Load
            sb.AppendLine($@"        /// <summary>
        /// ファイルから設定を非同期で読み込みます。 / Loads settings from a file asynchronously.
        /// </summary>
        public static async Task<{className}> LoadFromFileAsync(string path)
        {{
            if (!File.Exists(path)) return new {className}();
            try {{
                using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync(stream, _indentedContext.{className}) ?? new {className}();
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
                return new {className}();
            }}
        }}");

            // Async Save
            sb.AppendLine($@"        /// <summary>
        /// 設定をファイルに非同期で保存します。 / Saves settings to a file asynchronously.
        /// </summary>
        public async Task SaveToFileAsync(string path)
        {{
            try {{
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using var stream = File.Create(path);
                await JsonSerializer.SerializeAsync(stream, this, _indentedContext.{className});
            }} catch (Exception ex) {{
                Debug.WriteLine(ex.ToString());
            }}
        }}");
        }

        private static void GenerateSettingsMethods(StringBuilder sb, string className, string methodName)
        {
            sb.AppendLine($@"
        /// <summary>
        /// 規定のパスから設定を読み込みます。 / Loads settings from the default path.
        /// </summary>
        public static {className} LoadSettings() => LoadFromFile({methodName}());

        /// <summary>
        /// 規定のパスに設定を保存します。 / Saves settings to the default path.
        /// </summary>
        public void SaveSettings() => SaveToFile({methodName}());

        /// <summary>
        /// 規定のパスから設定を非同期で読み込みます。 / Loads settings from the default path asynchronously.
        /// </summary>
        public static Task<{className}> LoadSettingsAsync() => LoadFromFileAsync({methodName}());

        /// <summary>
        /// 規定のパスに設定を非同期で保存します。 / Saves settings to the default path asynchronously.
        /// </summary>
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