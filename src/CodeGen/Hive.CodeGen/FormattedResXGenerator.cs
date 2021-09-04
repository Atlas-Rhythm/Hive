using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Hive.CodeGen
{
    [Generator]
    public class FormattedResXGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor DbgPrintFiles = new(
            id: "HCG989",
            title: "Located additional file",
            messageFormat: "File: {0}",
            category: "Hive.CodeGen.Debug",
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor DbgPrint = new(
            id: "HCG988",
            title: "Debug print",
            messageFormat: "{0}",
            category: "Hive.CodeGen.Debug",
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor Err_CouldNotReadResX = new(
            id: "HCG010",
            title: "Could not read ResX file",
            messageFormat: "Could not read file '{0}'",
            category: "Hive.CodeGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static bool IsResXToGenerate(GeneratorExecutionContext context, AdditionalText file)
        {
            var options = context.AnalyzerConfigOptions.GetOptions(file);

            return options.TryGetValue("build_metadata.AdditionalFiles.IsResXToGenerate", out var value)
                && bool.TryParse(value, out var res) && res;
        }

        private static (string Namespace, string Name) GetFileNameInfo(GeneratorExecutionContext context, AdditionalText file)
        {
            var options = context.AnalyzerConfigOptions.GetOptions(file);

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDir))
                throw new InvalidOperationException("Cannot get project directory");

            if (!options.TryGetValue("build_metadata.AdditionalValues.Namespace", out var @namespace))
            {
                if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
                    rootNamespace = Path.GetFileNameWithoutExtension(projectDir);

                var isRelativeToProject = false;
                var path = file.Path;
                if (options.TryGetValue("build_metadata.AdditionalFiles.Link", out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    isRelativeToProject = true;
                    path = value;
                }

                if (!isRelativeToProject)
                {
                    if (!path.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Cannot figure out where to put the file; specify <Namespace>");
                    path = path.Substring(projectDir.Length).Trim(Path.DirectorySeparatorChar);
                }

                var localNamespace = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '.');

                @namespace = rootNamespace;
                if (!string.IsNullOrWhiteSpace(localNamespace))
                    @namespace += "." + localNamespace;
            }

            if (!options.TryGetValue("build_metadata.AdditionalValues.Name", out var name))
                name = Path.GetFileNameWithoutExtension(file.Path);

            return (@namespace, name);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var allFiles = context.AdditionalFiles
                .Where(f => IsResXToGenerate(context, f));

            foreach (var file in allFiles)
            {
                var result = GenerateForFile(context, file, out var ns, out var name);
                if (result != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DbgPrint,
                        null,
                        result.Replace(Environment.NewLine, "\\n")
                    ));
                    context.AddSource($"{ns}.{name}_resx.g", SourceText.From(result, Encoding.UTF8)); // always add .g so that coverlet doesn't hate us
                }
            }
        }

        private static string? GenerateForFile(GeneratorExecutionContext context, AdditionalText file, out string @namespace, out string name)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DbgPrintFiles,
                null,
                file.Path
            ));

            (@namespace, name) = GetFileNameInfo(context, file);

            context.ReportDiagnostic(Diagnostic.Create(
                DbgPrint,
                null,
                $"Namespace: {@namespace}, Name: {name};"
            ));

            var sourceText = file.GetText(context.CancellationToken);
            if (sourceText == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Err_CouldNotReadResX,
                    Location.None,
                    file.Path
                ));
                return null;
            }

            var sb = new StringBuilder($@"
#nullable enable
namespace {@namespace}
{{
    using System;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{typeof(FormattedResXGenerator).FullName}"", ""1.0.0"")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    internal static class {name}
    {{
        private static global::System.Resources.ResourceManager? resourceMan;

        private static global::System.Globalization.CultureInfo? resourceCulture;

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {{
            get
            {{
                if (object.ReferenceEquals(resourceMan, null))
                {{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(""{@namespace}.{name}"", typeof({name}).Assembly);
                    resourceMan = temp;
                }}
                return resourceMan;
            }}
        }}

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {{
            get
            {{
                return resourceCulture ?? global::System.Globalization.CultureInfo.CurrentUICulture;
            }}
            set
            {{
                resourceCulture = value;
            }}
        }}").AppendLine().AppendLine();

            var xdoc = XElement.Parse(sourceText.ToString(), LoadOptions.SetLineInfo);

            foreach (var elem in xdoc.Elements("data"))
            {
                var elName = elem.Attribute("name").Value;

                var valueEl = elem.Element("value")!;
                var commentEl = elem.Element("comment");

                var baseValue = valueEl.Value.Replace("<", "&lt;").Replace(">", "&gt;");
                var commentString = commentEl?.Value;

                if (commentString != null)
                    commentString = commentString.Replace("<", "&lt;").Replace(">", "&gt;");

                var (commentMain, parsed) = ParseDescriptionComment(commentString);

                _ = sb.Append($@"
        /// <summary>
        /// Gets a resource string for {elName} similar to '{baseValue}'.
        /// </summary>");
                if (commentString != null)
                {
                    _ = sb.Append(@"
        /// <remarks>");
                    if (commentMain != null)
                    {
                        _ = sb.Append($@"
        /// <para>{commentMain}</para>");
                    }

                    if (parsed != null)
                    {
                        _ = sb.Append(@"
        /// <para>Format arguments:
        /// <list type=""table"">");
                        foreach (var (idx, desc) in parsed)
                        {
                            _ = sb.Append($@"
        /// <item>
        ///     <term><b>Argument {idx}</b></term>
        ///     <description>{desc}</description>
        /// </item>");
                        }
                        _ = sb.Append(@"
        /// </list>
        /// </para>");
                    }

                    _ = sb.Append(@"
        /// </remarks>");
                }

                var objType = context.Compilation.GetTypeByMetadataName("System.Object")!;
                var stringType = context.Compilation.GetTypeByMetadataName("System.String")!;

                var typeName = stringType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (parsed != null)
                {
                    var objFullType = SyntaxFactory.ParseTypeName(objType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "?");
                    var valueTupleIdentifier = SyntaxFactory.Identifier(nameof(ValueTuple));
                    var valueTupleQualifier = SyntaxFactory.AliasQualifiedName(
                            "global",
                            SyntaxFactory.IdentifierName("System")
                        );

                    var count = parsed.Count;
                    var typeArgList = SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(
                            Enumerable.Repeat(
                                objFullType,
                                count % 7
                            )
                        )
                    );
                    count -= count % 7;

                    for (; count > 0; count -= 7)
                    {
                        var tuple = SyntaxFactory.QualifiedName(
                            valueTupleQualifier,
                            SyntaxFactory.GenericName(
                                valueTupleIdentifier,
                                typeArgList
                            )
                        );

                        typeArgList = SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList(
                                Enumerable.Repeat(
                                    objFullType,
                                    7
                                ).Concat(new[] { tuple })
                            )
                        );
                    }

                    typeName = SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.AliasQualifiedName(
                                "global",
                                SyntaxFactory.IdentifierName("Hive")
                            ),
                            SyntaxFactory.IdentifierName("Utilities")
                        ),
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("UnformattedString"),
                            typeArgList
                        )
                    ).ToFullString();
                }

                var getResText = $"ResourceManager.GetString(\"{elName}\", Culture)!";
                if (parsed != null)
                    getResText = $"new {typeName}(Culture, {getResText})";

                _ = sb.Append($@"
        internal static {typeName} {elName}
        {{
            get
            {{
                return {getResText};
            }}
        }}
");
            }

            _ = sb.Append(@"
    }
}
");

            return sb.ToString();
        }

        // TODO: support custom type restrictions for parameters
        private static (string? main, IReadOnlyCollection<(int index, string description)>? arguments) ParseDescriptionComment(string? comment)
        {
            if (comment == null) return (null, null);

            var lastIdx = comment.LastIndexOf(';');

            if (lastIdx < 0)
            {
                var parsed = ParseStructuredPart(comment.Trim());
                if (parsed != null)
                    return (null, parsed);
                else
                    return (comment, null);
            }
            else
            {
                var parsed = ParseStructuredPart(comment.Substring(lastIdx + 1).Trim());
                if (parsed != null)
                    return (comment.Substring(0, lastIdx), parsed);
                else
                    return (comment, null);
            }

            static IReadOnlyCollection<(int idx, string desc)>? ParseStructuredPart(string part)
            {
                var list = new List<(int, string)>();

                var lastIdx = -1;
                var lastStart = -1;
                var lastTail = 0;
                while (true)
                {
                    var start = lastTail;
                    do start = part.IndexOf('{', start);
                    while (start != -1 && start - 1 >= 0 && part[start - 1] == '\\');
                    if (start == -1) break;

                    var end = part.IndexOf('}', start);
                    if (end == -1) break;

                    lastTail = end + 1;

                    if (!int.TryParse(part.Substring(start + 1, end - start - 1), out var idx))
                        continue;

                    if (lastIdx != -1)
                    {
                        var lastText = part.Substring(lastStart, start - lastStart).Trim();
                        if (lastText.StartsWith("is", StringComparison.InvariantCultureIgnoreCase))
                            lastText = lastText.Substring(2).Trim();

                        list.Add((lastIdx, lastText.Trim(',')));
                    }

                    lastIdx = idx;
                    lastStart = lastTail;
                }

                if (lastIdx != -1)
                {
                    var lastText = part.Substring(lastStart).Trim();
                    if (lastText.StartsWith("is", StringComparison.InvariantCultureIgnoreCase))
                        lastText = lastText.Substring(2).Trim();

                    list.Add((lastIdx, lastText.Trim(',')));
                }

                if (list.Count == 0) return null;

                return list;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            DebugHelper.Attach();
        }
    }
}
