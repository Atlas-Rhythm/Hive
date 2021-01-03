# Hive

A general backend project for modding communities.

## Building

### Dependencies

- Visual Studio >= 16.6
  - If not on Visual Studio Preview 2 or later, enable `.NET Core Preview` from `Tools/Options/Environment/Preview Features`

### PostgreSQL

- PostgreSQL >= 12.0
- Add a connection string with the name `Default` to your project secrets of Hive. This should be used for connecting to 
  Hive's specific database.
  - Example: `User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database=postgres;Pooling=true;`
- Add a connection string (without a database) to the name `Test` to your project secrets of Hive. This is used in Hive.Tests for creating test databases.
  - Example: `User ID=postgres;Password=postgres;Host=localhost;Port=5432;`

### Visual Studio Intellisense

Visual Studio may complain about certain things if you start it without first building the code generator.
To make sure intellisense is functional, run `dotnet build` before opening Visual Studio. It may also just
not work if you're not running the latest preview.

### Permission Rules

- Rules are stored (persistently) in the filesystem. The location is configurable through `appsettings.json` (not yet supported).
- The file structure is:

```
| hive.rule
| hive/
|-> mod.rule
|-> mod/
|---> aditionalData.rule
```

### Plugins

- Plugins are loaded in a fashion TBD (most likely filesystem)

## Configuration

Configuration is done from Hive's `appsettings.json` file, located in the root of its directory.

### Rate Limiting

Hive natively comes bundled with a highly configurable rate limit system, powered by the [AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit/) NuGet package.
For a complete configuration breakdown, please look at the documentation for both rate limiting by client, and rate limiting by IP.

Hive uses a combination of both Client and IP rate limiting.
You can configure each rate limit system a number of ways; from per-endpoint buckets, to various whitelists, and even the status code to return.

#### `UseRateLimiting`

This is a global switch for the rate limit system. This is enabled by default.
It is recommended to keep rate limiting on at all times, however the option is available, should you need it.

#### `ClientRateLimiting`

This describes the rate limiting rules that will be enforced per client.
By default, Hive uses the `User-Agent` header to determine clients.
You can whitelist certain clients, as well as certain endpoints from ever being affected by client rate limiting.

#### `ClientRateLimitPolicies`

This describes advanced rules that will apply to specific client IDs.
For example, you could have common web browsers have less restrictive limits.
See the [Client rate limit configuration](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/ClientRateLimitMiddleware#setup) for more information.

#### `IpRateLimiting`

This describes the rate limiting rules that will be enforced per IP.
You can whitelist specific IPs, like your local IP. You can also whitelist certain endpoints from ever being affected by IP rate limits.
This also has support for proxies via the `RealIpHeader` option.

#### `IpRateLimitPolicies`

This describes advanced rules that will apply to specific IPs.
You can apply rules to specific addresses and ranges for both IPv4 and IPv6.
See the [IP rate limit configuration](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup) for more information.