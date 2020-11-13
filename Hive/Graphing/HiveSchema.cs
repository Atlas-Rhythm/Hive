using System;
using GraphQL.Types;
using GraphQL.Utilities;
using Hive.GraphQL.Types;

namespace Hive.Graphing
{
    public class HiveSchema : Schema
    {
        public HiveSchema(IServiceProvider provider)
        {
            Query = provider.GetRequiredService<HiveQuery>();
        }
    }
}