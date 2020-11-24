namespace Hive.Dependencies

open System.Threading.Tasks

/// <summary>
/// An interface providing access to the generic types given to the dependency resolver.
/// </summary>
/// <typeparamref name="Mod">The type that represents a single mod version.</typeparamref>
/// <typeparamref name="ModRef">The type that represents a reference to a version matching a version range.</typeparamref>
/// <typeparamref name="Version">The type that represents a version.</typeparamref>
/// <typeparamref name="VerRange">The type that represents a version range.</typeparamref>
type IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange> =
    interface
        // Accessors for Mod
        abstract member ID : mod_:'Mod -> string;
        abstract member Version : mod_:'Mod -> 'Version;
        abstract member Dependencies : mod_:'Mod -> 'ModRef seq;
        abstract member Conflicts : mod_:'Mod -> 'ModRef seq;

        // Accessors for ModRef
        abstract member ID : ref:'ModRef -> string;
        abstract member Range : ref:'ModRef -> 'VerRange;
        abstract member CreateRef : id:string -> range:'VerRange -> 'ModRef
        abstract member IsValidVersionRange : ref:'VerRange -> bool;

        // Comparisons
        abstract member Matches : range:'VerRange -> version:'Version -> bool;
        abstract member Compare : a:'Version -> b:'Version -> int;

        // Combiners
        abstract member Either : a:'VerRange -> b:'VerRange -> 'VerRange
        abstract member And : a:'VerRange -> b:'VerRange -> 'VerRange voption
        abstract member Not : a:'VerRange -> 'VerRange

        // External
        abstract member ModsMatching : ref:'ModRef -> Task<'Mod seq>
    end
