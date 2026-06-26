$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$app = Join-Path $root "windows-app"
$extension = Join-Path $root "chrome-extension"
$frameworkZip = Join-Path (Join-Path $app "release") "QRGrab-win-x64-framework.zip"

Push-Location $app
try {
  dotnet publish -c Release -r win-x64 --self-contained false -o release\win-x64-framework
  & .\release\win-x64-framework\QRGrab.exe --self-test

  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o release\win-x64-self-contained
  & .\release\win-x64-self-contained\QRGrab.exe --self-test

  if (Test-Path $frameworkZip) {
    Remove-Item -Force $frameworkZip
  }

  Compress-Archive -Path .\release\win-x64-framework\* -DestinationPath $frameworkZip -Force
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
Write-Host "  windows-app\release\QRGrab-win-x64-framework.zip"
Write-Host "  windows-app\release\win-x64-framework\QRGrab.exe"
Write-Host "  windows-app\release\win-x64-self-contained\QRGrab.exe"
Write-Host "  chrome-extension\release\qr-grab-extension.zip"
