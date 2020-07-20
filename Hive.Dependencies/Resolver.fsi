namespace Hive.Dependencies

open System.Threading.Tasks

module Resolver =
    /// <summary>
    /// Resolves the dependencies for a sequence of mods, returning a sequence containing all resolved mods for installation.
    /// </summary>
    /// <param name="accessor">The <see cref="IValueAccessor{Mod,ModRef,Version,VerRange}"/> to use.</param>
    /// <param name="mods">The list of mods taken as an input.</param>
    /// <returns>A task to the sequence of mods accessed.</returns>
    /// <exception cref="VersionNotFoundException{ModRef}">Thrown when a mod could not be found matching a reference.</exception>
    /// <exception cref="DependencyRangeInvalidException">Thrown when a dependeny and conflict could not be resolved.</exception>
    /// <exception cref="AggregateException">Thrown when multiple mods could not be found matching their references. Always contains <see cref="VersionNotFoundException{ModRef}"/>s.</exception>
    val Resolve : accessor: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange> -> mods: 'Mod seq -> Task<'Mod seq>