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

dotnet nuget push "$packageDir/*" `
        --api-key $ApiKey `
        --source https://api.nuget.org/v3/index.json `
        --skip-duplicate

Write-Host "Done."
