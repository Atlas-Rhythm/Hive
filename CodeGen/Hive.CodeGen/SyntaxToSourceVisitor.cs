using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace Hive.CodeGen
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", 
        Justification = "These methods will only be called through the overrides of CSharpSyntaxVisitor")]
    public sealed class SyntaxToSourceVisitor : CSharpSyntaxVisitor<string>
    {
        private const string Indentation = "    ";
        private static string Indent(string inp)
            => Indentation + inp.Replace(Environment.NewLine, Environment.NewLine + Indentation).Trim();

        public override string? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.ExpressionBody != null)
                return Visit(node.ExpressionBody) + ";";

            var sb = new StringBuilder();
            foreach (var attr in node.AttributeLists)
                sb.Append(Visit(attr)).Append(' ');

            foreach (var tok in node.Modifiers)
            {
                sb.Append(tok.Text)
                  .Append(' ');
            }

            sb.Append(node.Keyword.Text);

            if (node.Body != null)
                sb.AppendLine().Append(Visit(node.Body));

            return sb.ToString();
        }

        public override string? VisitAccessorList(AccessorListSyntax node)
        {
            if (node.Accessors.Count == 1 && node.Accessors[0].ExpressionBody != null)
                return Visit(node.Accessors[0]);

            var sb = new StringBuilder().AppendLine();
            sb.Append('{').AppendLine();

            foreach (var accessor in node.Accessors)
            {
                var content = Indent(Visit(accessor)!);
                sb.AppendLine(content);
            }

            sb.Append('}');

            return sb.ToString();
        }

        public override string? VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            => new StringBuilder(node.Alias.Identifier.Text).Append("::").Append(Visit(node.Name)).ToString();

        public override string? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            var sb = new StringBuilder();

            foreach (var tok in node.Modifiers)
            {
                sb.Append(tok.Text)
                  .Append(' ');
            }

            sb.Append("delegate");

            if (node.ParameterList != null)
            {
                sb.Append(" (").Append(Visit(node.ParameterList)).Append(')');
            }

            sb.AppendLine()
              .Append(Visit(node.Block));

            return sb.ToString();
        }

        public override string? VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            var sb = new StringBuilder("new")
                .AppendLine().AppendLine("{");

            foreach (var init in node.Initializers)
            {
                sb.Append(Indent(Visit(init)!)).AppendLine(",");
            }

            return sb.Append('}').ToString();
        }

        public override string? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
        {
            var sb = new StringBuilder();
            
            if (node.NameEquals != null)
            {
                sb.Append(Visit(node.NameEquals)).Append(' ');
            }

            return sb.Append(Visit(node.Expression)).ToString();
        }

        public override string? VisitArgument(ArgumentSyntax node)
        {
            var sb = new StringBuilder();

            if (node.NameColon != null)
            {
                sb.Append(Visit(node.NameColon)).Append(' ');
            }

            if (!node.RefKindKeyword.Span.IsEmpty)
            {
                sb.Append(node.RefKindKeyword.Text).Append(' ');
            }

            return sb.Append(Visit(node.Expression)).ToString();
        }

        public override string? VisitArgumentList(ArgumentListSyntax node)
        {
            var sb = new StringBuilder("(");

            AppendVisitedSeparatedSyntaxList(sb, node.Arguments);

            return sb.Append(')').ToString();
        }

        private StringBuilder AppendVisitedSeparatedSyntaxList<T>(StringBuilder sb, SeparatedSyntaxList<T> list)
            where T : SyntaxNode
        {
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(Visit(list[i]));
                if (i != list.Count - 1)
                    sb.Append(list.GetSeparator(i).Text).Append(' ');
            }

            return sb;
        }

        public override string? VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            var sb = new StringBuilder("new ")
                .Append(Visit(node.Type));

            if (node.Initializer != null)
            {
                sb.AppendLine()
                  .Append(node.Initializer);
            }

            return sb.ToString();
        }

        public override string? VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node)
        {
            var sb = new StringBuilder("[");

            AppendVisitedSeparatedSyntaxList(sb, node.Sizes);

            return sb.Append(']').ToString();
        }

        public override string? VisitArrayType(ArrayTypeSyntax node)
        {
            var sb = new StringBuilder(Visit(node.ElementType));

            foreach (var spec in node.RankSpecifiers)
            {
                sb.Append(Visit(spec));
            }

            return sb.ToString();
        }

        public override string? VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            => "=> " + Visit(node.Expression);

        public override string? VisitAssignmentExpression(AssignmentExpressionSyntax node)
            => Visit(node.Left) + " " + node.OperatorToken.Text + " " + Visit(node.Right);

        public override string? VisitAttribute(AttributeSyntax node)
            => Visit(node.Name) + (node.ArgumentList != null ? Visit(node.ArgumentList) : "");

        public override string? VisitAttributeArgument(AttributeArgumentSyntax node)
        {
            var sb = new StringBuilder();

            if (node.NameEquals != null)
            {
                sb.Append(Visit(node.NameEquals)).Append(' ');
            }

            if (node.NameColon != null)
            {
                sb.Append(Visit(node.NameColon)).Append(' ');
            }

            return sb.Append(Visit(node.Expression)).ToString();
        }

        public override string? VisitAttributeArgumentList(AttributeArgumentListSyntax node)
        {
            var sb = new StringBuilder("(");

            AppendVisitedSeparatedSyntaxList(sb, node.Arguments);

            return sb.Append(')').ToString();
        }
        public override string? VisitAttributeList(AttributeListSyntax node)
        {
            var sb = new StringBuilder("[");

            if (node.Target != null)
            {
                sb.Append(Visit(node.Target)).Append(' ');
            }

            AppendVisitedSeparatedSyntaxList(sb, node.Attributes);

            return sb.Append(']').ToString();
        }

        public override string? VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node)
            => node.Identifier.Text + ":";

        public override string? VisitAwaitExpression(AwaitExpressionSyntax node)
            => node.AwaitKeyword.Text + " " + Visit(node.Expression);

        // TODO: support trivia
        public override string? VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node) => base.VisitBadDirectiveTrivia(node);

        public override string? VisitBaseExpression(BaseExpressionSyntax node) => "base";

        public override string? VisitBaseList(BaseListSyntax node)
        {
            var sb = new StringBuilder(": ");

            AppendVisitedSeparatedSyntaxList(sb, node.Types);

            return sb.ToString();
        }

        public override string? VisitBinaryExpression(BinaryExpressionSyntax node)
            => Visit(node.Left) + " " + node.OperatorToken.Text + " " + Visit(node.Right);

        public override string? VisitBinaryPattern(BinaryPatternSyntax node)
            => Visit(node.Left) + " " + node.OperatorToken.Text + " " + Visit(node.Right);

        public override string? VisitBlock(BlockSyntax node)
        {
            var sb = new StringBuilder();

            foreach (var attr in node.AttributeLists)
            {
                sb.Append(Visit(attr)).Append(' ');
            }

            sb.AppendLine("{");

            foreach (var stmt in node.Statements)
            {
                sb.AppendLine(Indent(Visit(stmt)!));
            }

            return sb.Append('}').ToString();
        }

        public override string? VisitBracketedArgumentList(BracketedArgumentListSyntax node)
        {
            var sb = new StringBuilder("[");

            AppendVisitedSeparatedSyntaxList(sb, node.Arguments);

            return sb.Append(']').ToString();
        }
        public override string? VisitBracketedParameterList(BracketedParameterListSyntax node)
        {
            var sb = new StringBuilder("[");

            AppendVisitedSeparatedSyntaxList(sb, node.Parameters);

            return sb.Append(']').ToString();
        }
        public override string? VisitBreakStatement(BreakStatementSyntax node)
        {
            var sb = new StringBuilder();

            foreach (var attr in node.AttributeLists)
            {
                sb.Append(Visit(attr)).Append(' ');
            }

            return sb.Append("break;").ToString();
        }

        public override string? VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
        {
            var sb = new StringBuilder(node.Keyword.Text)
                .Append(' ')
                .Append(Visit(node.Pattern));

            if (node.WhenClause != null)
            {
                sb.Append(' ')
                  .Append(Visit(node.WhenClause));
            }

            return sb.Append(':').ToString();
        }

        public override string? VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
            => new StringBuilder(node.Keyword.Text)
                .Append(' ')
                .Append(Visit(node.Value))
                .Append(':').ToString();


        public override string? VisitCastExpression(CastExpressionSyntax node)
            => "(" + Visit(node.Type) + ") " + Visit(node.Expression);

        public override string? VisitCatchClause(CatchClauseSyntax node)
        {
            var sb = new StringBuilder("catch");

            if (node.Declaration != null)
            {
                sb.Append(' ').Append(Visit(node.Declaration)!);
            }

            if (node.Filter != null)
            {
                sb.Append(' ').Append(Visit(node.Filter)!);
            }

            return sb.AppendLine().Append(Visit(node.Block)!).ToString();
        }

        public override string? VisitCatchDeclaration(CatchDeclarationSyntax node)
            => "(" + Visit(node.Type) + " " + node.Identifier.Text + ")";

        public override string? VisitCatchFilterClause(CatchFilterClauseSyntax node)
            => "when (" + Visit(node.FilterExpression) + ")";

        public override string? VisitCheckedExpression(CheckedExpressionSyntax node)
            => node.Keyword.Text + "(" + Visit(node.Expression) + ")";

        public override string? VisitCheckedStatement(CheckedStatementSyntax node)
        {
            var sb = new StringBuilder();

            foreach (var attr in node.AttributeLists)
            {
                sb.Append(Visit(attr)).Append(' ');
            }

            sb.AppendLine(node.Keyword.Text)
              .Append(Visit(node.Block)!);

            return sb.ToString();
        }

        public override string? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var sb = new StringBuilder();

            AppendMemberBase(sb, node);

            sb.Append(node.Keyword.Text)
              .Append(node.Identifier.Text);

            if (node.TypeParameterList != null)
            {
                sb.Append(Visit(node.TypeParameterList));
            }

            if (node.BaseList != null)
            {
                sb.Append(' ').Append(Visit(node.BaseList));
            }

            foreach (var constraint in node.ConstraintClauses)
            {
                sb.AppendLine().Append(Indentation)
                  .Append(Visit(constraint));
            }

            sb.AppendLine().AppendLine("{");

            foreach (var member in node.Members)
            {
                sb.AppendLine(Indent(Visit(member)!));
            }

            return sb.Append('}').ToString();
        }

        public override string? VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node)
            => node.ClassOrStructKeyword.Text + node.QuestionToken.Text;

        public override string? VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var sb = new StringBuilder();

            foreach (var extAlias in node.Externs)
            {
                sb.AppendLine(Visit(extAlias));
            }

            foreach (var @using in node.Usings)
            {
                sb.AppendLine(Visit(@using));
            }

            foreach (var member in node.Members)
            {
                sb.AppendLine(Visit(member));
            }

            return sb.ToString();
        }
        public override string? VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            => Visit(node.Expression) + "? " + Visit(node.WhenNotNull);

        public override string? VisitConditionalExpression(ConditionalExpressionSyntax node)
            => Visit(node.Condition) + " ? " + Visit(node.WhenTrue) + " : " + Visit(node.WhenFalse);

        public override string? VisitConstantPattern(ConstantPatternSyntax node) => Visit(node.Expression)!;

        public override string? VisitConstructorConstraint(ConstructorConstraintSyntax node) => "new()";

        private StringBuilder AppendMemberBase(StringBuilder sb, MemberDeclarationSyntax node)
        {
            foreach (var attr in node.AttributeLists)
            {
                sb.AppendLine(Visit(attr));
            }

            foreach (var mod in node.Modifiers)
            {
                sb.Append(mod.Text).Append(' ');
            }

            return sb;
        }

        private StringBuilder AppendMethodBody(StringBuilder sb, BaseMethodDeclarationSyntax node)
        {
            if (node.ExpressionBody != null)
            {
                sb.Append(Indentation).Append(Visit(node.ExpressionBody)).Append(';');
            }

            if (node.Body != null)
            {
                sb.Append(Visit(node.Body));
            }

            return sb;
        }

        private StringBuilder AppendMethodParamsAndBody(StringBuilder sb, BaseMethodDeclarationSyntax node)
        {
            sb.Append(Visit(node.ParameterList));
            AppendMethodBody(sb.AppendLine(), node);
            return sb;
        }

        public override string? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var sb = new StringBuilder();

            AppendMemberBase(sb, node);

            sb.Append(node.Identifier.Text)
              .Append(Visit(node.ParameterList));

            if (node.Initializer != null)
            {
                sb.Append(' ').Append(Visit(node.Initializer));
            }

            AppendMethodBody(sb.AppendLine(), node);

            return sb.ToString();
        }

        public override string? VisitConstructorInitializer(ConstructorInitializerSyntax node)
            => ": " + node.ThisOrBaseKeyword.Text + Visit(node.ArgumentList);

        public override string? VisitContinueStatement(ContinueStatementSyntax node)
        {
            var sb = new StringBuilder();

            foreach (var attr in node.AttributeLists)
            {
                sb.Append(Visit(attr)).Append(' ');
            }

            return sb.Append("continue;").ToString();
        }

        public override string? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            var sb = new StringBuilder();

            AppendMemberBase(sb, node);

            sb.Append(node.ImplicitOrExplicitKeyword.Text).Append(' ')
              .Append(node.OperatorKeyword.Text).Append(' ')
              .Append(Visit(node.Type));

            AppendMethodParamsAndBody(sb, node);

            return sb.ToString();
        }

        public override string? VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node)
            => node.ImplicitOrExplicitKeyword.Text + " " + node.OperatorKeyword + " " + Visit(node.Type) + (node.Parameters != null ? Visit(node.Parameters) : "");

        public override string? VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node)
            => AppendVisitedSeparatedSyntaxList(new StringBuilder("["), node.Parameters).Append(']').ToString();

        public override string? VisitCrefParameter(CrefParameterSyntax node)
            => (node.RefKindKeyword.Span.IsEmpty ? "" : node.RefKindKeyword.Text + " ") + Visit(node.Type);

        public override string? VisitCrefParameterList(CrefParameterListSyntax node)
            => AppendVisitedSeparatedSyntaxList(new StringBuilder("("), node.Parameters).Append(')').ToString();

        public override string? VisitDeclarationExpression(DeclarationExpressionSyntax node) => base.VisitDeclarationExpression(node);
        public override string? VisitDeclarationPattern(DeclarationPatternSyntax node) => base.VisitDeclarationPattern(node);
        public override string? VisitDefaultConstraint(DefaultConstraintSyntax node) => base.VisitDefaultConstraint(node);
        public override string? VisitDefaultExpression(DefaultExpressionSyntax node) => base.VisitDefaultExpression(node);
        public override string? VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node) => base.VisitDefaultSwitchLabel(node);
        public override string? VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node) => base.VisitDefineDirectiveTrivia(node);
        public override string? VisitDelegateDeclaration(DelegateDeclarationSyntax node) => base.VisitDelegateDeclaration(node);
        public override string? VisitDestructorDeclaration(DestructorDeclarationSyntax node) => base.VisitDestructorDeclaration(node);
        public override string? VisitDiscardDesignation(DiscardDesignationSyntax node) => base.VisitDiscardDesignation(node);
        public override string? VisitDiscardPattern(DiscardPatternSyntax node) => base.VisitDiscardPattern(node);
        public override string? VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node) => base.VisitDocumentationCommentTrivia(node);
        public override string? VisitDoStatement(DoStatementSyntax node) => base.VisitDoStatement(node);
        public override string? VisitElementAccessExpression(ElementAccessExpressionSyntax node) => base.VisitElementAccessExpression(node);
        public override string? VisitElementBindingExpression(ElementBindingExpressionSyntax node) => base.VisitElementBindingExpression(node);
        public override string? VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node) => base.VisitElifDirectiveTrivia(node);
        public override string? VisitElseClause(ElseClauseSyntax node) => base.VisitElseClause(node);
        public override string? VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node) => base.VisitElseDirectiveTrivia(node);
        public override string? VisitEmptyStatement(EmptyStatementSyntax node) => base.VisitEmptyStatement(node);
        public override string? VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node) => base.VisitEndIfDirectiveTrivia(node);
        public override string? VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node) => base.VisitEndRegionDirectiveTrivia(node);
        public override string? VisitEnumDeclaration(EnumDeclarationSyntax node) => base.VisitEnumDeclaration(node);
        public override string? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) => base.VisitEnumMemberDeclaration(node);
        public override string? VisitEqualsValueClause(EqualsValueClauseSyntax node) => base.VisitEqualsValueClause(node);
        public override string? VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node) => base.VisitErrorDirectiveTrivia(node);
        public override string? VisitEventDeclaration(EventDeclarationSyntax node) => base.VisitEventDeclaration(node);
        public override string? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node) => base.VisitEventFieldDeclaration(node);
        public override string? VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node) => base.VisitExplicitInterfaceSpecifier(node);
        public override string? VisitExpressionStatement(ExpressionStatementSyntax node) => base.VisitExpressionStatement(node);
        public override string? VisitExternAliasDirective(ExternAliasDirectiveSyntax node) => base.VisitExternAliasDirective(node);
        public override string? VisitFieldDeclaration(FieldDeclarationSyntax node) => base.VisitFieldDeclaration(node);
        public override string? VisitFinallyClause(FinallyClauseSyntax node) => base.VisitFinallyClause(node);
        public override string? VisitFixedStatement(FixedStatementSyntax node) => base.VisitFixedStatement(node);
        public override string? VisitForEachStatement(ForEachStatementSyntax node) => base.VisitForEachStatement(node);
        public override string? VisitForEachVariableStatement(ForEachVariableStatementSyntax node) => base.VisitForEachVariableStatement(node);
        public override string? VisitForStatement(ForStatementSyntax node) => base.VisitForStatement(node);
        public override string? VisitFromClause(FromClauseSyntax node) => base.VisitFromClause(node);
        public override string? VisitFunctionPointerCallingConvention(FunctionPointerCallingConventionSyntax node) => base.VisitFunctionPointerCallingConvention(node);
        public override string? VisitFunctionPointerParameter(FunctionPointerParameterSyntax node) => base.VisitFunctionPointerParameter(node);
        public override string? VisitFunctionPointerParameterList(FunctionPointerParameterListSyntax node) => base.VisitFunctionPointerParameterList(node);
        public override string? VisitFunctionPointerType(FunctionPointerTypeSyntax node) => base.VisitFunctionPointerType(node);
        public override string? VisitFunctionPointerUnmanagedCallingConvention(FunctionPointerUnmanagedCallingConventionSyntax node) => base.VisitFunctionPointerUnmanagedCallingConvention(node);
        public override string? VisitFunctionPointerUnmanagedCallingConventionList(FunctionPointerUnmanagedCallingConventionListSyntax node) => base.VisitFunctionPointerUnmanagedCallingConventionList(node);
        public override string? VisitGenericName(GenericNameSyntax node) => base.VisitGenericName(node);
        public override string? VisitGlobalStatement(GlobalStatementSyntax node) => base.VisitGlobalStatement(node);
        public override string? VisitGotoStatement(GotoStatementSyntax node) => base.VisitGotoStatement(node);
        public override string? VisitGroupClause(GroupClauseSyntax node) => base.VisitGroupClause(node);
        public override string? VisitIdentifierName(IdentifierNameSyntax node) => base.VisitIdentifierName(node);
        public override string? VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node) => base.VisitIfDirectiveTrivia(node);
        public override string? VisitIfStatement(IfStatementSyntax node) => base.VisitIfStatement(node);
        public override string? VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) => base.VisitImplicitArrayCreationExpression(node);
        public override string? VisitImplicitElementAccess(ImplicitElementAccessSyntax node) => base.VisitImplicitElementAccess(node);
        public override string? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) => base.VisitImplicitObjectCreationExpression(node);
        public override string? VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node) => base.VisitImplicitStackAllocArrayCreationExpression(node);
        public override string? VisitIncompleteMember(IncompleteMemberSyntax node) => base.VisitIncompleteMember(node);
        public override string? VisitIndexerDeclaration(IndexerDeclarationSyntax node) => base.VisitIndexerDeclaration(node);
        public override string? VisitIndexerMemberCref(IndexerMemberCrefSyntax node) => base.VisitIndexerMemberCref(node);
        public override string? VisitInitializerExpression(InitializerExpressionSyntax node) => base.VisitInitializerExpression(node);
        public override string? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => base.VisitInterfaceDeclaration(node);
        public override string? VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node) => base.VisitInterpolatedStringExpression(node);
        public override string? VisitInterpolatedStringText(InterpolatedStringTextSyntax node) => base.VisitInterpolatedStringText(node);
        public override string? VisitInterpolation(InterpolationSyntax node) => base.VisitInterpolation(node);
        public override string? VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node) => base.VisitInterpolationAlignmentClause(node);
        public override string? VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node) => base.VisitInterpolationFormatClause(node);
        public override string? VisitInvocationExpression(InvocationExpressionSyntax node) => base.VisitInvocationExpression(node);
        public override string? VisitIsPatternExpression(IsPatternExpressionSyntax node) => base.VisitIsPatternExpression(node);
        public override string? VisitJoinClause(JoinClauseSyntax node) => base.VisitJoinClause(node);
        public override string? VisitJoinIntoClause(JoinIntoClauseSyntax node) => base.VisitJoinIntoClause(node);
        public override string? VisitLabeledStatement(LabeledStatementSyntax node) => base.VisitLabeledStatement(node);
        public override string? VisitLetClause(LetClauseSyntax node) => base.VisitLetClause(node);
        public override string? VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node) => base.VisitLineDirectiveTrivia(node);
        public override string? VisitLiteralExpression(LiteralExpressionSyntax node) => base.VisitLiteralExpression(node);
        public override string? VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node) => base.VisitLoadDirectiveTrivia(node);
        public override string? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) => base.VisitLocalDeclarationStatement(node);
        public override string? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => base.VisitLocalFunctionStatement(node);
        public override string? VisitLockStatement(LockStatementSyntax node) => base.VisitLockStatement(node);
        public override string? VisitMakeRefExpression(MakeRefExpressionSyntax node) => base.VisitMakeRefExpression(node);
        public override string? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) => base.VisitMemberAccessExpression(node);
        public override string? VisitMemberBindingExpression(MemberBindingExpressionSyntax node) => base.VisitMemberBindingExpression(node);
        public override string? VisitMethodDeclaration(MethodDeclarationSyntax node) => base.VisitMethodDeclaration(node);
        public override string? VisitNameColon(NameColonSyntax node) => base.VisitNameColon(node);
        public override string? VisitNameEquals(NameEqualsSyntax node) => base.VisitNameEquals(node);
        public override string? VisitNameMemberCref(NameMemberCrefSyntax node) => base.VisitNameMemberCref(node);
        public override string? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) => base.VisitNamespaceDeclaration(node);
        public override string? VisitNullableDirectiveTrivia(NullableDirectiveTriviaSyntax node) => base.VisitNullableDirectiveTrivia(node);
        public override string? VisitNullableType(NullableTypeSyntax node) => base.VisitNullableType(node);
        public override string? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) => base.VisitObjectCreationExpression(node);
        public override string? VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node) => base.VisitOmittedArraySizeExpression(node);
        public override string? VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node) => base.VisitOmittedTypeArgument(node);
        public override string? VisitOperatorDeclaration(OperatorDeclarationSyntax node) => base.VisitOperatorDeclaration(node);
        public override string? VisitOperatorMemberCref(OperatorMemberCrefSyntax node) => base.VisitOperatorMemberCref(node);
        public override string? VisitOrderByClause(OrderByClauseSyntax node) => base.VisitOrderByClause(node);
        public override string? VisitOrdering(OrderingSyntax node) => base.VisitOrdering(node);
        public override string? VisitParameter(ParameterSyntax node) => base.VisitParameter(node);
        public override string? VisitParameterList(ParameterListSyntax node) => base.VisitParameterList(node);
        public override string? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) => base.VisitParenthesizedExpression(node);
        public override string? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => base.VisitParenthesizedLambdaExpression(node);
        public override string? VisitParenthesizedPattern(ParenthesizedPatternSyntax node) => base.VisitParenthesizedPattern(node);
        public override string? VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node) => base.VisitParenthesizedVariableDesignation(node);
        public override string? VisitPointerType(PointerTypeSyntax node) => base.VisitPointerType(node);
        public override string? VisitPositionalPatternClause(PositionalPatternClauseSyntax node) => base.VisitPositionalPatternClause(node);
        public override string? VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) => base.VisitPostfixUnaryExpression(node);
        public override string? VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node) => base.VisitPragmaChecksumDirectiveTrivia(node);
        public override string? VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node) => base.VisitPragmaWarningDirectiveTrivia(node);
        public override string? VisitPredefinedType(PredefinedTypeSyntax node) => base.VisitPredefinedType(node);
        public override string? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node) => base.VisitPrefixUnaryExpression(node);
        public override string? VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node) => base.VisitPrimaryConstructorBaseType(node);
        public override string? VisitPropertyDeclaration(PropertyDeclarationSyntax node) => base.VisitPropertyDeclaration(node);
        public override string? VisitPropertyPatternClause(PropertyPatternClauseSyntax node) => base.VisitPropertyPatternClause(node);
        public override string? VisitQualifiedCref(QualifiedCrefSyntax node) => base.VisitQualifiedCref(node);
        public override string? VisitQualifiedName(QualifiedNameSyntax node) => base.VisitQualifiedName(node);
        public override string? VisitQueryBody(QueryBodySyntax node) => base.VisitQueryBody(node);
        public override string? VisitQueryContinuation(QueryContinuationSyntax node) => base.VisitQueryContinuation(node);
        public override string? VisitQueryExpression(QueryExpressionSyntax node) => base.VisitQueryExpression(node);
        public override string? VisitRangeExpression(RangeExpressionSyntax node) => base.VisitRangeExpression(node);
        public override string? VisitRecordDeclaration(RecordDeclarationSyntax node) => base.VisitRecordDeclaration(node);
        public override string? VisitRecursivePattern(RecursivePatternSyntax node) => base.VisitRecursivePattern(node);
        public override string? VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node) => base.VisitReferenceDirectiveTrivia(node);
        public override string? VisitRefExpression(RefExpressionSyntax node) => base.VisitRefExpression(node);
        public override string? VisitRefType(RefTypeSyntax node) => base.VisitRefType(node);
        public override string? VisitRefTypeExpression(RefTypeExpressionSyntax node) => base.VisitRefTypeExpression(node);
        public override string? VisitRefValueExpression(RefValueExpressionSyntax node) => base.VisitRefValueExpression(node);
        public override string? VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node) => base.VisitRegionDirectiveTrivia(node);
        public override string? VisitRelationalPattern(RelationalPatternSyntax node) => base.VisitRelationalPattern(node);
        public override string? VisitReturnStatement(ReturnStatementSyntax node) => base.VisitReturnStatement(node);
        public override string? VisitSelectClause(SelectClauseSyntax node) => base.VisitSelectClause(node);
        public override string? VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node) => base.VisitShebangDirectiveTrivia(node);
        public override string? VisitSimpleBaseType(SimpleBaseTypeSyntax node) => base.VisitSimpleBaseType(node);
        public override string? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => base.VisitSimpleLambdaExpression(node);
        public override string? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) => base.VisitSingleVariableDesignation(node);
        public override string? VisitSizeOfExpression(SizeOfExpressionSyntax node) => base.VisitSizeOfExpression(node);
        public override string? VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node) => base.VisitSkippedTokensTrivia(node);
        public override string? VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node) => base.VisitStackAllocArrayCreationExpression(node);
        public override string? VisitStructDeclaration(StructDeclarationSyntax node) => base.VisitStructDeclaration(node);
        public override string? VisitSubpattern(SubpatternSyntax node) => base.VisitSubpattern(node);
        public override string? VisitSwitchExpression(SwitchExpressionSyntax node) => base.VisitSwitchExpression(node);
        public override string? VisitSwitchExpressionArm(SwitchExpressionArmSyntax node) => base.VisitSwitchExpressionArm(node);
        public override string? VisitSwitchSection(SwitchSectionSyntax node) => base.VisitSwitchSection(node);
        public override string? VisitSwitchStatement(SwitchStatementSyntax node) => base.VisitSwitchStatement(node);
        public override string? VisitThisExpression(ThisExpressionSyntax node) => base.VisitThisExpression(node);
        public override string? VisitThrowExpression(ThrowExpressionSyntax node) => base.VisitThrowExpression(node);
        public override string? VisitThrowStatement(ThrowStatementSyntax node) => base.VisitThrowStatement(node);
        public override string? VisitTryStatement(TryStatementSyntax node) => base.VisitTryStatement(node);
        public override string? VisitTupleElement(TupleElementSyntax node) => base.VisitTupleElement(node);
        public override string? VisitTupleExpression(TupleExpressionSyntax node) => base.VisitTupleExpression(node);
        public override string? VisitTupleType(TupleTypeSyntax node) => base.VisitTupleType(node);
        public override string? VisitTypeArgumentList(TypeArgumentListSyntax node) => base.VisitTypeArgumentList(node);
        public override string? VisitTypeConstraint(TypeConstraintSyntax node) => base.VisitTypeConstraint(node);
        public override string? VisitTypeCref(TypeCrefSyntax node) => base.VisitTypeCref(node);
        public override string? VisitTypeOfExpression(TypeOfExpressionSyntax node) => base.VisitTypeOfExpression(node);
        public override string? VisitTypeParameter(TypeParameterSyntax node) => base.VisitTypeParameter(node);
        public override string? VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node) => base.VisitTypeParameterConstraintClause(node);
        public override string? VisitTypeParameterList(TypeParameterListSyntax node) => base.VisitTypeParameterList(node);
        public override string? VisitTypePattern(TypePatternSyntax node) => base.VisitTypePattern(node);
        public override string? VisitUnaryPattern(UnaryPatternSyntax node) => base.VisitUnaryPattern(node);
        public override string? VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node) => base.VisitUndefDirectiveTrivia(node);
        public override string? VisitUnsafeStatement(UnsafeStatementSyntax node) => base.VisitUnsafeStatement(node);
        public override string? VisitUsingDirective(UsingDirectiveSyntax node) => base.VisitUsingDirective(node);
        public override string? VisitUsingStatement(UsingStatementSyntax node) => base.VisitUsingStatement(node);
        public override string? VisitVariableDeclaration(VariableDeclarationSyntax node) => base.VisitVariableDeclaration(node);
        public override string? VisitVariableDeclarator(VariableDeclaratorSyntax node) => base.VisitVariableDeclarator(node);
        public override string? VisitVarPattern(VarPatternSyntax node) => base.VisitVarPattern(node);
        public override string? VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node) => base.VisitWarningDirectiveTrivia(node);
        public override string? VisitWhenClause(WhenClauseSyntax node) => base.VisitWhenClause(node);
        public override string? VisitWhereClause(WhereClauseSyntax node) => base.VisitWhereClause(node);
        public override string? VisitWhileStatement(WhileStatementSyntax node) => base.VisitWhileStatement(node);
        public override string? VisitWithExpression(WithExpressionSyntax node) => base.VisitWithExpression(node);
        public override string? VisitXmlCDataSection(XmlCDataSectionSyntax node) => base.VisitXmlCDataSection(node);
        public override string? VisitXmlComment(XmlCommentSyntax node) => base.VisitXmlComment(node);
        public override string? VisitXmlCrefAttribute(XmlCrefAttributeSyntax node) => base.VisitXmlCrefAttribute(node);
        public override string? VisitXmlElement(XmlElementSyntax node) => base.VisitXmlElement(node);
        public override string? VisitXmlElementEndTag(XmlElementEndTagSyntax node) => base.VisitXmlElementEndTag(node);
        public override string? VisitXmlElementStartTag(XmlElementStartTagSyntax node) => base.VisitXmlElementStartTag(node);
        public override string? VisitXmlEmptyElement(XmlEmptyElementSyntax node) => base.VisitXmlEmptyElement(node);
        public override string? VisitXmlName(XmlNameSyntax node) => base.VisitXmlName(node);
        public override string? VisitXmlNameAttribute(XmlNameAttributeSyntax node) => base.VisitXmlNameAttribute(node);
        public override string? VisitXmlPrefix(XmlPrefixSyntax node) => base.VisitXmlPrefix(node);
        public override string? VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node) => base.VisitXmlProcessingInstruction(node);
        public override string? VisitXmlText(XmlTextSyntax node) => base.VisitXmlText(node);
        public override string? VisitXmlTextAttribute(XmlTextAttributeSyntax node) => base.VisitXmlTextAttribute(node);
        public override string? VisitYieldStatement(YieldStatementSyntax node) => base.VisitYieldStatement(node);
    }
}
