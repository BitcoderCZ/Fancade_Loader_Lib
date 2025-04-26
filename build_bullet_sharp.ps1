Param (
	[Parameter(Mandatory=$false)]
	[string]$Configuration = 'Release'
)

Push-Location -Path './libs/BulletSharpPInvoke/libbulletc'

md -Force 'build'
Push-Location -Path './build'

cmake -DCMAKE_BUILD_TYPE=$Configuration ..

cmake --build . --config $Configuration

Pop-Location
Pop-Location
Push-Location -Path './libs/BulletSharpPInvoke/BulletSharp'

dotnet build BulletSharp.csproj -c $Configuration

Pop-Location