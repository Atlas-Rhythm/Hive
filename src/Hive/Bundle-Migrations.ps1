
Push-Location "$PSScriptRoot"

try {
    $discard = dotnet tool restore

    $baseIntermediateOutput = dotnet msbuild "$PSScriptRoot" -t:SdkPrintProperty -p:SdkPropertyToGet=BaseIntermediateOutputPath -v:m -nologo;
    $baseIntermediateOutput = $baseIntermediateOutput.Trim();

    dotnet ef migrations bundle -p "$PSScriptRoot" --msbuildprojectextensionspath "$baseIntermediateOutput" $args
} finally {
    Pop-Location
}