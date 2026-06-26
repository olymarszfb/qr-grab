# Pinpoint QR Scanner for Windows

![Pinpoint QR Scanner icon](Assets/app-icon-256.png)

Local Windows Forms QR scanner for selecting an on-screen region and decoding QR codes with ZXing.Net.

Use it like a QR-specific Snipping Tool: press `Ctrl+Alt+Q`, drag around a QR code, then open, copy, or dismiss the decoded result. Link results are shown in a confirmation prompt before opening.

## Run

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o release\win-x64-framework
.\release\win-x64-framework\PinpointQrScanner.exe
```

Click **Scan Region** or press `Ctrl+Alt+Q`, drag around a QR code, then copy or open the decoded result. Closing or minimizing the window keeps it running in the system tray, where the hotkey continues to work.

## Build

```powershell
dotnet build -c Release
```

Use `dotnet publish` for runnable release outputs.

## Published Exes

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o release\win-x64-framework
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o release\win-x64-self-contained
```

- `release\win-x64-framework\PinpointQrScanner.exe` is small and uses the installed .NET Desktop Runtime.
- `release\win-x64-self-contained\PinpointQrScanner.exe` is a larger standalone exe for Windows x64.

## Self-Test

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o release\win-x64-framework
.\release\win-x64-framework\PinpointQrScanner.exe --self-test
```

The self-test generates a QR code in memory and verifies that the app decoder reads it back. It is run against the published exe because this is a Windows GUI app with an embedded application icon.
