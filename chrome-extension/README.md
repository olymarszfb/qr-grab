# QR Grab Extension

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

## When the Icon Does Nothing

Chrome does not allow extensions to inject UI into protected pages such as `chrome://` pages, the Chrome Web Store, extension pages, and some browser-managed documents. QR Grab shows a temporary `!` badge on the extension icon when Chrome blocks the current tab.

If you are using the unpacked development build and just reloaded or updated the extension, reload the target tab once. The Windows app does not have this browser-page limitation and is the best fallback for scanning anything visible on screen.

## Package

```powershell
npm run package
```

The zip is written to `release\qr-grab-extension.zip`.
