using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    internal static class AggregatedMethodGenerator
    {
        public static Delegate Generate(Type iface, MethodInfo toAggregate, Type delegateType)
        {
            var expr = Expression.Lambda(delegateType,
                Expression.Constant(null));

            return expr.Compile();
        }
    }
}
