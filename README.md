# QR Grab

QR Grab is a local QR scanning toolkit for the common desktop problem: a QR code is visible somewhere on your screen, but it is awkward to screenshot, crop, upload, and center it in a scanner.

The Windows app works like a QR-specific snipping tool. Press `Ctrl+Alt+Q`, drag around the QR code, and the result is decoded locally. If the result is a link, QR Grab asks whether you want to open it, copy it, or leave it alone.

![QR Grab icon](windows-app/Assets/app-icon-256.png)

## Projects

- `windows-app`: Windows Forms desktop utility and recommended daily-use app.
- `chrome-extension`: Chrome extension companion for scanning QR codes from the visible browser tab.

## Features

- Snipping-style region selection for any visible QR code.
- Local decoding; no upload service required.
- Safe link prompt with Open, Copy, and Not now choices.
- Global Windows hotkey: `Ctrl+Alt+Q`.
- Browser extension shortcut: `Alt+Shift+Q`.
- Matching app and extension icon assets.

## Windows App

```powershell
dotnet build .\windows-app -c Release
```

Publish a small framework-dependent exe:

```powershell
dotnet publish .\windows-app -c Release -r win-x64 --self-contained false -o .\windows-app\release\win-x64-framework
.\windows-app\release\win-x64-framework\QRGrab.exe --self-test
```

Publish a larger standalone exe:

```powershell
dotnet publish .\windows-app -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\windows-app\release\win-x64-self-contained
.\windows-app\release\win-x64-self-contained\QRGrab.exe --self-test
```

## Chrome Extension

```powershell
cd .\chrome-extension
npm install
npm run verify
npm run package
```

Load `chrome-extension\dist` as an unpacked Chrome extension during development. Upload `chrome-extension\release\qr-grab-extension.zip` as a release asset when publishing.

## Release Guidance

Keep generated binaries and zip files out of git. Use GitHub Releases for:

- `QRGrab-win-x64-framework.zip`
- `QRGrab.exe` standalone self-contained build
- `qr-grab-extension.zip`

To rebuild all release artifacts locally:

```powershell
.\scripts\package-release.ps1
```

## License

MIT. See `LICENSE` and `THIRD_PARTY_NOTICES.md`.
