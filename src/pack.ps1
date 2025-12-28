Remove-Item "./nupkgs/*"
dotnet clean
dotnet build -c Release
dotnet pack -c Release -o nupkgs/