# Lookr QuickText

Lookr QuickText is a native Windows desktop app (WPF), not a browser app.
It works offline and is designed for non-technical users who need fast reusable quicktexts.

## Key features
- Offline-first quicktext library.
- Floating always-on-top widget for fast access.
- Global shortcut: `Ctrl + Shift + Space` to show/hide widget.
- Rich search across title, content, category, and keywords.
- Category filtering in both the main app and widget.
- Excel template import (`.xlsx`, `.xlsm`) for bulk quicktext creation.
- Optional local AI generation using `llama.cpp` + GGUF model.
- System tray support.

## Security and privacy
- Snippets are encrypted with Windows DPAPI (`CurrentUser`).
- Snippet file path: `%LOCALAPPDATA%\\LookrQuickText\\snippets.bin`.
- App settings file path: `%LOCALAPPDATA%\\LookrQuickText\\settings.json`.
- No cloud sync, no telemetry, no external API calls.

## Excel template format
Import works from the first worksheet. Header names are flexible.

Supported columns:
- Title: `Title`, `Name`, `Snippet`, `Subject`
- Content: `Content`, `Text`, `Body`, `QuickText`, `Message`, `Template`
- Category: `Category`, `Group`, `Folder`, `Section`
- Keywords: `Keywords`, `Keyword`, `Tags`, `Tag`

At least one of Title or Content must exist.

A starter template is included at:
`templates/quicktexts-template.csv`

You can open this CSV in Excel and save as `.xlsx` before importing.

## Local AI setup (optional)
1. Download `llama.cpp` for Windows and use `llama-cli.exe`.
2. Download a GGUF instruction model.
3. In the app, set:
   - AI executable path (`llama-cli.exe`)
   - GGUF model path
4. Enter a prompt and click `Generate`.

Recommended lightweight local models:
- `Phi-3-mini` (Q4 GGUF)
- `Qwen2.5-3B-Instruct` (Q4 GGUF)

## Build and run (Windows)
1. Install .NET 8 SDK.
2. Open `src/LookrQuickText/LookrQuickText.csproj`.
3. Build and run in Debug or Release.

## Publish a portable build
```powershell
dotnet publish .\src\LookrQuickText\LookrQuickText.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\installer\publish
```

Publish output:
`installer\\publish`

## Build installer (easy end-user install)
Installer uses Inno Setup and installs per-user (no admin required).

1. Install [Inno Setup](https://jrsoftware.org/isinfo.php) (which provides `iscc.exe`).
2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -Version 1.0.0
```

Installer output:
`installer\\dist\\LookrQuickText-Setup-<version>.exe`

## Notes
- Closing the app window sends it to tray; use tray `Exit` to fully close.
- The app remains fully functional without AI; AI is optional.
- Keep the full publish folder together when sharing builds; launching only `LookrQuickText.exe` without adjacent files will fail.
