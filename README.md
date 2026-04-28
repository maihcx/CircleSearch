# CircleSearch — Circle to Search for Windows

> Draw a circle around anything on your screen to search, OCR, translate, or reverse-image-search it — inspired by Android's Circle to Search feature.

---

## Solution Structure

```
CircleSearch.sln
├── CircleSearch/              ← Settings UI (WPF-UI + Fluent Design, .NET 10)
├── CircleSearch.Core/         ← Background service: hotkey listener + process orchestrator
├── CircleSearch.Tray/         ← System tray app (WPF-UI, persistent in taskbar)
├── CircleSearch.Overlay/      ← Fullscreen drawing overlay + action popup (pure WPF)
└── CircleSearch.CFS/          ← Shared IPC library (ConfluxService — named-pipe messaging)
```

### How the processes connect

```
CircleSearch (Settings UI)
    │  ConfluxService (MainToCore / CoreToMain)
    ▼
CircleSearch.Core  ◄───────────────────────────────┐
    │  ConfluxService (CoreToTray / TrayToCore)    │
    ▼                                              │
CircleSearch.Tray  ────────────────────────────────┘
    (system tray, always running)

CircleSearch.Core
    │  spawns on hotkey press
    ▼
CircleSearch.Overlay  (short-lived, exits after one capture)
```

`CircleSearch.Core` is the hub. It owns the global hotkey registration, launches/monitors the other processes, and routes messages between them via `ConfluxService`.

---

## Architecture Overview

### ConfluxService (CFS)

A lightweight named-pipe IPC library shared by all projects. Each channel is a pair of directional pipes carrying `name|value` messages. Processes call `Register()` then `StartServiceAsync()` to begin listening, and `Send(name, value)` to dispatch.

### CircleSearch.Core

- Runs as a background console process (no window).
- Registers the global hotkey via `GlobalHotkeyService` — a pure Win32 P/Invoke loop on a dedicated thread (no Windows.Forms dependency).
- On hotkey press, spawns `CircleSearch.Overlay.exe` with the current overlay config as CLI args.
- Bridges messages between `CircleSearch` (settings UI) and `CircleSearch.Tray` so neither needs to talk to each other directly.
- Handles `hotkey-change` and `overlaycfg-change` messages to update hotkey registration or forward new config to the next overlay launch.

### CircleSearch (Settings UI)

- WPF-UI / Fluent Design settings window, built on .NET Generic Host with DI.
- Single-instance enforced via a named `Mutex` + named pipe: a second launch sends `"SHOW"` to the running instance and exits.
- Communicates settings changes to Core over CFS (`hotkey-change`, `overlaycfg-change`).
- Supports a **View At Boot** mode: if disabled, the process exits immediately after sending config to Core, leaving Core and Tray running silently.

### CircleSearch.Tray

- Persistent system-tray icon built with WPF-UI's `TrayIcon`.
- Connects to Core over CFS; relays tray menu actions (Open, Home, Settings, Exit) as `tray-event` messages.
- Reacts to `main-event` notifications from Core to update language, theme, material, and corner radius at runtime without restarting.

### CircleSearch.Overlay

- Launched fresh for each capture session; exits automatically when the action popup closes.
- Receives its entire config (search engine, OCR language, opacity, accent color) as CLI arguments parsed by `OverlayConfig.ParseArgs()`.
- `OverlayWindow` covers all monitors (`VirtualScreen*` bounds), captures a full screenshot on load via `BitBlt` WinAPI, and renders a smooth freehand stroke using Bézier geometry (`CircleHelper.CreateSmoothPath`).
- On mouse-up, crops the bounding region of the drawn stroke (DPI-aware) and opens `ActionPopup`.
- `ActionPopup` runs Tesseract OCR asynchronously via the `Tesseract` NuGet package (`TesseractEngine` → `PixConverter.ToPix` → `page.GetText()`).

---

## Prerequisites

### 1. .NET 10 SDK

Download from: https://dotnet.microsoft.com/download/dotnet/10.0

### 2. Tesseract traineddata files (required for text recognition)

The Overlay project uses the `Tesseract` NuGet package (v5.2.0) — no manual DLL installation needed. However, you still need to provide the language data files. Place them in a `tessdata/` folder next to `CircleSearch.Overlay.exe`:

```
publish/
├── CircleSearch.Overlay.exe
└── tessdata/
    ├── eng.traineddata          ← English (required)
    └── vie.traineddata          ← Vietnamese (optional)
```

Download `.traineddata` files for any languages you need from the official repository: https://github.com/tesseract-ocr/tessdata

---

## Build

```bash
# Build and publish all projects to ./publish
build.bat

# Or build individually with dotnet
dotnet publish CircleSearch/CircleSearch.csproj        -c Release -r win-x64 -o publish
dotnet publish CircleSearch.Core/CircleSearch.Core.csproj   -c Release -r win-x64 -o publish
dotnet publish CircleSearch.Tray/CircleSearch.Tray.csproj   -c Release -r win-x64 -o publish
dotnet publish CircleSearch.Overlay/CircleSearch.Overlay.csproj -c Release -r win-x64 -o publish

# Or open CircleSearch.sln in Visual Studio 2022+ and Build → Build Solution
```

All four executables land in `publish/`. Copy the Tesseract DLLs and `tessdata/` folder there before running.

---

## Running

1. Launch `CircleSearch.Core.exe` — this starts the hotkey listener and spawns `CircleSearch.Tray.exe` automatically.
2. Optionally launch `CircleSearch.exe` to open the settings UI.
3. Press the hotkey (default **Ctrl + Win + Z**) to activate the overlay.

> **Note:** `CircleSearch.exe` can be set to run at Windows startup via the Settings UI. When the **View At Boot** option is disabled, the settings window will not appear on startup — only Core and Tray run silently in the background.

---

## Usage

1. **Activate** — Press the hotkey (default `Ctrl + Win + Z`).
2. **Draw** — Hold left mouse button and draw a circle or region around the content you want.
3. **Choose an action** from the popup:
   - 🔍 **Search** — OCR the region and search with your chosen engine (Google, Bing, etc.)
   - 🖼️ **Search by image** — Copy the region to clipboard and open Google Lens
   - 📋 **Copy text** — OCR offline and copy result to clipboard
   - 🌐 **Translate** — OCR and open Google Translate
4. **Dismiss** — Press `Esc` or click outside the popup.

---

## Settings

Open `CircleSearch.exe` to configure:

| Setting | Description |
|---|---|
| Hotkey | Modifier keys (Ctrl / Win / Alt / Shift) + any key |
| Search engine | Google, Bing, DuckDuckGo, Yandex, Baidu |
| OCR language | Any language supported by your `tessdata/` folder |
| Start with Windows | Register/remove from Windows startup |
| Overlay opacity | Dimming level of the fullscreen overlay |
| Accent color | Color of the drawn stroke and selection rectangle |

---

## Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| `eng.traineddata` missing | Tessdata missing | Download from github.com/tesseract-ocr/tessdata and place in `tessdata/` next to the exe |
| Hotkey not working | Conflicting shortcut | Change the key combination in Settings |
| Overlay does not appear | Core not running | Make sure `CircleSearch.Core.exe` is running |
| Poor OCR results | Region too small or blurry | Draw a larger area; ensure the text is legible on screen |
| Settings window won't open twice | Single-instance guard | The first instance will be focused automatically |

---

## Why four processes?

| Process | Reason for isolation |
|---|---|
| **Core** | Needs to stay alive in the background without a UI window; owns the hotkey Win32 message loop on a dedicated thread |
| **Settings UI** | Heavy WPF-UI / Fluent Design stack; only needed when the user wants to change settings |
| **Tray** | Must remain persistent in the system tray even when the settings window is closed |
| **Overlay** | Spawned fresh per capture so any crash or resource leak is self-contained; terminates immediately after use |

Inter-process communication is handled entirely by `ConfluxService` (named pipes), keeping each process decoupled and independently deployable.
