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

The permission system is a key component to Hive. Hive uses highly configurable rules to determine access for various components in Hive.
These rules are persistently stored in the file system. The permission system itself can be configured through `appsettings.json`, documented below.

[Check out the dedicated documentation page for further information.](https://github.com/Atlas-Rhythm/Hive/blob/master/Hive.Permissions/docs/Usage.md).

#### File Structure

Below is an example of the file structure for Hive's permission rules. Notice how subfolders are determined by the separator token.
For example, the file location for the rule `hive.mod.edit` will be `<Rules folder>/hive/mod/edit.rule`.
Higher level rules, such as `hive` and `hive.mod`, will also have their own definitions in the file system.

```
| hive.rule
| hive/
|-> mod.rule
|-> mod/
|---> edit.rule
|---> additionalData.rule
```

#### Disclaimer

Hive does *not* automatically generate rule files if they don't exist.
Rules with no defined definition in the file system are treated as if they do not exist, and are skipped.

### Plugins

- Plugins are loaded in a fashion TBD (most likely filesystem)

## Configuration

Configuration is done from Hive's `appsettings.json` file, located in the root of its directory.

### Permissions Rules

A key component to Hive is its permission system, and the rules that govern it.

#### `RuleSubfolder`

This determines the subfolder in the Hive installation folder where permission rules will be read from. By default, this is set to a `Rules` subfolder.

### Rate Limiting

Hive natively comes bundled with a highly configurable rate limit system, powered by [AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit/).
For a complete configuration breakdown, please look at AspNetCoreRateLimit's documentation for rate limiting by client, and rate limiting by IP.

Hive uses both Client and IP rate limiting.
You can configure each rate limit system a number of ways; from per-endpoint buckets, to various whitelists, and even the status code to return.

#### `UseRateLimiting`

A global switch for the rate limit system. This is enabled by default.
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

### Restricting Non-Authenticated Users

There are certain situations where you need to restrict route access to authenticated users.
While this can be achieved with Hive's permission system, it is an expensive operation which may not be desireable, especially with potential Denial of Service attacks.
To solve this, Hive has a simple Middleware that prevents all non-authenticated users from accessing a given list of routes. This does not use the permission system, so it is faster in cases where you only need to deny access to non-authenticated users.

#### `RestrictEndpoints`

This is a simple boolean value that toggles this Middleware on and off.
If you do not care about restricting access, then disabling the entire system can slightly increase performance.

#### `RestrictedRoutes`

This is a list of all routes that will be restricted to authenticated users.
Any subroutes will also be restricted. For example, restricting `/api/mod/` will also restrict `/api/mod/move` and `/api/mod/edit`.
