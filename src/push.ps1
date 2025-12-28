param (
    [Parameter(Mandatory = $true)]
    [string]$ApiKey
)

$ErrorActionPreference = "Stop"

$packageDir = Join-Path $PSScriptRoot "nupkgs"

if (-not (Test-Path $packageDir)) {
    Write-Error "Directory not found: $packageDir"
    exit 1
}

$packages = Get-ChildItem -Path $packageDir -Filter "*.nupkg" -File

if ($packages.Count -eq 0) {
    Write-Host "No .nupkg files found in $packageDir"
    exit 0
}

foreach ($pkg in $packages) {
    Write-Host "Pushing $($pkg.Name)..."

    dotnet nuget push $pkg.FullName `
        --api-key $ApiKey `
        --source https://api.nuget.org/v3/index.json `
        --skip-duplicate
}

Write-Host "Done."