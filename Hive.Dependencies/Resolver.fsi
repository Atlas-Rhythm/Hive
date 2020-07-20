namespace Hive.Dependencies

open System.Threading.Tasks

module Resolver =
    /// <summary>
    /// Resolves the dependencies for a sequence of mods, returning a sequence containing all resolved mods for installation.
    /// </summary>
    /// <param name="accessor">The <see cref="IValueAccessor{Mod,ModRef,Version,VerRange}"/> to use.</param>
    /// <param name="mods">The list of mods taken as an input.</param>
    /// <returns>A task to the sequence of mods accessed.</returns>
    val Resolve : accessor: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange> -> mods: 'Mod seq -> Task<'Mod seq>