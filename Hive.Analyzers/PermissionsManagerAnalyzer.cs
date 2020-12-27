using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;

namespace Hive.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PermissionsManagerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(new[]
            {
                Wrn_UseStringLiteralForActionString,
                Wrn_UseActionParseStateOverload,
                Wrn_UseNonActionParseStateOverloadForRuntime,
                Wrn_UnknownCanDoOverload,
            });

        private static readonly DiagnosticDescriptor Wrn_UseStringLiteralForActionString
            = new(
                id: "Hive0011",
                title: "Action strings provided to PermissionManager.CanDo should be string literals",
                messageFormat: "Permission action strings should be string literals, because one location should only check one permission",
                category: "Hive.Permissions",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Wrn_UseActionParseStateOverload
            = new(
                id: "Hive0012",
                title: "Use the CanDo(StringView, TContext, ref PermissionActionParseState) overload when possible",
                messageFormat: "Use the action parse state overload of CanDo when possible to avoid re-parsing the action string",
                category: "Hive.Permissions",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Wrn_UseNonActionParseStateOverloadForRuntime
            = new(
                id: "Hive0013",
                title: "Use the CanDo(StringView, TContext) overload for runtime-specified actions",
                messageFormat: "Use the non-ActionParseState overload of CanDo when the action string isn't known until runtime",
                category: "Hive.Permissions",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Wrn_UnknownCanDoOverload
            = new(
                id: "Hive0019",
                title: "Unknown overload of CanDo",
                messageFormat: "This overload of CanDo is unrecognized by the analyzer",
                category: "Hive.Permissions",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterOperationAction(OnMethodCall, OperationKind.Invocation);
        }

        private void OnMethodCall(OperationAnalysisContext ctx)
        {
            var invocation = (IInvocationOperation)ctx.Operation;

            var targetMethod = invocation.TargetMethod;

            if (targetMethod.MethodKind != MethodKind.Ordinary)
                return;

            var owningType = targetMethod.ContainingType;

            if (!owningType.IsGenericType)
                return;

            var typeDef = owningType.ConstructedFrom;

            var permissionManager = ctx.Compilation.GetTypeByMetadataName("Hive.Permissions.PermissionsManager`1");
            if (permissionManager == null) return;

            if (!typeDef.Equals(permissionManager, SymbolEqualityComparer.Default))
                return;

            if (targetMethod.Name != "CanDo")
                return;

            var argumentList = invocation.Arguments;

            if (argumentList.Length is < 2 or > 3)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Wrn_UnknownCanDoOverload,
                    invocation.Syntax.GetLocation()
                ));
                return;
            }

            var actStringIsConst = true;
            var actStringArg = argumentList[0];
            var actStringValue = actStringArg.Value;

            if (actStringValue is IConversionOperation conversion)
            {
                var conv = conversion.GetConversion();

                if (conv.IsUserDefined && conv.IsImplicit)
                {
                    actStringValue = conversion.Operand;
                }
                else
                {
                    actStringIsConst = false;
                }
            }

            actStringIsConst &= actStringValue.ConstantValue.HasValue;

            // TODO: improve reporting here (how do I want them reported?)
            //       It currently doesn't actually report all the cases I want it to, but I'm not sure
            //         the rules I want it to report based on.

            if (argumentList.Length == 2)
            {
                if (actStringIsConst)
                {
                    /*ctx.ReportDiagnostic(Diagnostic.Create(
                        Wrn_UseActionParseStateOverload,
                        invocation.Syntax.GetLocation()
                    ));*/
                }
                else
                {
                    // all is good
                }

                // we report unconditionally because we want to prefer the ActionParseState overload
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Wrn_UseActionParseStateOverload,
                    invocation.Syntax.GetLocation()
                ));
            }

            if (argumentList.Length == 3)
            {
                if (actStringIsConst)
                {
                    // TODO: check that the referenced variable persists for a long time and is never used with a different action string
                    // all is good
                }
                else
                {
                    /*ctx.ReportDiagnostic(Diagnostic.Create(
                        Wrn_UseStringLiteralForActionString,
                        invocation.Syntax.GetLocation()
                    ));*/

                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Wrn_UseNonActionParseStateOverloadForRuntime,
                        invocation.Syntax.GetLocation()
                    ));
                }
            }
        }
    }
}
