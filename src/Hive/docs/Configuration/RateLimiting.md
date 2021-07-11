# Rate Limiting

Hive natively comes bundled with a highly configurable rate limit system, powered by [AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit/).
For a complete configuration breakdown, please look at AspNetCoreRateLimit's documentation for rate limiting by client, and rate limiting by IP.

Hive uses both Client and IP rate limiting.
You can configure each rate limit system a number of ways; from per-endpoint buckets, to various whitelists, and even the status code to return.

## `UseRateLimiting` - bool

A global switch for the rate limit system. This is enabled by default.
It is recommended to keep rate limiting on at all times, however the option is available, should you need it.

## `ClientRateLimiting` - object

This describes the rate limiting rules that will be enforced per client.
By default, Hive uses the `User-Agent` header to determine clients.
You can whitelist certain clients, as well as certain endpoints from ever being affected by client rate limiting.

## `ClientRateLimitPolicies` - object

This describes advanced rules that will apply to specific client IDs.
For example, you could have common web browsers have less restrictive limits.
See the [Client rate limit configuration](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/ClientRateLimitMiddleware#setup) for more information.

## `IpRateLimiting` - object

This describes the rate limiting rules that will be enforced per IP.
You can whitelist specific IPs, like your local IP. You can also whitelist certain endpoints from ever being affected by IP rate limits.
This also has support for proxies via the `RealIpHeader` option.

## `IpRateLimitPolicies` - object

This describes advanced rules that will apply to specific IPs.
You can apply rules to specific addresses and ranges for both IPv4 and IPv6.
See the [IP rate limit configuration](https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup) for more information.