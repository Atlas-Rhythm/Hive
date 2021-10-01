
# Make sure we're executing in the root of the Hive source tree
Set-Location $PSScriptRoot

dotnet pack @args

$StandaloneBuildable = "Hive.Plugins","Hive.Versioning"
foreach ($it in $StandaloneBuildable) {
    Set-Location "src/$it/"
    dotnet pack "-p:BuildStandalone=$it" @args
    Set-Location $PSScriptRoot
}