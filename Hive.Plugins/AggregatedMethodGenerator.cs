using Hive.Plugins.Resources;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    internal static class AggregatedMethodGenerator
    {
        private delegate ref int RefReturnDel();

        public static Delegate Generate(Type iface, MethodInfo toAggregate, Type delegateType)
        {
            var targetParameters = toAggregate.GetParameters();
            var parameterAttributes = targetParameters.Select(p => (p, a: p.GetCustomAttributes())).ToArray();
            var returnAttributes = toAggregate.ReturnParameter.GetCustomAttributes().AsEnumerable();

            ValidateParam(toAggregate.ReturnParameter, returnAttributes, isRetval: true);
            foreach (var (param, attrs) in parameterAttributes)
                ValidateParam(param, attrs);

            var returnOutInfo = CreateOutParamInfo(returnAttributes, isRet: true);
            var paramInfo = parameterAttributes.Select(t => CreateParamInfo(t.p, t.a)).ToArray();
            foreach (var p in paramInfo) p.ValidateForParams(toAggregate.ReturnType, targetParameters);

            var listParam = Expression.Parameter(typeof(IAggregateList<>).MakeGenericType(iface), "list");
            var lambdaParams = targetParameters.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

            var (returnStorage, returnStorageSet, returnTemp) =
                toAggregate.ReturnType == typeof(void)
                    ? (null, null, null)
                    : (Expression.Variable(toAggregate.ReturnType, "sreturn"),
                       Expression.Variable(typeof(bool), "returnSet"),
                       Expression.Variable(toAggregate.ReturnType, "treturn"));

            var outParamStorage = new ParameterExpression?[targetParameters.Length];
            var outParamStorageSet = new ParameterExpression?[targetParameters.Length];
            var outParamTemps = new ParameterExpression?[targetParameters.Length];
            for (int i = 0; i < targetParameters.Length; i++)
            {
                var param = targetParameters[i];
                if (!param.IsOut) continue;
                // because we will always be seeing an out param, it will always be a ByRef, so we need to GetElementType() to get the variable type.
                var varType = param.ParameterType.GetElementType();
                outParamStorage[i] = lambdaParams[i]; // because this is an out param, our storage is the out param itself //Expression.Variable(varType, $"s_{param.Name}");
                outParamStorageSet[i] = Expression.Variable(typeof(bool), $"is_{param.Name}");
                outParamTemps[i] = Expression.Variable(varType, $"t_{param.Name}");
            }

            var initializers = outParamStorageSet
                .Where(p => p != null)
                .Select(p => Expression.Assign(p!, DefaultForType(p!.Type)));
            if (returnStorage != null)
            {
                initializers = initializers
                    .Prepend(Expression.Assign(returnStorage, DefaultForType(toAggregate.ReturnType)))
                    .Append(Expression.Assign(returnStorageSet!, Expression.Constant(false)));
            }

            var loopBreak = Expression.Label("break");

            var loopBody = new List<Expression>();
            var loopBodyEnd = new List<Expression>();
            if (returnStorage != null)
            {
                loopBody.Add(Expression.Assign(
                            returnStorage,
                            Expression.Condition( // if its not been set, then we always set it
                                returnStorageSet,
                                returnOutInfo.ExpressionAggregator.Aggregate(returnStorage, returnTemp!),
                                returnTemp
                            )
                        ));
                loopBody.Add(Expression.Assign(
                            returnStorageSet,
                            Expression.Constant(true)
                        ));

                var stopIfRet = returnOutInfo.StopIfReturns;
                if (stopIfRet != null)
                {
                    loopBodyEnd.Add(Expression.IfThen(
                                stopIfRet.Test(returnStorage),
                                Expression.Break(loopBreak)
                            ));
                }
            }

            var callArguments = new List<Expression>();
            for (int i = 0; i < paramInfo.Length; i++)
            {
                var info = paramInfo[i];
                if (info.CopiedFromRet)
                {
                    callArguments.Add(
                        Expression.Condition(
                            returnStorageSet,
                            returnStorage,
                            lambdaParams[i]
                        )
                    );
                }
                else if (info.CopiedFromOut != null)
                {
                    callArguments.Add(
                        Expression.Condition(
                            outParamStorageSet[info.CopiedFromOut.Value],
                            outParamStorage[info.CopiedFromOut.Value],
                            lambdaParams[i]
                        )
                    );
                }
                else if (info.OutInfo != null) // is an out param
                {
                    callArguments.Add(outParamTemps[i]!);
                    loopBody.Add(Expression.Assign(
                                outParamStorage[i],
                                Expression.Condition( // if its not been set, then we always set it
                                    outParamStorageSet[i],
                                    info.OutInfo.Value.ExpressionAggregator.Aggregate(outParamStorage[i]!, outParamTemps[i]!),
                                    outParamTemps[i]
                                )
                            ));
                    loopBody.Add(Expression.Assign(
                                outParamStorageSet[i],
                                Expression.Constant(true)
                            ));

                    var stopIfRet = info.OutInfo.Value.StopIfReturns;
                    if (stopIfRet != null)
                    {
                        loopBodyEnd.Add(Expression.IfThen(
                                    stopIfRet.Test(outParamStorage[i]!),
                                    Expression.Break(loopBreak)
                                ));
                    }
                }
                else
                {
                    callArguments.Add(lambdaParams[i]);
                }
            }

            var loopVar = Expression.Variable(iface, "impl");

            Expression callExpr = Expression.Call(loopVar, toAggregate, callArguments);
            if (returnTemp != null)
                callExpr = Expression.Assign(returnTemp, callExpr);
            var loopBodyFinal = loopBody.Prepend(callExpr).Concat(loopBodyEnd);

            var stores = outParamStorageSet.Where(e => e != null);
            if (returnStorage != null)
                stores = stores.Append(returnStorage).Append(returnStorageSet);

            var temps = outParamTemps.Where(e => e != null);
            if (returnTemp != null)
                temps = temps.Append(returnTemp);

            var expr = Expression.Lambda(
                delegateType,
                Expression.Block(
                    toAggregate.ReturnType,
                    stores,
                    initializers.Concat(new[] {
                        new ForeachExpression(
                            Expression.Property(listParam, nameof(IAggregateList<int>.List)),
                            loopVar,
                            loopBreak,
                            Expression.Block(
                                typeof(void),
                                temps,
                                loopBodyFinal
                            )
                        ),
                        returnStorage ?? DefaultForType(toAggregate.ReturnType)
                    })
                ),
                lambdaParams.Prepend(listParam)
            );

            return expr.Compile();
        }

        private static AggregateParameterInfo CreateParamInfo(ParameterInfo param, IEnumerable<Attribute> attrs)
        {
            var inputSpec = attrs.Where(a => a is ISpecifiesInput).Cast<ISpecifiesInput>().SingleOrDefault();

            bool copyFromRet = false;
            int? copyFromOut = null;
            if (inputSpec != null)
            {
                if (inputSpec is TakesOutValueAttribute takeOut)
                    copyFromOut = takeOut.ParameterIndex;
                else if (inputSpec is TakesReturnValueAttribute)
                    copyFromRet = true;
                else
                    throw new InvalidOperationException(SR.Generator_UnknownInputAttribute.Format(inputSpec));
            }

            return new AggregateParameterInfo(
                param,
                copyFromOut,
                copyFromRet,
                param.IsOut ? CreateOutParamInfo(attrs) : new OutParameterInfo?()
            );
        }

        private struct AggregateParameterInfo
        {
            public ParameterInfo Parameter { get; }
            public int Index => Parameter.Position;
            public int? CopiedFromOut { get; }
            public bool CopiedFromRet { get; }

            public readonly OutParameterInfo? OutInfo;

            public AggregateParameterInfo(ParameterInfo param, int? copyFromOut, bool copyFromRet, OutParameterInfo? outInfo)
            {
                Parameter = param;
                CopiedFromOut = copyFromOut;
                CopiedFromRet = copyFromRet;
                OutInfo = outInfo;
            }

            public void ValidateForParams(Type returnType, ParameterInfo[] parameters)
            {
                if (CopiedFromRet && !Parameter.ParameterType.AsNonByRef().IsAssignableFrom(returnType))
                    throw new InvalidOperationException(SR.Generator_ParameterCannotTakeReturn.Format(Parameter));
                if (CopiedFromOut != null)
                {
                    if (CopiedFromOut.Value >= parameters.Length || CopiedFromOut.Value < 0)
                        throw new InvalidOperationException(SR.Generator_InvalidParameterIndex.Format(CopiedFromOut.Value, Parameter));

                    var targetParam = parameters[CopiedFromOut.Value];
                    if (!targetParam.IsOut)
                        throw new InvalidOperationException(SR.Generator_ParameterNotOutParam.Format(CopiedFromOut.Value));
                    if (!Parameter.ParameterType.AsNonByRef().IsAssignableFrom(targetParam.ParameterType.AsNonByRef()))
                        throw new InvalidOperationException(SR.Generator_OutParamNotCompatible.Format(Parameter));
                }
            }
        }
        private static OutParameterInfo CreateOutParamInfo(IEnumerable<Attribute> attrs, bool isRet = false)
        {
            return new OutParameterInfo(
                attrs.Where(a => a is IExpressionAggregator).Cast<IExpressionAggregator>().SingleOrDefault(),
                attrs.Where(a => a is IStopIfReturns).Cast<IStopIfReturns>().SingleOrDefault(),
                isRet
            );
        }

        private struct OutParameterInfo
        {
            public bool IsReturn { get; }
            public IExpressionAggregator ExpressionAggregator { get; }
            public IStopIfReturns? StopIfReturns { get; }

            public OutParameterInfo(IExpressionAggregator? exprAgg, IStopIfReturns? stopIfRet, bool isRet = false)
            {
                ExpressionAggregator = exprAgg ?? new ReturnLastAttribute();
                StopIfReturns = stopIfRet;
                IsReturn = isRet;
            }
        }

        // Note: The below method and type exist because expression trees *cannot* take a ref of a variable, but they *can* pass around references
        //       So, I have a small type and wrapper function to do it for me.
        private static Expression DefaultForType(Type type)
            => type.IsByRef
            ? Expression.Call(Expression.New(typeof(DefaultByRef<>).MakeGenericType(type.GetElementType())), nameof(DefaultByRef<object>.ByRefDefault), null)
            : (Expression)Expression.Default(type);

        private class DefaultByRef<T>
        {
            public T Default = default!;
            public ref T ByRefDefault() => ref Default;
        }

        private static void ValidateParam(ParameterInfo param, IEnumerable<Attribute> attrs, bool isRetval = false)
        {
            foreach (var attr in attrs)
            {
                if (!CheckAttribute(param, attr, isRetval))
                    throw new InvalidOperationException(SR.Generator_AttributeInvalidOn.Format(attr, param));
            }
        }

        private static bool CheckAttribute(ParameterInfo param, Attribute attr, bool isRetval = false)
        {
            if (!(attr is IAggregatorAttribute)) return true;
            if (!CheckAttributeTarget(param, attr, isRetval)) return false;
            if (attr is IRequiresType reqTy) return reqTy.CheckType(param.ParameterType.AsNonByRef());
            return true;
        }

        private static bool CheckAttributeTarget(ParameterInfo param, Attribute attr, bool isRetval = false)
        {
            if (param.IsRetval || isRetval) return attr is ITargetsReturn;
            if (param.IsOut) return attr is ITargetsOutParam;
            return attr is ITargetsInParam;
        }
    }
}
