namespace Hive.Dependencies

type VersionNotFoundException<'ModRef>(ref: 'ModRef) =
    inherit exn("A mod could not be found matching reference")
    member public _.ModReference = ref
    override _.Message = base.Message + " " + ref.ToString()

type DependencyRangeInvalidException(id: string) =
    inherit exn("Could not resolve valid version range for mod")
    member public _.ID = id
    override _.Message = base.Message + " " + id