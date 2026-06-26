$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$app = Join-Path $root "qr-screen-scanner-app"
$extension = Join-Path $root "qr-region-scanner-extension"

Push-Location $app
try {
  dotnet publish -c Release -r win-x64 --self-contained false -o release\win-x64-framework
  & .\release\win-x64-framework\PinpointQrScanner.exe --self-test

  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o release\win-x64-self-contained
  & .\release\win-x64-self-contained\PinpointQrScanner.exe --self-test
}
finally {
  Pop-Location
}

Push-Location $extension
try {
  npm ci
  npm run verify
  npm run package
}
finally {
  Pop-Location
}

Write-Host "Release artifacts are ready:"
Write-Host "  qr-screen-scanner-app\release\win-x64-framework\PinpointQrScanner.exe"
Write-Host "  qr-screen-scanner-app\release\win-x64-self-contained\PinpointQrScanner.exe"
Write-Host "  qr-region-scanner-extension\release\pinpoint-qr-scanner-extension.zip"
