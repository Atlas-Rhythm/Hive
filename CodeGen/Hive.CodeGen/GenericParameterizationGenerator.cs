using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hive.CodeGen
{
    [Generator]
    public class GenericParameterizationGenerator : ISourceGenerator
    {
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<TypeDeclarationSyntax> CandidateClasses { get; } = new List<TypeDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax cls
                 && cls.Arity > 0
                 && cls.AttributeLists.Count > 0)
                {
                    CandidateClasses.Add(cls);
                }
            }
        }

        private const string AttributesSource = @"
using System;
using System.CodeDom.Compiler;

namespace Hive.CodeGen
{
    /// <summary>Specifies that a generic class will be automatically parameterized within the range specified.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    [GeneratedCode(""Hive.CodeGen"", """")]
    internal sealed class ParameterizeGenericParametersAttribute : Attribute
    {
        public int MinParameters { get; }
        public int MaxParameters { get; }
        /// <param name=""min"">The minimum number of generic parameters to parameterize with.</param>
        /// <param name=""max"">The maximum number of generic parameters to parameterize with.</param>
        public ParameterizeGenericParametersAttribute(int min, int max)
            => (MinParameters, MaxParameters) = (min, max);
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attributes
            context.AddSource("CodeGen_ParameterizeGenericAttributes", SourceText.From(AttributesSource, Encoding.UTF8));

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // create a new compilation with the attribute
            var options = (CSharpParseOptions)((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AttributesSource, Encoding.UTF8), options));

            var targetingAttribute = compilation.GetTypeByMetadataName("Hive.CodeGen.ParameterizeGenericParametersAttribute");

            var classes = new List<(INamedTypeSymbol typeSym, TypeDeclarationSyntax syn, int minParam, int maxParam)>();
            foreach (var synType in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(synType.SyntaxTree);
                var type = model.GetDeclaredSymbol(synType);
                if (type == null)
                    continue;

                if (type.TypeParameters.IsDefaultOrEmpty) continue;

                var targetAttribute = type.GetAttributes().FirstOrDefault(ad => ad.AttributeClass!.Equals(targetingAttribute, SymbolEqualityComparer.Default));
                if (targetAttribute == null) continue;

                var minTypedArg = targetAttribute.ConstructorArguments[0];
                var maxTypedArg = targetAttribute.ConstructorArguments[1];

                var minValue = (int)minTypedArg.Value!;
                var maxValue = (int)maxTypedArg.Value!;

                classes.Add((type, synType, minValue, maxValue));
            }

            foreach (var (type, syn, min, max) in classes)
            {
                var source = GenerateForType(type, syn, min, max, context);
                if (source != null) 
                {
                    context.AddSource($"Parameterized_{type.Name}.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        private static readonly DiagnosticDescriptor ToParameterizeType = new DiagnosticDescriptor(
                    id: "HCG0001",
                    title: "Found type to be parameterized",
                    messageFormat: "Type to parameterize: {0} with {1} params ({2} to {3})",
                    category: "Hive.CodeGen.Testing",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true
                );

        private static string? GenerateForType(INamedTypeSymbol type, TypeDeclarationSyntax synType, int minParam, int maxParam, GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(ToParameterizeType, Location.Create(synType.SyntaxTree, synType.Span), type.Name, type.Arity, minParam, maxParam));

            return null;
        }
    }
}
