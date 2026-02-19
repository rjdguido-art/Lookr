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
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o .\dist\portable\publish-temp
```

Portable EXE:
`dist\\portable\\publish-temp\\LookrQuickText.exe`

## Build portable EXE (recommended)
Use the helper script to produce a versioned portable EXE plus SHA256 file:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-portable.ps1 -Version 1.0.0 -Runtime win-x64
```

Script output:
- `dist\\portable\\LookrQuickText-1.0.0-win-x64.exe`
- `dist\\portable\\LookrQuickText-1.0.0-win-x64.exe.sha256`

## GitHub Actions artifact
Tag push (for example `v1.0.1`) triggers the workflow:
`.github/workflows/build-portable-exe.yml`

Artifact uploaded by CI:
- `LookrQuickText-Portable-<version>-win-x64`
- Contains:
  - `LookrQuickText-<version>-win-x64.exe`
  - `LookrQuickText-<version>-win-x64.exe.sha256`

## Notes
- Closing the app window sends it to tray; use tray `Exit` to fully close.
- The app remains fully functional without AI; AI is optional.
- The portable EXE does not require admin privileges. Users can run it directly from any writable folder.
