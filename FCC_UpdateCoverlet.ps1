
if ($PSVersionTable.PSVersion -lt [System.Version]::New(5,1,0)) {
	Write-Error ("Powershell version too old! It must be at least 5.1.0 to run this script. Your version is $($PSVersionTable.PSVersion).`n" +
				 "We recommend that you use 'dotnet tool install --global powershell', then use 'pwsh' to run this script.")
	return
}

if ([System.Environment]::OSVersion.Platform -ne [System.PlatformId]::Win32NT) {
	Write-Warning "This script isn't needed on non-Windows platforms! Code coverage should have no issues."
	return
}

# This script is needed to easily update the coverlet that FCC uses to one that is compatible with our projects.

Write-Warning "This script will update the coverlet installation that the Fine Code Coverage extension for Visual Studio uses."
Write-Warning "It requires that the extension be installed. It is entirely unnecessary unless you are using this extension."
$Response = Read-Host "Do you want to continue?"

if ($Response.Length -lt 1 -or $Response[0] -ne "y") {
	Write-Warning "Aborting."
	return
}

Set-Location $PSScriptRoot # we need to be in the solution root, otherwise we won't be able to get the version we want.

$ToolPath = "$($env:LOCALAPPDATA)\FineCodeCoverage\coverlet"
$ToolVersion = "3.0.3-preview.4"

if (Test-Path -Path "$ToolPath\coverlet.exe" -PathType Leaf) {
	# its already been installed, so we'll use update
	dotnet tool update coverlet.console --version $ToolVersion --tool-path "$ToolPath"
} else {
	# otherwise, we need to install it
	dotnet tool install coverlet.console --version $ToolVersion --tool-path "$ToolPath"
}

Write-Output "Coverlet has been sucessfully installed to '$ToolPath'!"