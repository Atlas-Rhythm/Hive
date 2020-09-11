using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Hive.Plugins
{
    internal static class AggregatedInstanceGenerator<T> where T : class
    {
        public  static readonly IEnumerable<(MethodInfo Method, Type DelegateType)> ImplOrder;
        private static readonly Func<Delegate[], IEnumerable<object>, object> Creator;

        static AggregatedInstanceGenerator()
        {
            (ImplOrder, Creator) = AggregatedInstanceGenerator.CreateAggregatedInstance(typeof(T));
        }

        internal static T Create(Delegate[] delegates, IEnumerable<T> impls)
            => (T)Creator(delegates, impls);
    }

    internal static class AggregatedInstanceGenerator
    {
        private static readonly AssemblyBuilder Assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Hive.Plugins.Aggregates"), AssemblyBuilderAccess.RunAndCollect);
        private static readonly ModuleBuilder Module = Assembly.DefineDynamicModule(Assembly.GetName().Name);

        public static (IEnumerable<(MethodInfo Method, Type DelegateType)> ImplOrder, Func<Delegate[], IEnumerable<object>, object> Creator) CreateAggregatedInstance(Type ifaceType)
        {
            if (!ifaceType.IsInterface)
                throw new ArgumentException("Aggregated instances can only be generated from interfaces!", nameof(ifaceType));
            if (ifaceType.GetCustomAttribute<AggregableAttribute>() == null)
                throw new ArgumentException("Aggregated interfaces must be marked [Aggregable]!", nameof(ifaceType));

            var gen = Module.DefineType($"{ifaceType.Namespace}.Aggregate{ifaceType.Name}", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
            gen.AddInterfaceImplementation(ifaceType);

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(ifaceType);
            var aggregateListField = gen.DefineField("_aggregatedImpls", enumerableType, FieldAttributes.Private | FieldAttributes.InitOnly);

            #region IAggregateList
            {
                var aggList = typeof(IAggregateList<>).MakeGenericType(ifaceType);
                gen.AddInterfaceImplementation(aggList);

                var listProp = aggList.GetProperty(nameof(IAggregateList<object>.List)) ?? throw new InvalidOperationException();
                var listGet = listProp.GetGetMethod();

                var listImpl = gen.DefineMethod("get_List", MethodAttributes.Public, enumerableType, Array.Empty<Type>());
                gen.DefineMethodOverride(listImpl, listGet);

                {
                    var il = listImpl.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, aggregateListField);
                    il.Emit(OpCodes.Ret);
                }
            }
            #endregion

            var methods = ifaceType.GetMethods();
            var fields = new List<FieldBuilder>(methods.Length);

            static void EmitLdarg(ILGenerator il, int i)
            {
                switch (i)
                {
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

                var (delType, hasResult) = GetGenericDelegateType(ifaceType, args, ret);

                var typeArgs = args.Select(p => p.ParameterType).Prepend(ifaceType);
                if (hasResult) typeArgs.Append(ret.ParameterType);
                delType = delType.MakeGenericType(typeArgs.ToArray());

                var genField = gen.DefineField($"impl__{method.Name}", delType, FieldAttributes.Private | FieldAttributes.InitOnly);
                fields.Add(genField);

                var genMethod = gen.DefineMethod($"<{method.Name}>", MethodAttributes.Public, ret.ParameterType, args.Select(p => p.ParameterType).ToArray());
                gen.DefineMethodOverride(genMethod, method);

                static ParameterAttributes GetAttrsFor(ParameterInfo param)
                {
                    var attrs = ParameterAttributes.None;
                    if (param.IsIn) attrs |= ParameterAttributes.In;
                    if (param.IsOut) attrs |= ParameterAttributes.Out;
                    if (param.IsLcid) attrs |= ParameterAttributes.Lcid;
                    return attrs;
                }

                genMethod.DefineParameter(0, GetAttrsFor(ret), ret.Name);
                for (int i = 0; i < args.Length; i++)
                {
                    var param = args[i];
                    genMethod.DefineParameter(i + 1, GetAttrsFor(param), param.Name);
                }

                {
                    var il = genMethod.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0); // load this
                    il.Emit(OpCodes.Ldfld, genField);

                    /* the actual delegate invocation */
                    il.Emit(OpCodes.Ldarg_0); // load this, but again

                    for (int i = 1; i <= args.Length; i++)
                    {
                        EmitLdarg(il, i);
                    }

                    var target = delType.GetMethod("Invoke");
                    il.Emit(OpCodes.Tailcall);
                    il.Emit(OpCodes.Callvirt, target);
                    il.Emit(OpCodes.Ret);
                }
            }
            #endregion

            var ctor = gen.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Delegate[]), enumerableType });
            {
                var il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, aggregateListField);

                for (int i = 0; i < fields.Count; i++)
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

            var genType = gen.CreateType();

            var delParams = Expression.Parameter(typeof(Delegate[]), "delegates");
            var enumParams = Expression.Parameter(typeof(IEnumerable<object>), "impls");
            var creator = Expression.Lambda<Func<Delegate[], IEnumerable<object>, object>>(
                Expression.New(ctor, delParams, Expression.Convert(enumParams, enumerableType)),
                delParams, enumParams
            ).Compile();

            return (methods.Zip(fields, (m, f) => (m, f.FieldType)), creator);
        }

        private static (Type delType, bool hasResult) GetGenericDelegateType(Type iface, ParameterInfo[] args, ParameterInfo ret)
        {
            // the first argument will always be an IAggregateList<T0>

            var name = BuildName(args, ret);

            var type = Module.GetType(name);

            if (type != null)
            {
                return (type, ret.ParameterType != typeof(void));
            }

            var newDelType = Module.DefineType(name, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.Sealed, typeof(MulticastDelegate));
            var genericParams = newDelType.DefineGenericParameters(Enumerable.Range(0, 1 + args.Length).Select(i => $"T{i}").ToArray());

            // emit ctor
            var ctor = newDelType.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) });
            ctor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            throw new NotImplementedException();
        }

        private static string BuildName(ParameterInfo[] args, ParameterInfo ret)
        {
            var sb = new StringBuilder();

            sb.Append("Hive.Plugins.Aggregates.D")
              .Append(args.Length);

            static void AppendParam(StringBuilder sb, ParameterInfo p)
            {
                if (p.IsIn)
                    sb.Append("i");
                if (p.IsOut)
                    sb.Append("o");

                if (p.ParameterType.IsByRef)
                    sb.Append("R");
                else
                    sb.Append("N");
            }

            foreach (var p in args) AppendParam(sb, p);

            if (ret.ParameterType == typeof(void))
                sb.Append("V");
            else
                AppendParam(sb, ret);

            return sb.ToString();
        }
    }
}
