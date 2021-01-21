using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax cls
                 && cls.Arity > 0
                 && cls.AttributeLists.Count > 0)
                {
                    CandidateClasses.Add(cls);
                }
                else if (syntaxNode is MethodDeclarationSyntax meth
                 && meth.Arity > 0
                 && meth.AttributeLists.Count > 0)
                {
                    CandidateMethods.Add(meth);
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            DebugHelper.Attach();
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            var compilation = context.Compilation;

            var targetingAttribute = compilation.GetTypeByMetadataName("Hive.CodeGen." + nameof(ParameterizeGenericParametersAttribute))!;

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

            var ct = 0;

            foreach (var (type, syn, min, max) in classes)
            {
                var source = GenerateForType(type, targetingAttribute, syn, min, max, context);
                if (source != null)
                {
                    context.AddSource($"Parameterized_{type.Name}_{ct++}.cs", SourceText.From(source, Encoding.UTF8));
                }
            }

            var methods = new List<(IMethodSymbol methSym, MethodDeclarationSyntax syn, int minParam, int maxParam)>();
            foreach (var synMeth in receiver.CandidateMethods)
            {
                var model = compilation.GetSemanticModel(synMeth.SyntaxTree);
                var meth = model.GetDeclaredSymbol(synMeth);
                if (meth is null)
                    continue;

                if (meth.TypeParameters.IsDefaultOrEmpty) continue;

                var targetAttribute = meth.GetAttributes().FirstOrDefault(ad => ad.AttributeClass!.Equals(targetingAttribute, SymbolEqualityComparer.Default));
                if (targetAttribute == null) continue;

                var minTypedArg = targetAttribute.ConstructorArguments[0];
                var maxTypedArg = targetAttribute.ConstructorArguments[1];

                var minValue = (int)minTypedArg.Value!;
                var maxValue = (int)maxTypedArg.Value!;

                methods.Add((meth, synMeth, minValue, maxValue));
            }

            ct = 0;

            foreach (var g in methods
                .GroupBy(t => t.methSym.ContainingType, (IEqualityComparer<INamedTypeSymbol?>)SymbolEqualityComparer.Default))
            {
                var source = GenerateForMethodsOnType(g.Key, targetingAttribute, g.AsEnumerable(), context);
                if (source != null)
                {
                    context.AddSource($"ParameterizedMeth_{g.Key.Name}_{ct++}.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        internal static readonly DiagnosticDescriptor ToParameterizeType = new(
                id: "HCG999",
                title: "Found type to be parameterized",
                messageFormat: "Type to parameterize: {0} with {1} params ({2} to {3}) {4}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor ToRemoveTypeArg = new(
                id: "HCG998",
                title: "Found type argument to be removed",
                messageFormat: "To remove: {0} == {1}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor Report = new(
                id: "HCG997",
                title: "Report",
                messageFormat: "{0}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );

        internal static readonly DiagnosticDescriptor Report2 = new(
                id: "HCG996",
                title: "Report2",
                messageFormat: "{0}",
                category: "Hive.CodeGen.Testing",
                defaultSeverity: DiagnosticSeverity.Hidden,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Err_InvalidParameterizations = new(
                id: "HCG001",
                title: "Specified parameterization range is not valid on the type",
                messageFormat: "Type {0} has {1} generic parameters, but the attribute specified generating {2} to {3} parameter variants",
                category: "Hive.CodeGen.Parameterization",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Err_InternalError = new(
                id: "HCG002",
                title: "Parameterization caused internal exception",
                messageFormat: "Internal exception while generating parameterization {3} of type {0}: '{1}' at {2}",
                category: "Hive.CodeGen.Parameterization",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Err_UnknownTypeKind = new(
                id: "HCG003",
                title: "Parameterization could not continue because of unknown TypeKind on declaring type",
                messageFormat: "Cannot generate parameterizations of methods on type {0} because it is of TypeKind {1}",
                category: "Hive.CodeGen.Parameterization",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

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

            var semModel = context.Compilation.GetSemanticModel(synType.SyntaxTree);

            if (minParam <= 0 || maxParam >= type.Arity)
            {
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

            context.ReportDiagnostic(Diagnostic.Create(Report,
                null,
                synType.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")
            ));

            // remove the original attribute(s)
            synType = (TypeDeclarationSyntax)CloneWithoutAttributes(attribute, synType, context, semModel);

            var fullOriginalType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            context.ReportDiagnostic(Diagnostic.Create(Report,
                null,
                synType.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")
            ));

            var sb = new StringBuilder();

            foreach (var @extern in root.Externs)
            {
                _ = sb.Append(@extern.ToFullString());
            }

            foreach (var @using in root.Usings)
            {
                _ = sb.Append(@using.ToFullString());
            }

            _ = sb.Append($@"
namespace {type.ContainingNamespace.ToDisplayString()}
{{
");

            for (var i = minParam; i <= maxParam; i++)
            {
                try
                {
                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Generating for {i} params"
                    ));

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Trivia before: {synType.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")}"
                    ));

                    TypeDeclarationSyntax generateWith;
                    // add the generated attribute
                    {
                        var attrList = GetInstantiationAttribute(context, fullOriginalType, i);

                        generateWith = synType.AddAttributeLists(attrList);
                        generateWith = synType.CopyAnnotationsTo(generateWith)!;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Trivia after: {generateWith.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")}"
                    ));

                    var decl = GenerateInstantiation(synType.SyntaxTree, generateWith, i, context);
                    var str = decl.ToFullString();

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        str.Replace(Environment.NewLine, "\\n")
                    ));

                    _ = sb.AppendLine(str);
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

            _ = sb.Append(@"
}
");

            var text = sb.ToString();

            context.ReportDiagnostic(Diagnostic.Create(Report2,
                null,
                text.Replace(Environment.NewLine, "\\n")
            ));

            return text;
        }

        [SuppressMessage("Style", "IDE0072:Add missing cases",
            Justification = "All other cases are for types that cannot (or shouldn't be able to) have declared members with the attribute")]
        private string? GenerateForMethodsOnType(INamedTypeSymbol type,
            INamedTypeSymbol attribute,
            IEnumerable<(IMethodSymbol methSym, MethodDeclarationSyntax syn, int minParam, int maxParam)> enumerable,
            GeneratorExecutionContext context)
        {
            var typeSpec = type.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Interface => "interface",
                TypeKind.Struct => "struct",
                _ => null
            };

            if (typeSpec is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Err_UnknownTypeKind,
                    null,
                    type.ToDisplayString(), type.TypeKind));
                return null;
            }

            var sb = new StringBuilder();
            _ = sb.Append(@$"
namespace {type.ContainingNamespace.ToDisplayString()}
{{
    partial {typeSpec} {type.Name}
    {{
");

            foreach (var (methSym, syn, minParam, maxParam) in enumerable)
            {
                try
                {
                    var generated = GenerateForMethod(attribute, methSym, syn, minParam, maxParam, context);
                    foreach (var decl in generated)
                    {
                        var str = decl.ToFullString();

                        context.ReportDiagnostic(Diagnostic.Create(Report,
                            null,
                            str.Replace(Environment.NewLine, "\\n")
                        ));

                        _ = sb.Append(str);
                    }
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Err_InternalError,
                        null,
                        e.GetType(),
                        e.Message.Replace(Environment.NewLine, "\\n"),
                        e.ToString().Replace(Environment.NewLine, "\\n"),
                        -1
                    ));
                }
            }

            _ = sb.Append(@"
    }
}
");

            return sb.ToString();
        }

        private IEnumerable<MethodDeclarationSyntax> GenerateForMethod(INamedTypeSymbol attribute,
            IMethodSymbol methSym,
            MethodDeclarationSyntax methodSyntax,
            int minParam, int maxParam,
            GeneratorExecutionContext context)
        {
            var semModel = context.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);
            var qualifiedSyntax = (MethodDeclarationSyntax)CloneWithoutAttributes(attribute, methodSyntax, context, semModel);

            // I need to do this to get a semantic model for the right method
            var qualCu = SyntaxFactory.CompilationUnit()
                .WithMembers(SyntaxFactory.List(
                    new MemberDeclarationSyntax[] {
                        SyntaxFactory.ClassDeclaration("z")
                            .WithMembers(SyntaxFactory.List(new MemberDeclarationSyntax[] { qualifiedSyntax }))
                    }
                ));
            var qualCuComp = context.Compilation.AddSyntaxTrees(qualCu.SyntaxTree);
            semModel = qualCuComp.GetSemanticModel(qualCu.SyntaxTree);
            qualifiedSyntax = (MethodDeclarationSyntax)((ClassDeclarationSyntax)qualCu.Members.First()).Members.First();
            qualifiedSyntax = (MethodDeclarationSyntax)QualifyTypeNames(qualifiedSyntax, semModel);

            for (var i = minParam; i <= maxParam; i++)
            {
                MethodDeclarationSyntax? decl = null;
                try
                {
                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Generating {methSym.ToDisplayString()} for {i} params"
                    ));

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Trivia before: {qualifiedSyntax.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")}"
                    ));

                    MethodDeclarationSyntax generateWith;
                    // add the generated attribute
                    {
                        var attrList = GetInstantiationAttribute(context, null, i);

                        generateWith = qualifiedSyntax.AddAttributeLists(attrList);
                        generateWith = qualifiedSyntax.CopyAnnotationsTo(generateWith)!;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Report,
                        null,
                        $"Trivia after: {generateWith.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")}"
                    ));

                    decl = (MethodDeclarationSyntax)GenerateInstantiation(methodSyntax.SyntaxTree, generateWith, i, context);
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Err_InternalError,
                        null,
                        e.GetType(),
                        e.Message.Replace(Environment.NewLine, "\\n"),
                        e.ToString().Replace(Environment.NewLine, "\\n"),
                        i
                    ));
                }

                if (decl is null)
                    continue;
                yield return decl;
            }

        }

        private static AttributeListSyntax GetInstantiationAttribute(GeneratorExecutionContext context, string? fullOriginalType, int i)
        {
            var newAttr = context.Compilation.GetTypeByMetadataName("Hive.CodeGen." + nameof(GeneratedParameterizationAttribute))!;

            var attrList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName(newAttr.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SeparatedList(new[] {
                                SyntaxFactory.AttributeArgument(
                                    nameEquals: null,
                                    nameColon: SyntaxFactory.NameColon("from"),
                                    expression: fullOriginalType is null
                                        ? SyntaxFactory.IdentifierName("null")
                                        : SyntaxFactory.TypeOfExpression(
                                            (TypeSyntax)new GenericInstantiationEmptier().Visit(
                                                SyntaxFactory.ParseTypeName(fullOriginalType)
                                            )
                                        )
                                ),
                                SyntaxFactory.AttributeArgument(
                                    nameEquals: null,
                                    nameColon: SyntaxFactory.NameColon("with"),
                                    expression: SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(i)
                                    )
                                )
                            })
                        )
                    )
                )
            );
            return attrList;
        }

        private static MemberDeclarationSyntax CloneWithoutAttributes(INamedTypeSymbol attribute, MemberDeclarationSyntax synType, GeneratorExecutionContext context, SemanticModel semModel)
        {
            var attrLists = synType.AttributeLists;

            var removedAttributeTrivia = new List<SyntaxTriviaList>();

            for (var i = 0; i < attrLists.Count; i++)
            {
                var attrList = attrLists[i];

                context.ReportDiagnostic(Diagnostic.Create(Report,
                    null,
                    attrList.GetLeadingTrivia().ToFullString().Replace(Environment.NewLine, "\\n")
                ));

                if (attrList.Target != null) continue;

                var attrs = attrList.Attributes;
                var attrs2 = attrs;

                for (var j = 0; j < attrs.Count; j++)
                {
                    var attr = attrs[j];

                    var attrSymbol = semModel.GetSymbolInfo(attr.Name).Symbol?.ContainingType;

                    if (attrSymbol?.Equals(attribute, SymbolEqualityComparer.Default) ?? false)
                    {
                        attrs2 = attrs.RemoveAt(j);
                        removedAttributeTrivia.Add(attr.GetLeadingTrivia());
                        removedAttributeTrivia.Add(attr.GetTrailingTrivia());
                        break;
                    }
                }

                if (attrs2.Count != attrs.Count)
                {
                    if (attrs2.Count == 0)
                    {
                        attrLists = attrLists.RemoveAt(i--);
                        synType = attrList.CopyAnnotationsTo(synType)!;
                        removedAttributeTrivia.Add(attrList.GetLeadingTrivia());
                        removedAttributeTrivia.Add(attrList.GetTrailingTrivia());
                    }
                    else
                    {
                        var newAttrList = attrList.WithAttributes(attrs2);
                        newAttrList = attrList.CopyAnnotationsTo(newAttrList)!;
                        attrLists = attrLists.Replace(attrList, newAttrList);
                    }
                }
            }

            // generate attribute to apply trivia to
            {
                var genCodeAttr = context.Compilation.GetTypeByMetadataName("Hive.CodeGen." + nameof(IsGeneratedAttribute))!;

                var attrList = SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.ParseName(genCodeAttr.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                            null
                        )
                    )
                );

                attrList = attrList.WithLeadingTrivia(removedAttributeTrivia.SelectMany(l => l));

                attrLists = attrLists.Insert(0, attrList);
            }

            var copy = synType.WithAttributeLists(attrLists);
            synType = synType.CopyAnnotationsTo(copy)!;

            return synType;
        }

        private sealed class GenericInstantiationEmptier : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
                => SyntaxFactory.TypeArgumentList(
                    node.LessThanToken,
                    SyntaxFactory.SeparatedList<TypeSyntax>(
                        Enumerable.Repeat(SyntaxFactory.OmittedTypeArgument(), node.Arguments.Count)
                    ),
                    node.GreaterThanToken
                );
        }

        private static SyntaxNode QualifyTypeNames(SyntaxNode tree, SemanticModel model)
            => new TypenameQualifier(model).Visit(tree)!;

        private sealed class TypenameQualifier : CSharpSyntaxRewriter
        {
            private readonly SemanticModel semModel;

            public TypenameQualifier(SemanticModel model) : base(true)
                => semModel = model;

            public override SyntaxNode? VisitGenericName(GenericNameSyntax node) => QualifyName(node);
            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) => QualifyName(node);
            public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node) => QualifyName(node);
            public override SyntaxNode? VisitAliasQualifiedName(AliasQualifiedNameSyntax node) => QualifyName(node);

            private SyntaxNode QualifyName(NameSyntax name)
            {
                var symInfo = semModel.GetSymbolInfo(name);
                var symbol = symInfo.Symbol;

                return GetQualifiedName(symbol, name);
            }

            private SyntaxNode GetQualifiedName(ISymbol? symbol, NameSyntax orig)
                => symbol switch
                {
                    var s when s is ITypeSymbol and not ITypeParameterSymbol => SyntaxFactory.ParseName(s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                    IMethodSymbol m when !m.IsExtensionMethod => GetQualifiedName(m.ContainingType, orig), // this should only be triggered for attribute constructors
                    _ => orig
                };
        }

        private static SyntaxNode GenerateInstantiation(SyntaxTree tree, SyntaxNode orig, int paramCount, GeneratorExecutionContext context)
        {
            var transformer = new GenericClassTransformer(context, tree, paramCount);

            return transformer.Visit(orig);
        }
    }
}
