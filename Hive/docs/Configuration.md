# Configuration

Configuration is done from Hive's `appsettings.json` file, located in the root of its directory.

## Permissions Rules

A key component to Hive is its permission system, and the rules that govern it.

### `RuleSubfolder`

This determines the subfolder in the Hive installation folder where permission rules will be read from. By default, this is set to a `Rules` subfolder.

## Rate Limiting

Hive natively comes bundled with a highly configurable rate limit system, powered by [AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit/).
For a complete configuration breakdown, please look at AspNetCoreRateLimit's documentation for rate limiting by client, and rate limiting by IP.

Hive uses both Client and IP rate limiting.
You can configure each rate limit system a number of ways; from per-endpoint buckets, to various whitelists, and even the status code to return.

### `UseRateLimiting`

A global switch for the rate limit system. This is enabled by default.
It is recommended to keep rate limiting on at all times, however the option is available, should you need it.

### `ClientRateLimiting`

This describes the rate limiting rules that will be enforced per client.
By default, Hive uses the `User-Agent` header to determine clients.
You can whitelist certain clients, as well as certain endpoints from ever being affected by client rate limiting.

### `ClientRateLimitPolicies`

This describes advanced rules that will apply to specific client IDs.
For example, you could have common web browsers have less restrictive limits.
See the [Client rate limit configuration](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/ClientRateLimitMiddleware#setup) for more information.

### `IpRateLimiting`

This describes the rate limiting rules that will be enforced per IP.
You can whitelist specific IPs, like your local IP. You can also whitelist certain endpoints from ever being affected by IP rate limits.
This also has support for proxies via the `RealIpHeader` option.

### `IpRateLimitPolicies`

This describes advanced rules that will apply to specific IPs.
You can apply rules to specific addresses and ranges for both IPv4 and IPv6.
See the [IP rate limit configuration](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup) for more information.

## Restricting Non-Authenticated Users

There are certain situations where you need to restrict route access to authenticated users.
While this can be achieved with Hive's permission system, it can be a potentially expensive operation, which may not be desireable.
To solve this, Hive has a simple and highly configurable Middleware that prevents non-authenticated users from accessing certain routes and subroutes.

By default, all routes are *unrestricted*, meaning they can be accessed by non-authenticated users.
To *restrict* them to authenticated users only, you can utilize the below configuration options.

### `RestrictEndpoints`

This is a simple boolean value that toggles this Middleware on and off.
If you do not care about restricting access, then disabling the entire system can slightly increase performance.

### `RestrictedRoutes`

This is a list of all routes that will be restricted to authenticated users.
HTTP Methods/Verbs are not supported and should not be included.
Query parameters are ignored and do not affect how the route is parsed.

There are some additional syntax to be aware of, which can affect how Hive processes each entry.

#### Single Route

If you want to restrict a single route, you can simply write it as is. Any subroutes will not be restricted.
Depending on how many routes you want to restrict, this can get tedious.

```json
[
    "/api",
    "/api/mods",
    "/api/mods/latest"
]
```

#### Cascading Route

If you want to restrict a route, and have it implicitly apply to all subroutes, you can append a forward slash (`/`) as a suffix.
This is called a *cascading* route, as the restricted status of the entry will apply to any subroutes.
However, explicitly defined subroutes will not be affected by a cascading parent route.

```json
[
    "/api/"
]
```

#### Wildcard

You can also use the wildcard token (`*`) if you want a default case that covers routes which aren't explicitly defined.
The wildcard token only applies to the part of a route it was defined in, but can be cascaded to cover subroutes as well.

```json
[
    "/api/channel/*/"
]
```

You can also use the wildcard token to ignore potential route parameters.

```json
[
    "/api/mod/*/move"
]
```

#### Explicit Unrestricting

If you need to explicitly unrestrict a route, you can prefix a route with an exclamation point (`!`).
This can be useful if you have an entry that cascades, or uses a wildcard, and you need to make a subroute available to everyone.

```json
[
    "/api/*/",
    "!/api/mod/"
]
```

This form of explicit unrestricting can be combined with cascading routes and wildcards.

```json
[
    "/api/*/",
    "!/api/mods",
    "!/api/mod/"
]
```

#### Ambiguity

Ambiguity can happen when a route is defined multiple times with differing restriction results.
In this case, Hive will throw an `System.InvalidOperationException`, and tell which route entry has ambiguity.

**Example 1**

The most basic case of ambiguity is when two entries directly contradict each other.

```json
[
    "/api",
    "!/api"
]
```

In this instance, you will have to pick one of the entries to keep, and discard the other.

**Example 2**

Ambiguity can also occur with multiple entries, where one entry cascades, while the other doesn't:

```json
[
    "/api/",
    "!/api"
]
```

In this case, you should consider reforming the cascading entry to utilize a cascading wildcard subroute:

```json
[
    "/api/*/",
    "!/api"
]
```