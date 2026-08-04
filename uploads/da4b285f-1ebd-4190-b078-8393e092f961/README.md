# Epic Camera Scanner — POC

Windows tray utility that turns a Windows camera into keyboard-wedge barcode input for Epic.

## Default workflow

1. Start `EpicCameraScanner.exe`; it remains in the Windows system tray.
2. Place Epic in the state where it expects scanner input.
3. Press **Ctrl+Alt+S**.
4. The selected camera opens with a preview and 15-second countdown.
5. The first supported barcode is decoded.
6. A green confirmation overlay and sound indicate success.
7. Focus returns to the window that was active before scanning.
8. The utility sends `\SCANNED_DATA\` as keyboard input.

The default configuration does not append Enter or Tab.

## POC features

- Global Ctrl+Alt+S scan hotkey
- Windows system-tray operation
- Optional launch at user sign-in
- Configurable prefix and suffix
- Optional Enter or Tab after the scan
- Camera selection by device index
- Configurable scan timeout
- Green success overlay
- Optional audible confirmation
- Code 39, Code 128, QR, Data Matrix, PDF417, Aztec, UPC and EAN
- Configurable symbology list
- Auto-rotation, enhanced decoding and inverted-code detection
- Optional diagnostic logging that does **not** record barcode values
- Duplicate-instance protection
- JSON configuration for repeatable deployment

## Settings

Right-click the tray icon and select **Settings**. Configuration is stored at:

`%LOCALAPPDATA%\EpicCameraScanner\settings.json`

The tray menu also provides:

- Scan now
- Settings
- Start with Windows
- Open settings folder
- Exit

Camera index `0` is normally the default/built-in camera. Try `1` or `2` for another attached camera.

## Build the EXE

On Windows 10/11 x64:

1. Extract the package.
2. Right-click `Build-EXE.ps1` and choose **Run with PowerShell**.
3. The script installs the .NET 8 SDK for the current user if it is missing.
4. Find the completed application at `Published\EpicCameraScanner.exe`.

Manual command:

```powershell
dotnet restore .\EpicCameraScanner.csproj -r win-x64
dotnet publish .\EpicCameraScanner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\Published
```

## Recommended POC validation

Use a non-production Epic environment and compare against a supported physical scanner.

1. Confirm Ctrl+Alt+S is not intercepted by Epic, Citrix, VMware or another utility.
2. Confirm a known barcode arrives as exactly `\VALUE\`.
3. Confirm no Enter or Tab is added under the default settings.
4. Test every barcode symbology required by the workflow.
5. Test damaged, curved, glossy and low-contrast labels.
6. Test the correct camera index and Windows camera privacy policy.
7. Test focus restoration after cancelling and after a successful scan.
8. Test inside the virtual session if Epic is delivered through Citrix/VDI.
9. Confirm endpoint security permits the unsigned POC executable.
10. Code-sign and formally validate before clinical or production use.

## Security and privacy

- Camera frames are processed in memory and are not saved.
- Diagnostic logging is disabled by default.
- When enabled, logs contain operational events and barcode character counts, but not barcode values.
- Windows can block input injection into an application running at a higher integrity level. Run the scanner at the same integrity level as Epic.
