Param (
	[Parameter(Mandatory=$false)]
	[ValidateSet("Debug", "Release")]
	[string]$Configuration = 'Release'
)

Remove-Item "./nupkgs/*"
dotnet clean
dotnet build -c $Configuration -p:WarningLevel=0
dotnet pack -c $Configuration -o nupkgs/ -p:WarningLevel=0
