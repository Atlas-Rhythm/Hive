using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hive.CodeGen
{
    internal sealed class GenericClassTransformer : CSharpSyntaxRewriter
    {
        private readonly GeneratorExecutionContext context;
        private readonly SyntaxTree origTree;
        private readonly int rewriteWithParams;

        public GenericClassTransformer(GeneratorExecutionContext context, SyntaxTree tree, int paramCount) : base(true)
            => (this.context, origTree, rewriteWithParams) = (context, tree, paramCount);

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            => VisitTypeDeclaration(node, node => base.VisitClassDeclaration((ClassDeclarationSyntax)node));

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            => VisitTypeDeclaration(node, node => base.VisitInterfaceDeclaration((InterfaceDeclarationSyntax)node));

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
            => VisitTypeDeclaration(node, node => base.VisitStructDeclaration((StructDeclarationSyntax)node));

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
            => VisitTypeDeclaration(node, node => base.VisitRecordDeclaration((RecordDeclarationSyntax)node));

        private SyntaxNode? CurrentlyRewriting;
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
                var constraints = node.ConstraintClauses;

                List<TypeParameterSyntax> paramsToRemove;
                (typeParamList, constraints, paramsToRemove) = InitialTypeParamFix(typeParamList, constraints);

                node = node.WithTypeParameterList(typeParamList).WithConstraintClauses(constraints);

                TypeParamsToRemove = paramsToRemove;
                CurrentlyRewriting = node;

                var rewritten = orig(node);

                CurrentlyRewriting = null;

                return rewritten;
            }

            return orig(node);
        }

        private SyntaxNode? VisitFirstNodeMethodDecl(MethodDeclarationSyntax node, Func<BaseMethodDeclarationSyntax, SyntaxNode?> orig)
        {
            // this is called if the topmost node we find is a method decl (ie not currently rewriting anything)

            if (node.Arity == 0 || node.TypeParameterList == null)
                throw new InvalidOperationException("First method decl the transformer finds must be generic");

            if (node.Arity < rewriteWithParams)
                throw new InvalidOperationException("Requested rewrite with too few generic parameters");

            var typeParamList = node.TypeParameterList;
            var constraints = node.ConstraintClauses;

            List<TypeParameterSyntax> paramsToRemove;
            (typeParamList, constraints, paramsToRemove) = InitialTypeParamFix(typeParamList, constraints);

            node = node.WithTypeParameterList(typeParamList).WithConstraintClauses(constraints);

            TypeParamsToRemove = paramsToRemove;
            CurrentlyRewriting = node;

            var rewritten = orig(node);

            CurrentlyRewriting = null;

            return rewritten;
        }

        private (TypeParameterListSyntax TypeParams, SyntaxList<TypeParameterConstraintClauseSyntax> Constraints, List<TypeParameterSyntax> ToRemove)
            InitialTypeParamFix(TypeParameterListSyntax typeParamList, SyntaxList<TypeParameterConstraintClauseSyntax> constraints)
        {

            var paramList = typeParamList.Parameters;

            var paramsToRemove = new List<TypeParameterSyntax>();

            var paramList2 = paramList;
            for (var i = paramList.Count - 1; i >= rewriteWithParams; i--)
            {
                var toRemove = paramList[i];
                paramsToRemove.Add(toRemove);
                paramList2 = paramList2.RemoveAt(i);

                var constraintToRemove = constraints.FirstOrDefault(s => s.Name.Identifier.Text == toRemove.Identifier.Text);
                if (constraintToRemove != null)
                    constraints = constraints.Remove(constraintToRemove);

                context.ReportDiagnostic(Diagnostic.Create(
                    GenericParameterizationGenerator.ToRemoveTypeArg,
                    null,
                    toRemove.ToFullString(),
                    constraintToRemove?.ToFullString()
                ));
            }

            typeParamList = typeParamList.WithParameters(paramList2);

            return (typeParamList, constraints, paramsToRemove);
        }

        public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            if (CurrentlyRewriting != null)
            {
                var args = node.Arguments;

                //var args2 = args;
                for (var i = 0; i < args.Count; i++)
                {
                    var arg = args[i];
                    if (arg is IdentifierNameSyntax simple)
                    {
                        foreach (var option in TypeParamsToRemove)
                        {
                            if (simple.Identifier.Text == option.Identifier.Text)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    GenericParameterizationGenerator.ToRemoveTypeArg,
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

                node = node.WithArguments(args);
            }

            return base.VisitTypeArgumentList(node);
        }

        public override SyntaxNode? VisitTupleType(TupleTypeSyntax node)
        {
            if (CurrentlyRewriting != null)
            {
                var args = node.Elements;

                //var args2 = args;
                for (var i = 0; i < args.Count; i++)
                {
                    var arg = args[i].Type;
                    if (arg is IdentifierNameSyntax simple)
                    {
                        foreach (var option in TypeParamsToRemove)
                        {
                            if (simple.Identifier.Text == option.Identifier.Text)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    GenericParameterizationGenerator.ToRemoveTypeArg,
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

                node = node.WithElements(args);
            }

            return base.VisitTupleType(node);
        }

        public override SyntaxNode? VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node)
        {
            if (CurrentlyRewriting != null)
            {
                var nodes = node.Content;

                for (var i = 0; i < nodes.Count; i++)
                {
                    var elem = nodes[i];

                    if (elem is XmlElementSyntax fullElem)
                    {
                        var start = fullElem.StartTag;
                        var name = start.Name;

                        // we don't want to process ones with a prefix
                        if (name.Prefix != null) continue;

                        var attr = start.Attributes.FirstOrDefault(a => a is XmlNameAttributeSyntax) as XmlNameAttributeSyntax;
                        if (attr == null) continue;

                        context.ReportDiagnostic(Diagnostic.Create(GenericParameterizationGenerator.Report,
                            null,
                            $"Node: {elem.ToFullString()}"
                        ));

                        var targetName = attr.Identifier.Identifier.Text;

                        var needsRemoved = false;
                        switch (name.LocalName.Text)
                        {
                            case "typeparam":
                                needsRemoved = TypeParamsToRemove.Any(p => p.Identifier.Text == targetName);
                                break;

                            case "param":
                                if (ParamsToRemove != null)
                                {
                                    needsRemoved =
                                        !(ParamsToKeep != null && ParamsToKeep.Any(p => p.Identifier.Text == targetName))
                                        && ParamsToRemove.Any(p => p.Identifier.Text == targetName);
                                }
                                break;

                            default:
                                continue;
                        }

                        context.ReportDiagnostic(Diagnostic.Create(GenericParameterizationGenerator.Report,
                            null,
                            $"Removed: {needsRemoved}"
                        ));

                        if (needsRemoved)
                        {
                            nodes = nodes.RemoveAt(i--);
                        }
                    }
                }

                node = node.WithContent(nodes);
            }

            return base.VisitDocumentationCommentTrivia(node);
        }

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
            if (CurrentlyRewriting == null && node is MethodDeclarationSyntax meth)
            {
                return VisitFirstNodeMethodDecl(meth, s => VisitBaseMethodDecl(s, orig));
            }

            if (CurrentlyRewriting != null)
            {
                var paramList = node.ParameterList;
                var parameters = paramList.Parameters;

                var removed = new List<ParameterSyntax>();
                var reAdded = new List<ParameterSyntax>();
                for (var i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    if (param.Type is SimpleNameSyntax simple)
                    {
                        foreach (var option in TypeParamsToRemove)
                        {
                            if (simple.Identifier.Text == option?.Identifier.Text)
                            {
                                parameters = parameters.RemoveAt(i--);
                                removed.Add(param);
                                goto @continue;
                            }
                        }
                    }

                    if (ParamsToRemove?.Any(p => p?.Identifier.Text == param.Identifier.Text) ?? false)
                        reAdded.Add(param);

                    @continue:;
                }

                paramList = paramList.WithParameters(parameters);
                node = node.WithParameterList(paramList);

                var oldParamsToRemove = ParamsToRemove;
                ParamsToRemove = removed;
                if (oldParamsToRemove != null)
                    ParamsToRemove = ParamsToRemove.Concat(oldParamsToRemove);

                var oldParamsToKeep = ParamsToKeep;
                ParamsToKeep = reAdded;

                context.ReportDiagnostic(Diagnostic.Create(GenericParameterizationGenerator.Report,
                    null,
                    $"ParamsToRemove: {string.Join(" / ", ParamsToRemove.Select(p => p.ToFullString() + $" ({p.Identifier.Text})"))}"
                ));
                if (ParamsToKeep != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GenericParameterizationGenerator.Report,
                        null,
                        $"ParamsToKeep: {string.Join(" / ", ParamsToKeep.Select(p => p.ToFullString() + $" ({p.Identifier.Text})"))}"
                    ));
                }

                var rewritten = orig(node);

                ParamsToKeep = oldParamsToKeep;

                ParamsToRemove = oldParamsToRemove;

                return rewritten;
            }

            return orig(node);
        }

        public override SyntaxNode? VisitTupleExpression(TupleExpressionSyntax node)
        {
            if (ParamsToRemove != null)
            {
                node = node.WithArguments(FilterArguments(node.Arguments));
            }

            return base.VisitTupleExpression(node);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (ParamsToRemove != null)
            {
                var list = node.ArgumentList;
                list = list.WithArguments(FilterArguments(list.Arguments));
                node = node.WithArgumentList(list);
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (ParamsToRemove != null)
            {
                var list = node.ArgumentList;
                if (list != null)
                {
                    list = list.WithArguments(FilterArguments(list.Arguments));
                    node = node.WithArgumentList(list);
                }
            }

            return base.VisitObjectCreationExpression(node);
        }

        private SeparatedSyntaxList<ArgumentSyntax> FilterArguments(SeparatedSyntaxList<ArgumentSyntax> args)
        {
            if (ParamsToRemove == null) return args;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                context.ReportDiagnostic(Diagnostic.Create(GenericParameterizationGenerator.Report,
                    null,
                    $"Arg: ({arg.Expression.ToFullString()}) {arg.Expression.GetType()}"
                ));

                var visitor = new ReferencesRemovedParamsVisitor(ParamsToRemove, ParamsToKeep);
                visitor.Visit(arg.Expression);

                context.ReportDiagnostic(Diagnostic.Create(GenericParameterizationGenerator.Report,
                    null,
                    $"Needs removal: {visitor.ReferencesParams}"
                ));

                if (visitor.ReferencesParams)
                    args = args.RemoveAt(i--);
            }

            return args;
        }

        private sealed class ReferencesRemovedParamsVisitor : CSharpSyntaxVisitor
        {
            public bool ReferencesParams { get; private set; }

            private readonly IEnumerable<ParameterSyntax> Removed;
            private readonly IEnumerable<ParameterSyntax>? Kept;

            public ReferencesRemovedParamsVisitor(IEnumerable<ParameterSyntax> rem, IEnumerable<ParameterSyntax>? keep)
                => (Removed, Kept) = (rem, keep);

            public override void VisitArgumentList(ArgumentListSyntax node)
            {
            }

            public override void VisitTupleExpression(TupleExpressionSyntax node)
            {
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                var name = node.Identifier.Text;
                if (Kept != null && Kept.Any(p => p.Identifier.Text == name))
                    return;

                if (Removed.Any(p => p.Identifier.Text.Equals(name, StringComparison.Ordinal)))
                    ReferencesParams = true;
            }
        }
    }
}
