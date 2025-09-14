Param (
	[Parameter(Mandatory=$false)]
	[ValidateSet("Debug", "Release")]
	[string]$Configuration = 'Release'
)

Push-Location -Path './libs/BulletSharpPInvoke'

./build.ps1 -Configuration $Configuration

Pop-Location

Push-Location -Path './src'

dotnet build -c $Configuration

Pop-Location

$targets = 'netstandard2.1', 'net8.0', 'net9.0'
foreach ($target in $targets) {
	Copy-Item "./libs/BulletSharpPInvoke/BulletSharp/bin/$Configuration/$target/libbulletc.dll" -Destination "./src/FancadeLoaderLib.Runtime.Bullet/bin/$Configuration/$target"
}