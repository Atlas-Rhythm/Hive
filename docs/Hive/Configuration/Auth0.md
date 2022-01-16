# Auth0 Configuration Options

## Configuration Header

`Auth0`

If this configuration section is not present, Auth0 will be disabled.
Disabling can increase performance, but another user-based plugin MUST be installed for Hive to behave as intended.

## Sample configuration

```json
"Auth0": {
    "Domain": "Auth0 domain",
    "Audience": "Auth0 audience domain",
    "ClientID": "Auth0 client id",
    "ClientSecret": "Auth0 client secret",
    "TimeoutMS": 30000,
    "BaseDomain": "Domain to redirect to. Should be YOUR hosting domain"
}
```

## `Domain` - `Uri`

The Auth0 domain to use.
This configuration option is required.

## `Audience` - `string`

The Auth0 audience to use.
This configuration option is required.

## `ClientID` - `string`

The Auth0 client ID to use.
This configuration option is required.

## `ClientSecret` - `string`

The Auth0 client secret to use.
This configuration option is required.

## `TimeoutMS` - `int`

The timeout for Auth0 callback requests in milliseconds. Must be positive or 0. If not present, will default to `10000`.
