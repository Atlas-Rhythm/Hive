# Using a `PermissionsManager`

The Hive permissions system is based on a concept of *rule*s and *action*s. Users of the system query
it for an *action*, asking "Do I have permission to perform this action?" The system will then break
up the action and then look up and evaluate the *rule*s that that action is composed of. An *action*
is just a string, while a *rule* is a pair of strings consisting of a name and a definition.

A typical *action* will look like `hive.mod.edit`, or more generally, `<id><sep>...` where `<id>` is an
identifier and `<sep>` is the separator that the PermissionsManager uses. Typically `<sep>` will be `.`,
however something else like `/` may also be used, though any string is technically a valid separator.

A *rule* has a name that looks very similar to an *action*'s, and, in fact, they are very closely related.
When querying an *action*, that *action* is split by the seperator into its constituent identifiers, which
are then recombined into prefixes before each seperator and used to look up the *rule*s to use to evaluate
the *action*. For example, the *action* `hive.mod.edit` will be evaluated using the following *rule*s, in 
this order:
1. `hive`
2. `hive.mod`
3. `hive.mod.edit`

## <a name="writing" /> Writing Rules

The body of a rule is an expression that returns a boolean `true` or `false` that represents that the action
associated with that rule is allowed or not. It is given a single parameter `ctx` that contains an object of
type `TContext` (the type parameter to the PermissionsManager), and all fields, properties, and operators
defined on that type are accessible as long as they are public. It is important to remember that the expressions
are ***not*** C# expressions, but instead a DSL implemented by [MathExpr](https://github.com/nike4613/MathExpr).
It was originally written to compile and evaluate mathematical expressions, and so the operators' spelling is
catered primarily to that. These expressions are entirely statically typed, however all types are implicit,
though you can force a value to a specific type using the [`cast()`](Builtins.md#cast_type_value_) builtin.

For example, most boolean operators are represented using their single character rather than a double (ex. `&`
instead of `&&`, `|` instead of `||`), negation is represented by `~` and can be prepended to a boolean operator
to negate it (ex. `~|` for a NOR, equivalent to `~(a | b)`), and `!` is used as a postfix operator for factorial
on numbers. XOR (`^^`) is a notable exception to the above, having a double character operator, because exponentiation
(`^`) was deemed more common for math. It can be turned into an XNOR by writing `~^`. Equality comparison is also
spelled with only one character, so it looks like `=` instead of `==`.

<a name="next_bool_" />

When executing a rule, the first rule will be executed, and additional rules will only be executed if that rule
contains a call to `next(bool)`. `next(bool)` is a builtin function that is always available that calls the next
rule in the execution chain, and returns its result, or if there is no next rule, its parameter. Its parameter
can be any arbitrary expression, or, specially, the identifiers `true` or `false` if there does not need to be
an expression evaluated for the default value.

With the example of `hive.mod.edit`, if the rule `hive` was defined as `ctx.User.IsSuperAdmin | next(false)`,
when `ctx.User.IsSuperAdmin` is falsey, `hive.mod` would be evaluated if it existed. If it did not exist,
`hive.mod.edit` would be evaluated. If that also did not, since that is the last rule in the chain, `next(bool)`
would return `false`, since that is its parameter.

### <a name="writing_functions" /> Functions

A rule can define its own functions to facilitate code reuse. Such functions take the form 
`name'(arguments, ...) = definition; <expression using name'(args, ...)>`. The definition of the function is
seperated from the expresison that uses it by a semicolon, and follows a declaration that looks like a call with
only variables as arguments followed by an `=`. The single quote is required, as it is what distinguishes a user
function from a builtin function like [`next(bool)`](#next_bool_).

Some applications may also provide a way to define functions avaliable to all rules. They will typically require
the same syntax as described above, just not defined inline by a rule.

Applications may also provide as many builtin functions as they like, and those may be overloaded based on the
argument types. For example, an application may choose to expose a function `contains(List<T>, T)` where T could
be any applicable type, that allows rules to check if a given element exists in the provided list. It may also
choose to provide a function `isOwner(User, Item)` that checks if a given user is the owner of a given item.

## <a name="query" /> Querying Actions

At its most basic level, querying an action looks like this:

```cs
permissionsManager.CanDo("action.name.here", context)
```

`permissionsManager` would typically be injected using DI and be of type `PermissionsManager<TContext>`, and
`context` would be of type `TContext`, and is the object that is given to the rules as `ctx`. `CanDo` returns
a boolean representing whether or not the operation is allowed.

This approach has the drawback of re-parsing the action into a list of rule names, and re-querying for those
rules for each invocation. A slightly better approach would be to do the following:

```cs
private static PermissionActionParseState actionParseState;

// then in the method that querys the action...

permissionsManager.CanDo("action.name.here", context, ref actionParseState)
```

This caches the parsed action and rule information in `actionParseState`, which is stored inline wherever it
is declared. Each parse state **must** correspond to one and only one action, otherwise the wrong action will
be queried, as the manager does not verify that the cache actually matches the action string. Typically, however,
this should not be an issue, as actions should be string literals, and not dynamically generated.