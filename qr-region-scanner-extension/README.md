# Pinpoint QR Scanner Extension

Click the extension icon or press `Alt+Shift+Q`, drag over a visible QR code, and decode it locally from the selected screen region.

This is the browser companion to the Windows app. The Windows app is the smoother choice for scanning across any desktop app, while the extension is useful when you want a browser-only workflow.

## Build

```powershell
npm install
npm run build
```

Load `dist` as an unpacked extension in Chrome:

1. Open `chrome://extensions`.
2. Enable Developer mode.
3. Choose **Load unpacked**.
4. Select this project's `dist` folder.

## Verify

```powershell
npm run verify
```

This rebuilds the extension and verifies the bundled QR decoder against a generated QR fixture.

## Package

```powershell
npm run package
```

The zip is written to `release\pinpoint-qr-scanner-extension.zip`.
