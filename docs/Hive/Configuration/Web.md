# Web Configuration Options

## Configuration Header

`Web`

If this configuration section is not present, CORS will be disabled and HTTPS Redirection will be enabled.

## Sample configuration

```json
"Web": {
    "CORS": true,
    "AllowedOrigins": ["https://example.com", "https://localhost:3000"],
    "AllowedMethods": ["GET", "POST", "PUT"],
    "AllowedHeaders": ["Content-Type", "x-requested-with"],
    "PolicyName": "_hiveOrigins",
    "HTTPSRedirection": true
}
```

## `CORS` - `boolean`

Whether or not to enable CORS. If disabled, the Allowed\* and PolicyName fields are ignored. If not present, will default to `false`.

## `AllowedOrigins` - `string[]`

The origins to allow through CORS. If CORS is enabled, this is required. You cannot allow all origins, as then you wouldn't be able to use the authorization headers in Hive.

## `AllowedMethods` - `string[]`

The methods to allow through CORS. If not present or if it has no items, it will allow any method.

## `AllowedHeaders` - `string[]`

The headers to allow through CORS. If not present or if it has no items, it will allow any header.

## `PolicyName` - `string`

The CORS policy name. If not present, will default to `_hiveOrigins`

## `HTTPSRedirection` - `boolean`

Whether or not to enable HTTPS Redirection. If not present, will default to `true`.
