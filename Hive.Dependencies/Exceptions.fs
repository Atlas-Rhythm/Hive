namespace Hive.Dependencies

type VersionNotFoundException<'ModRef>(ref: 'ModRef) =
    inherit exn("A mod could not be found matching reference")
    member public _.ModReference = ref
    override _.Message = base.Message + " " + ref.ToString()

type DependencyRangeInvalidException<'VerRange>(id: string, range: 'VerRange) =
    inherit exn("Could not resolve valid version range for mod")
    member public _.ID = id
    member public _.Range = range
    override _.Message = "Mod " + id + " has no version matching range " + range.ToString()