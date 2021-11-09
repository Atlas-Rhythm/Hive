using Npgsql.Internal.TypeHandlers;
using Npgsql.Internal.TypeHandling;
using Npgsql.Internal;
using System.Text.Json;
using System;
using Npgsql.PostgresTypes;
using System.Text;
using Npgsql.TypeMapping;

namespace Hive
{
    /// <summary>
    /// A class implementing an Npgsql workaround for using JsonSerializerOptions
    /// </summary>
    public static class NpgsqlJsonOptionsWorkaround
    {
        /// <summary>
        /// Adds the provided <see cref="JsonSerializerOptions"/> to the provided <see cref="INpgsqlTypeMapper"/>.
        /// </summary>
        /// <param name="mapper">The mapper to add to.</param>
        /// <param name="options">The options to add.</param>
        public static void AddJsonbOptions(this INpgsqlTypeMapper mapper, JsonSerializerOptions options)
        {
            if (mapper is null) throw new ArgumentNullException(nameof(mapper));
            mapper.AddTypeResolverFactory(new JsonOverrideTypeHandlerResolverFactory(options));
        }

        // https://github.com/npgsql/efcore.pg/issues/2005#issuecomment-945126345
        internal class JsonOverrideTypeHandlerResolverFactory : TypeHandlerResolverFactory
        {
            private readonly JsonSerializerOptions _options;

            public JsonOverrideTypeHandlerResolverFactory(JsonSerializerOptions options)
                => _options = options;

            public override TypeHandlerResolver Create(NpgsqlConnector connector)
                => new JsonOverrideTypeHandlerResolver(connector, _options);

            public override string? GetDataTypeNameByClrType(Type clrType)
                => null;

            public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
                => null;

            private class RealJsonHandler : JsonHandler
            {
                protected internal RealJsonHandler(PostgresType postgresType, Encoding encoding, bool isJsonb, JsonSerializerOptions? serializerOptions = null)
                    : base(postgresType, encoding, isJsonb, serializerOptions)
                {
                }
            }

            private class JsonOverrideTypeHandlerResolver : TypeHandlerResolver
            {
                private readonly JsonHandler jsonbHandler;

                internal JsonOverrideTypeHandlerResolver(NpgsqlConnector connector, JsonSerializerOptions options)
                    => jsonbHandler ??= new RealJsonHandler(
                        connector.DatabaseInfo.GetPostgresTypeByName("jsonb"),
                        connector.TextEncoding,
                        isJsonb: true,
                        options);

                public override NpgsqlTypeHandler? ResolveByDataTypeName(string typeName)
                    => typeName == "jsonb" ? jsonbHandler : null;

                public override NpgsqlTypeHandler? ResolveByClrType(Type type)
                    => null; // we don't want to *always* map anything to jsonb

                public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
                    => null; // Let the built-in resolver do this
            }
        }

    }
}
