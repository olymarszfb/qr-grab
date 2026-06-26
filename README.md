# Pinpoint QR Scanner

Pinpoint QR Scanner is a local QR scanning toolkit for the common desktop problem: a QR code is visible somewhere on your screen, but it is awkward to screenshot, crop, upload, and center it in a scanner.

The main app works like a QR-specific snipping tool. Press `Ctrl+Alt+Q`, drag around the QR code, and the result is decoded locally. If the result is a link, the app asks whether you want to open it, copy it, or leave it alone.

![Pinpoint QR Scanner icon](qr-screen-scanner-app/Assets/app-icon-256.png)

## Projects

- `qr-screen-scanner-app`: Windows Forms desktop utility and recommended daily-use app.
- `qr-region-scanner-extension`: Chrome extension companion for scanning QR codes from the visible browser tab.

## Features

- Snipping-style region selection for any visible QR code.
- Local decoding; no upload service required.
- Safe link prompt with Open, Copy, and Not now choices.
- Global Windows hotkey: `Ctrl+Alt+Q`.
- Browser extension shortcut: `Alt+Shift+Q`.
- Matching app and extension icon assets.

## Windows App

```powershell
dotnet build .\qr-screen-scanner-app -c Release
```

Publish a small framework-dependent exe:

```powershell
dotnet publish .\qr-screen-scanner-app -c Release -r win-x64 --self-contained false -o .\qr-screen-scanner-app\release\win-x64-framework
.\qr-screen-scanner-app\release\win-x64-framework\PinpointQrScanner.exe --self-test
```

Publish a larger standalone exe:

```powershell
dotnet publish .\qr-screen-scanner-app -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\qr-screen-scanner-app\release\win-x64-self-contained
.\qr-screen-scanner-app\release\win-x64-self-contained\PinpointQrScanner.exe --self-test
```

## Chrome Extension

```powershell
cd .\qr-region-scanner-extension
npm install
npm run verify
npm run package
```

Load `qr-region-scanner-extension\dist` as an unpacked Chrome extension during development. Upload `qr-region-scanner-extension\release\pinpoint-qr-scanner-extension.zip` as a release asset when publishing.

## Release Guidance

Keep generated binaries and zip files out of git. Use GitHub Releases for:

- `PinpointQrScanner.exe`
- `pinpoint-qr-scanner-extension.zip`

To rebuild all release artifacts locally:

```powershell
.\scripts\package-release.ps1
```

## License

MIT. See `LICENSE` and `THIRD_PARTY_NOTICES.md`.
