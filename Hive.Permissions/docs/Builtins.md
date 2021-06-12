# Builtins

What follows is a list of builtins always provided by the permissions system, along with a description of their behaviour.

- [`next(bool)`](Usage.md#next_bool_) - This is described in [Usage](Usage.md#next_bool_).
- [`cast(Type, value)`](#cast_type_value_) - Casts a value to the specified type, and returns the casted value.

## <a name="cast_type_value_" /> `cast(Type, value)`

Casts the value given by the expression in the second argument to the type described in the first. The type
may be described in one of 3 ways:

1. It can be one of the type keywords (`object`, `string`, `int`, etc.),
2. If it is a core lib type (defined in System.Private.CoreLib, or the equivalent) and not generic, it may
   be specified as the type name with no special adornments (`System.String`, etc.)
3. Otherwise, it must be specified with a string that would resolve the type when passed to
   [`Type.GetType(string)`](https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype?view=net-5.0#System_Type_GetType_System_String_).

For example, if you wanted to cast a value to a `double`, you could write `cast(double, value)`, and if you
wanted to cast a value to `System.Type`, you would write `cast(System.Type, value)`, but if you wanted to cast
to some custom type, like `HivePlugin.PermissionContext` (in assembly `HivePlugin`), you would have to write
`cast("HivePlugin.PermissionContext, HivePlugin", value)`.