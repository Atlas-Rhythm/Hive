
Push-Location "$PSScriptRoot"

try {
    $discard = dotnet tool restore

    $baseIntermediateOutput = dotnet msbuild "$PSScriptRoot" -t:SdkPrintProperty -p:SdkPropertyToGet=BaseIntermediateOutputPath -v:m -nologo;
    $baseIntermediateOutput = $baseIntermediateOutput.Trim();

    dotnet ef migrations add -p "$PSScriptRoot" --msbuildprojectextensionspath "$baseIntermediateOutput" $args
} finally {
    Pop-Location
}