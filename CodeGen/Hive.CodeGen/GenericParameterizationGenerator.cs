using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Hive.CodeGen
{
    [Generator]
    public partial class GenericParameterizationGenerator : ISourceGenerator
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

        /*private const string AttributesSource = @"
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
";*/

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attributes
            //context.AddSource("CodeGen_ParameterizeGenericAttributes", SourceText.From(AttributesSource, Encoding.UTF8));

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // create a new compilation with the attribute
            //var options = (CSharpParseOptions)((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options;
            //var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AttributesSource, Encoding.UTF8), options));

            var compilation = context.Compilation;

            var targetingAttribute = compilation.GetTypeByMetadataName(typeof(ParameterizeGenericParametersAttribute).FullName)!;

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
                var source = GenerateForType(type, targetingAttribute, syn, min, max, context);
                if (source != null) 
                {
                    context.AddSource($"Parameterized_{type.Name}.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        private static readonly DiagnosticDescriptor ToParameterizeType = new DiagnosticDescriptor(
                id: "HCG999",
                title: "Found type to be parameterized",
                messageFormat: "Type to parameterize: {0} with {1} params ({2} to {3}) {4}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );
        private static readonly DiagnosticDescriptor ToRemoveTypeArg = new DiagnosticDescriptor(
                id: "HCG998",
                title: "Found type argument to be removed",
                messageFormat: "To remove: {0} == {1}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );
        private static readonly DiagnosticDescriptor Report = new DiagnosticDescriptor(
                id: "HCG997",
                title: "Report",
                messageFormat: "{0}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );
        private static readonly DiagnosticDescriptor Report2 = new DiagnosticDescriptor(
                id: "HCG996",
                title: "Report2",
                messageFormat: "{0}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Err_InvalidParameterizations = new DiagnosticDescriptor(
                id: "HCG001",
                title: "Specified parameterization range is not valid on the type",
                messageFormat: "Type {0} has {1} generic parameters, but the attribute specified generating {2} to {3} parameter variants",
                category: "Hive.CodeGen.Parameterization",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Err_InternalError = new DiagnosticDescriptor(
                id: "HCG002",
                title: "Parameterization caused internal exception",
                messageFormat: "Internal exception while generating parameterization {3} of type {0}: '{1}' at {2}",
                category: "Hive.CodeGen.Parameterization",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "I don't want the excepption propagating any higher, no matter what")]
        private static string? GenerateForType(INamedTypeSymbol type, INamedTypeSymbol attribute, TypeDeclarationSyntax synType, int minParam, int maxParam, GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(ToParameterizeType, 
                Location.Create(synType.SyntaxTree, synType.Identifier.Span), 
                type.Name, 
                type.Arity, 
                minParam, 
                maxParam,
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            ));

            if (minParam <= 0 || maxParam >= type.Arity)
            {
                var semModel = context.Compilation.GetSemanticModel(synType.SyntaxTree);

                var attr = synType.AttributeLists
                    .SelectMany(l => l.Attributes)
                    .First(a => semModel.GetSymbolInfo(a.Name).Symbol?.Equals(attribute, SymbolEqualityComparer.Default) ?? false);

                context.ReportDiagnostic(Diagnostic.Create(Err_InvalidParameterizations,
                    Location.Create(attr.SyntaxTree, attr.Name.Span),
                    type.Name,
                    type.Arity,
                    minParam,
                    maxParam
                ));

                return null;
            }

            var root = synType.SyntaxTree.GetCompilationUnitRoot();

            var sb = new StringBuilder();

            foreach (var @extern in root.Externs)
            {
                sb.Append(@extern.ToFullString());
            }

            foreach (var @using in root.Usings)
            {
                sb.Append(@using.ToFullString());
            }

            // TODO: fix up doc comments

            sb.Append($@"
namespace {type.ContainingNamespace.ToDisplayString()}
{{
#pragma warning disable CS1711
");

            for (int i = minParam; i <= maxParam; i++)
            {
                try
                {
                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Generating for {i} params"
                    ));

                    var decl = GenerateInstantiation(synType.SyntaxTree, synType, i, context);
                    var str = decl.ToFullString();

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        str.Replace(Environment.NewLine, "\\n")
                    ));

                    sb.AppendLine(str);
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Err_InternalError,
                        null,
                        e.GetType(),
                        e.Message,
                        e.TargetSite,
                        i
                    ));
                }
            }

            sb.Append(@"
}
");

            var text = sb.ToString();

            context.ReportDiagnostic(Diagnostic.Create(Report2,
                null,
                text.Replace(Environment.NewLine, "\\n")
            ));

            return text;
        }

        private static SyntaxNode GenerateInstantiation(SyntaxTree tree, TypeDeclarationSyntax orig, int paramCount, GeneratorExecutionContext context)
        {
            var semModel = context.Compilation.GetSemanticModel(tree);

            var transformer = new GenericClassTransformer(context, tree, paramCount);

            return transformer.Visit(orig);
        }

        private sealed class GenericClassTransformer : CSharpSyntaxRewriter
        {
            private readonly GeneratorExecutionContext context;
            private readonly SyntaxTree origTree;
            private readonly int rewriteWithParams;

            public GenericClassTransformer(GeneratorExecutionContext context, SyntaxTree tree, int paramCount)
                => (this.context, origTree, rewriteWithParams) = (context, tree, paramCount);

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) 
                => VisitTypeDeclaration(node, node => base.VisitClassDeclaration((ClassDeclarationSyntax)node));

            public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
                => VisitTypeDeclaration(node, node => base.VisitInterfaceDeclaration((InterfaceDeclarationSyntax)node));

            public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
                => VisitTypeDeclaration(node, node => base.VisitStructDeclaration((StructDeclarationSyntax)node));

            public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
                => VisitTypeDeclaration(node, node => base.VisitRecordDeclaration((RecordDeclarationSyntax)node));

            private TypeDeclarationSyntax? CurrentlyRewriting;
            private IEnumerable<TypeParameterSyntax> TypeParamsToRemove = Enumerable.Empty<TypeParameterSyntax>();

            private SyntaxNode? VisitTypeDeclaration(TypeDeclarationSyntax node, Func<TypeDeclarationSyntax, SyntaxNode?> orig)
            {
                if (CurrentlyRewriting == null)
                {
                    if (node.Arity == 0 || node.TypeParameterList == null)
                        throw new InvalidOperationException("First type decl the transformer finds must be generic");

                    if (node.Arity < rewriteWithParams)
                        throw new InvalidOperationException("Requested rewrite with too few generic parameters");

                    var typeParamList = node.TypeParameterList;
                    var paramList = typeParamList.Parameters;
                    var constraints = node.ConstraintClauses;

                    var paramsToRemove = new List<TypeParameterSyntax>();

                    var paramList2 = paramList;
                    for (int i = paramList.Count - 1; i >= rewriteWithParams; i--)
                    {
                        var toRemove = paramList[i];
                        paramsToRemove.Add(toRemove);
                        paramList2 = paramList2.RemoveAt(i);

                        var constraintToRemove = constraints.FirstOrDefault(s => s.Name.Identifier.Text == toRemove.Identifier.Text);
                        if (constraintToRemove != null)
                            constraints = constraints.Remove(constraintToRemove);

                        context.ReportDiagnostic(Diagnostic.Create(
                            ToRemoveTypeArg,
                            null,
                            toRemove.ToFullString(),
                            constraintToRemove?.ToFullString()
                        ));
                    }

                    typeParamList = typeParamList.WithParameters(paramList2);
                    node = node.WithTypeParameterList(typeParamList).WithConstraintClauses(constraints);

                    TypeParamsToRemove = paramsToRemove;
                    CurrentlyRewriting = node;

                    var rewritten = orig(node);

                    CurrentlyRewriting = null;

                    return rewritten;
                }

                return orig(node);
            }

            public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
            {
                if (CurrentlyRewriting != null)
                {
                    var args = node.Arguments;

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Starting with {args.Count} args"
                    ));

                    //var args2 = args;
                    for (int i = 0; i < args.Count; i++)
                    {
                        var arg = args[i];
                        if (arg is IdentifierNameSyntax simple)
                        {
                            foreach (var option in TypeParamsToRemove)
                            {
                                if (simple.Identifier.Text == option.Identifier.Text)
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(
                                        ToRemoveTypeArg,
                                        Location.Create(origTree, simple.Span),
                                        new[] { Location.Create(origTree, option.Span) },
                                        simple.ToFullString(),
                                        option.ToFullString()
                                    ));

                                    args = args.RemoveAt(i--);
                                    break;
                                }
                            }
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Ending with {args.Count} args"
                    ));

                    node = node.WithArguments(args);

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        node.ToFullString()
                    ));
                }

                return base.VisitTypeArgumentList(node);
            }

            /*
            // This implements rough parameter shadowing
            // Complex cases won't work, but w/e
            private IEnumerable<ParameterSyntax>? ParamsToRemove;
            private IEnumerable<ParameterSyntax>? ParamsToKeep;

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) 
                => VisitBaseMethodDecl(node, node => base.VisitMethodDeclaration((MethodDeclarationSyntax)node));
            public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node) 
                => VisitBaseMethodDecl(node, node => base.VisitConstructorDeclaration((ConstructorDeclarationSyntax)node));

            private SyntaxNode? VisitBaseMethodDecl(BaseMethodDeclarationSyntax node, Func<BaseMethodDeclarationSyntax, SyntaxNode?> orig)
            {
                if (CurrentlyRewriting != null)
                {
                    var paramList = node.ParameterList;
                    var parameters = paramList.Parameters;
                    var params2 = parameters;

                    var removed = new List<ParameterSyntax>();
                    var reAdded = new List<ParameterSyntax>();
                    foreach (var param in parameters)
                    {
                        if (param.Type is SimpleNameSyntax simple)
                        {
                            foreach (var option in TypeParamsToRemove)
                            {
                                if (simple.Identifier.Text == option?.Identifier.Text)
                                {
                                    params2 = params2.Remove(param);
                                    removed.Add(param);
                                    goto @continue;
                                }
                            }
                        }

                        if (ParamsToRemove?.Any(p => p?.Identifier.Text == param.Identifier.Text) ?? false)
                            reAdded.Add(param);

                    @continue:;
                    }

                    paramList = paramList.WithParameters(params2);
                    node = node.WithParameterList(paramList);

                    var oldParamsToRemove = ParamsToRemove;
                    ParamsToRemove = removed;
                    if (oldParamsToRemove != null)
                        ParamsToRemove = ParamsToRemove.Concat(oldParamsToRemove);

                    var oldParamsToKeep = ParamsToKeep;
                    ParamsToKeep = reAdded;

                    var rewritten = orig(node);

                    ParamsToKeep = oldParamsToKeep;

                    ParamsToRemove = oldParamsToRemove;

                    return rewritten;
                }

                return orig(node);
            }
            */

            /*
            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node is null) return null;

                if (ParamsToKeep != null)
                {
                    if (ParamsToKeep.Any(p => p?.Identifier.Text == node.Identifier.Text))
                        return node;
                }

                if (ParamsToRemove != null)
                {
                    if (ParamsToRemove.Any(p => p?.Identifier.Text == node.Identifier.Text))
                        return null;
                }

                if (CurrentlyRewriting != null)
                {
                    if (TypeParamsToRemove.Any(p => p?.Identifier.Text == node.Identifier.Text))
                        return null;
                }

                return node;
            }
            */
        }
    }
}
