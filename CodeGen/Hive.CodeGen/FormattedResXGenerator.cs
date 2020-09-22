using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hive.CodeGen
{
    [Generator]
    public class FormattedResXGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor DbgPrintFiles = new DiagnosticDescriptor(
            id: "HCG989",
            title: "Located additional file",
            messageFormat: "File: {0}",
            category: "Hive.CodeGen.Debug",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor DbgPrint = new DiagnosticDescriptor(
            id: "HCG988",
            title: "Debug print",
            messageFormat: "{0}",
            category: "Hive.CodeGen.Debug",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        private static bool IsResXToGenerate(GeneratorExecutionContext context, AdditionalText file)
        {
            var options = context.AnalyzerConfigOptions.GetOptions(file);

            if (options.TryGetValue("build_metadata.AdditionalFiles.IsResXToGenerate", out var value))
            {
                if (bool.TryParse(value, out var res))
                    return res;
                return false;
            }

            return false;
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

                bool isRelativeToProject = false;
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
                    context.AddSource($"{ns}.{name}_resx", SourceText.From(result, Encoding.UTF8));
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

            return null;
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
