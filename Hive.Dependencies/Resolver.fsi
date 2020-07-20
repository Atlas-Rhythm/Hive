namespace Hive.Dependencies

open System.Threading.Tasks

module Resolver =
    val Resolve : accessor: IValueAccessor<'Mod, 'ModRef, 'Version, 'VerRange> -> mods: 'Mod seq -> Task<'Mod seq>