Param (
	[Parameter(Mandatory=$false)]
	[string]$Configuration = 'Release'
)

./build_bullet_sharp.ps1 -Configuration $Configuration

Push-Location -Path './src'

dotnet build -c $Configuration

Pop-Location

$targets = 'netstandard2.1', 'net8.0', 'net9.0'
foreach ($target in $targets) {
	if ($Configuration -eq 'Debug')
	{
		Copy-Item "./libs/BulletSharpPInvoke/libbulletc/build/lib/$Configuration/libbulletc_Debug.dll" "./src/FancadeLoaderLib.Runtime.Bullet/bin/$Configuration/$target/libbulletc.dll"
	}
	else
	{
		Copy-Item "./libs/BulletSharpPInvoke/libbulletc/build/lib/$Configuration/libbulletc.dll" -Destination "./src/FancadeLoaderLib.Runtime.Bullet/bin/$Configuration/$target"
	}
}