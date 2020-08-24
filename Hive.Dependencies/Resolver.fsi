namespace Hive.Dependencies

open System.Threading.Tasks

/// A static class providing access to dependency resolution functionality.
module Resolver =
    /// <summary>
    /// Resolves the dependencies for a sequence of mods, returning a sequence containing all resolved mods for installation.
    /// </summary>
    /// <param name="accessor">The <see cref="T:Hive.Dependencies.IValueAccessor`4"/> to use.</param>
    /// <param name="mods">The list of mods taken as an input.</param>
    /// <returns>A task to the sequence of mods accessed.</returns>
    /// <exception cref="T:Hive.Dependencies.VersionNotFoundException`1">Thrown when a mod could not be found matching a reference.</exception>
    /// <exception cref="T:Hive.Dependencies.DependencyRangeInvalidException">Thrown when a dependeny and conflict could not be resolved.</exception>
    /// <exception cref="T:System.AggregateException">Thrown when multiple mods could not be found matching their references. Always contains <see cref="T:Hive.Dependencies.VersionNotFoundException`1"/>s.</exception>
    val Resolve : accessor: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange> -> mods: 'Mod seq -> Task<'Mod seq>