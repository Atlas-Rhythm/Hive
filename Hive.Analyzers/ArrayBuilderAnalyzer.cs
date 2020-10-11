using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Hive.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArrayBuilderAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Hive.Utilities.ArrayBuilder";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(new[]
            {
                Err_BuilderNotCleanedUp,
            });

        private static readonly DiagnosticDescriptor Err_BuilderNotCleanedUp = new DiagnosticDescriptor(
            id: "Hive0001",
            title: "ArrayBuilder not correctly cleaned up",
            messageFormat: "ArrayBuilder in variable '{0}' not declared in a using declaration",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterOperationAction(OnVariableDeclaration, OperationKind.VariableDeclarator);
        }

        private void OnVariableDeclaration(OperationAnalysisContext ctx)
        {
            var operation = (IVariableDeclaratorOperation)ctx.Operation;

            var symbol = operation.Symbol;

            var type = symbol.Type;

            if (!type.ToDisplayString().StartsWith("Hive.Utilities.ArrayBuilder", StringComparison.Ordinal))
                return; // we don't care about this attr

            var current = operation.Parent;

            while (true)
            {
                if (current is null) 
                    break;

                if (current is IUsingDeclarationOperation)
                    break;

                if (current is IBlockOperation)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Err_BuilderNotCleanedUp,
                        symbol.Locations.FirstOrDefault(),
                        symbol.ToDisplayString()
                    ));
                    break;
                }

                current = current.Parent;
            }
        }
    }
}
