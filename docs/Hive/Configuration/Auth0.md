# Auth0 Configuration Options

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

## `Audience` - `string`

The Auth0 audience to use.

## `ClientID` - `string`

The Auth0 client ID to use.

## `ClientSecret` - `string`

The Auth0 client secret to use.

## `TimeoutMS` - `int`

The timeout for Auth0 callback requests in milliseconds. If not present, will default to `10000`.

## `BaseDomain` - `Uri`

The callback URI to use for determining callback resolution. Should be the forward-facing URI of the instance.
