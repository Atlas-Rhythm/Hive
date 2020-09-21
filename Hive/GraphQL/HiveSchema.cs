using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace Hive.GraphQL
{
    public class HiveSchema : Schema
    {
        public HiveSchema(IServiceProvider provider) : base(provider)
        {
            Query = provider.GetRequiredService<HiveQuery>();
        }
    }
}