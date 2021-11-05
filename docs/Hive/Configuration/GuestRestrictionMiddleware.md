# Restricting Non-Authenticated Users

There are certain situations where you need to restrict route access to authenticated users.
While this can be achieved with Hive's permission system, it can be a potentially expensive operation, which may not be desireable.
To solve this, Hive has a simple and highly configurable Middleware that prevents non-authenticated users from accessing certain routes and subroutes.

By default, all routes are *unrestricted*, meaning they can be accessed by non-authenticated users.
To *restrict* them to authenticated users only, you can utilize the below configuration options.

## `RestrictEndpoints` - `bool`

This is a simple boolean value that toggles this Middleware on and off.
If you do not care about restricting access, then disabling the entire system can slightly increase performance.

## `RestrictedRoutes` - `string[]`

This is a list of all routes that will be restricted to authenticated users.
HTTP Methods/Verbs are not supported and should not be included.
Query parameters are ignored and do not affect how the route is parsed.

There are some additional syntax to be aware of, which can affect how Hive processes each entry.

### Single Route

If you want to restrict a single route, you can simply write it as is. Any subroutes will not be restricted.
Depending on how many routes you want to restrict, this can get tedious.

```json
[
    "/api",
    "/api/mods",
    "/api/mods/latest"
]
```

### Cascading Route

If you want to restrict a route, and have it implicitly apply to all subroutes, you can append a forward slash (`/`) as a suffix. This is called a *cascading* route.
However, any explicitly defined routes will have higher priority than cascading routes.

```json
[
    "/api/"
]
```

### Wildcard

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

You may also have a single wildcard token entry to restrict every available route in Hive.

```json
[
    "*"
]
```

### Explicit Unrestricting

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

### Ambiguity

Ambiguity can happen when a route is defined multiple times with differing restriction results.
In this case, Hive will throw an `System.InvalidOperationException`, and tell which route entry has ambiguity.

#### Example 1

The most basic case of ambiguity is when two entries directly contradict each other.

```json
[
    "/api",
    "!/api"
]
```

In this instance, you will have to pick one of the entries to keep, and discard the other.

#### Example 2

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
