using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Hive.Plugins.Aggregates;
using Hive.Plugins.Resources;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo(AggregatedInstanceGenerator.AssemblyName)]

namespace Hive.Plugins.Aggregates
{
    internal static class AggregatedInstanceGenerator<T> where T : class
    {
        private static IEnumerable<(MethodInfo Method, Type DelegateType)>? implOrder;
        private static Func<Delegate[], IEnumerable<object>, object>? creator;
        private static Delegate[]? methodImpls;
        private static AggregableAttribute? attribute;

        private static void LazySetup()
        {
            lock (AggregatedInstanceGenerator.Lock)
            {
                if (implOrder != null) return;
                (implOrder, creator) = AggregatedInstanceGenerator.CreateAggregatedInstance(typeof(T));
            }

            attribute = typeof(T).GetCustomAttribute<AggregableAttribute>();

            methodImpls = implOrder
                .Select(t => AggregatedMethodGenerator.Generate(typeof(T), t.Method, t.DelegateType))
                .ToArray();
        }

        internal static T Create(IEnumerable<T> impls, IServiceProvider services)
        {
            if (creator == null) LazySetup();
            if (!impls.Any() && attribute?.Default is { } defaultType)
            {
                impls = new[] { (T)ActivatorUtilities.GetServiceOrCreateInstance(services, defaultType) };
            }
            return (T)creator!(methodImpls!, impls);
        }
    }

    internal static class AggregatedInstanceGenerator
    {
        internal static readonly object Lock = new();

        public const string AssemblyName = "Hive.Plugins.Aggregates";

        private static readonly AssemblyBuilder Assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.RunAndCollect);
        private static readonly ModuleBuilder Module = Assembly.DefineDynamicModule(AssemblyName);

        private static ParameterAttributes GetAttrsFor(ParameterInfo param)
            => param.Attributes;

        internal static Type AsNonByRef(this Type type)
            => type.IsByRef ? type.GetElementType()! : type;

        public static (IEnumerable<(MethodInfo Method, Type DelegateType)> ImplOrder, Func<Delegate[], IEnumerable<object>, object> Creator) CreateAggregatedInstance(Type ifaceType)
        {
            if (!ifaceType.IsInterface)
                throw new ArgumentException(SR.Generator_CanOnlyAggregateInterfaces, nameof(ifaceType));
            if (ifaceType.GetCustomAttribute<AggregableAttribute>() == null)
                throw new ArgumentException(SR.Generator_AggregateMustBeAggregable, nameof(ifaceType));

            var gen = Module.DefineType($"{ifaceType.Namespace}.Aggregate{ifaceType.Name}", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass);
            gen.AddInterfaceImplementation(ifaceType);

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(ifaceType);
            var aggregateListField = gen.DefineField("_aggregatedImpls", enumerableType, FieldAttributes.Private | FieldAttributes.InitOnly);

            const MethodAttributes MethodAttrs = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Final;

            #region IAggregateList

            {
                var aggList = typeof(IAggregateList<>).MakeGenericType(ifaceType);
                gen.AddInterfaceImplementation(aggList);

                var listProp = aggList.GetProperty(nameof(IAggregateList<object>.List)) ?? throw new InvalidOperationException();
                var listGet = listProp.GetGetMethod()!;

                var listImpl = gen.DefineMethod("get_List", MethodAttrs, enumerableType, Array.Empty<Type>());
                gen.DefineMethodOverride(listImpl, listGet);

                {
                    var il = listImpl.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, aggregateListField);
                    il.Emit(OpCodes.Ret);
                }
            }

            #endregion IAggregateList

            var methods = ifaceType.GetMethods();
            var fields = new List<FieldBuilder>(methods.Length);

            static void EmitLdarg(ILGenerator il, int i)
            {
                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;

                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;

                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;

                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;

                    case var j when j <= byte.MaxValue:
                        il.Emit(OpCodes.Ldarg_S, j);
                        break;

                    default:
                        il.Emit(OpCodes.Ldarg, i);
                        break;
                }
            }

            #region Method Implementation

            foreach (var method in methods)
            {
                var args = method.GetParameters();
                var ret = method.ReturnParameter;

                var (delType, hasResult) = GetGenericDelegateType(args, ret);

                var typeArgs = args.Select(p => p.ParameterType).Prepend(ifaceType);
                if (hasResult) typeArgs = typeArgs.Append(ret.ParameterType);
                delType = delType.MakeGenericType(typeArgs.Select(AsNonByRef).ToArray());

                var genField = gen.DefineField($"impl__{method.Name}", delType, FieldAttributes.Private | FieldAttributes.InitOnly);
                fields.Add(genField);

                var genMethod = gen.DefineMethod(
                    $"<{method.Name}>",
                    MethodAttrs,
                    CallingConventions.Standard,
                    ret.ParameterType,
                    ret.GetRequiredCustomModifiers(),
                    ret.GetOptionalCustomModifiers(),
                    args.Select(p => p.ParameterType).ToArray(),
                    args.Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                    args.Select(p => p.GetOptionalCustomModifiers()).ToArray()
                );
                gen.DefineMethodOverride(genMethod, method);

                _ = genMethod.DefineParameter(0, GetAttrsFor(ret), ret.Name);

                for (var i = 0; i < args.Length; i++)
                {
                    var param = args[i];
                    _ = genMethod.DefineParameter(i + 1, GetAttrsFor(param), param.Name);
                }

                {
                    var il = genMethod.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0); // load this
                    il.Emit(OpCodes.Ldfld, genField);

                    /* the actual delegate invocation */
                    il.Emit(OpCodes.Ldarg_0); // load this, but again

                    for (var i = 1; i <= args.Length; i++)
                    {
                        EmitLdarg(il, i);
                    }

                    var target = delType.GetMethod("Invoke")!;
                    il.Emit(OpCodes.Tailcall);
                    il.Emit(OpCodes.Callvirt, target);
                    il.Emit(OpCodes.Ret);
                }
            }

            #endregion Method Implementation

            var ctor = gen.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Delegate[]), enumerableType });
            {
                var il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, aggregateListField);

                for (var i = 0; i < fields.Count; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Castclass, fields[i].FieldType);
                    il.Emit(OpCodes.Stfld, fields[i]);
                }

                il.Emit(OpCodes.Ret);
            }

            var genType = gen.CreateType()!;

            var delParams = Expression.Parameter(typeof(Delegate[]), "delegates");
            var enumParams = Expression.Parameter(typeof(IEnumerable<object>), "impls");
            var creator = Expression.Lambda<Func<Delegate[], IEnumerable<object>, object>>(
                Expression.New(genType.GetConstructor(new[] { typeof(Delegate[]), enumerableType })!, delParams, Expression.Convert(enumParams, enumerableType)),
                delParams, enumParams
            ).Compile();

            return (methods.Zip(fields, (m, f) => (m, f.FieldType)).ToArray(), creator);
        }

        private static (Type delType, bool hasResult) GetGenericDelegateType(ParameterInfo[] args, ParameterInfo ret)
        {
            // the first argument will always be an IAggregateList<T0>

            var hasResult = ret.ParameterType != typeof(void);

            var name = BuildName(args, ret);

            var type = Module.GetType(name);

            if (type != null)
            {
                return (type, hasResult);
            }

            var newDelType = Module.DefineType(name, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.Sealed, typeof(MulticastDelegate));
            var genericParams = newDelType.DefineGenericParameters(Enumerable.Range(0, 1 + args.Length + (hasResult ? 1 : 0)).Select(i => $"T{i}").ToArray());
            // genericParams[0] is the argument to IAggregateList, and is invariant
            var iface = genericParams[0];

            var argParams = new GenericTypeParameterBuilder[args.Length];
            for (var i = 1; i <= args.Length; i++)
            {
                // all the argument types should be contravariant (if possible
                if (!args[i - 1].ParameterType.IsByRef)
                    genericParams[i].SetGenericParameterAttributes(GenericParameterAttributes.Contravariant);
                argParams[i - 1] = genericParams[i];
            }

            // return type (if present) should be covariant
            GenericTypeParameterBuilder? resultParam = null;
            if (hasResult)
            {
                resultParam = genericParams[^1];
                if (!ret.ParameterType.IsByRef)
                    resultParam.SetGenericParameterAttributes(GenericParameterAttributes.Covariant);
            }

            // emit ctor
            var ctor = newDelType.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) });
            ctor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            var coreArgTypes = new Type[args.Length + 1];
            coreArgTypes[0] = typeof(IAggregateList<>).MakeGenericType(iface);
            for (var i = 0; i < args.Length; i++)
            {
                var param = args[i];
                Type gtype = argParams[i];
                if (param.ParameterType.IsByRef)
                    gtype = gtype.MakeByRefType();
                coreArgTypes[i + 1] = gtype;
            }

            var retType = hasResult ? genericParams[^1] : typeof(void);
            if (hasResult && ret.ParameterType.IsByRef)
                retType = retType.MakeByRefType();

            var invoke = newDelType.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                retType, coreArgTypes);
            invoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            _ = invoke.DefineParameter(0, GetAttrsFor(ret), "return");
            _ = invoke.DefineParameter(1, ParameterAttributes.None, "inst");
            for (var i = 0; i < args.Length; i++)
            {
                _ = invoke.DefineParameter(i + 2, GetAttrsFor(args[i]), $"arg{i}");
            }

            var beginInvoke = newDelType.DefineMethod("BeginInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                typeof(IAsyncResult), coreArgTypes.Append(typeof(AsyncCallback)).Append(typeof(object)).ToArray());
            beginInvoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            _ = beginInvoke.DefineParameter(1, ParameterAttributes.None, "inst");
            for (var i = 0; i < args.Length; i++)
            {
                _ = beginInvoke.DefineParameter(i + 2, GetAttrsFor(args[i]), $"arg{i}");
            }

            var endInvoke = newDelType.DefineMethod("EndInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                retType, new[] { typeof(IAsyncResult) });
            endInvoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
            _ = endInvoke.DefineParameter(0, GetAttrsFor(ret), "return");
            _ = endInvoke.DefineParameter(1, ParameterAttributes.None, "result");

            type = newDelType.CreateType()!;

            return (type, hasResult);
        }

        private static string BuildName(ParameterInfo[] args, ParameterInfo ret)
        {
            var sb = new StringBuilder();

            _ = sb.Append($"{AssemblyName}.D")
              .Append(args.Length);

            static void AppendParam(StringBuilder sb, ParameterInfo p)
            {
                if (p.ParameterType.IsByRef)
                    _ = sb.Append('R');
                else
                    _ = sb.Append('N');

                if (p.IsIn)
                    _ = sb.Append('i');
                if (p.IsOut)
                    _ = sb.Append('o');
            }

            foreach (var p in args) AppendParam(sb, p);

            if (ret.ParameterType == typeof(void))
                _ = sb.Append('V');
            else
                AppendParam(sb, ret);

            return sb.ToString();
        }
    }
}
