# Formatted Resources

We use a custom generator to access resources files. It is mostly the same as the normal generator,
except that it handles format strings sanely. 

Any string with a comment containing braced integers (after the last semicolon in the message, if any)
is considered a formatted resource string. All arguments that must be provided must be listed in the
comment, as they are used to choose the `UnformattedString<>` instantiation to expose. The text immediately
following the braced integer is used as documentation for that argument, and is listed in the generated
doc comments of the property.

Using this scheme, format resource strings can be enforced to be provided with the correct number of arguments.

In the future, there may be support for limiting the argument types as well.

## Usage Notes

In order for the generator to actually operate on a resource file, it must have the item metadata 
`IsResXToGenerate="true"` applied to it. This is so that localizations (of the form `SR.locale.resx`)
don't get included in the generation and have additional classes generated (that don't compile).
Only the main resource file needs to have the item metadata set.

Additionally, the `Name` and `Namespace` item metadata can be set to control the class name and namespace
that the generated class is in.