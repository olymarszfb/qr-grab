$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$dist = Join-Path $root "dist"
$release = Join-Path $root "release"
$zip = Join-Path $release "qr-grab-extension.zip"

if (-not (Test-Path $dist)) {
  throw "dist folder is missing. Run npm run build first."
}

New-Item -ItemType Directory -Force -Path $release | Out-Null
if (Test-Path $zip) {
  Remove-Item -Force $zip
}

Compress-Archive -Path (Join-Path $dist "*") -DestinationPath $zip -Force
Write-Host "Created $zip"
