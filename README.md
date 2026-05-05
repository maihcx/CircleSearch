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
├── CircleSearch.CFS/          ← Shared IPC library (ConfluxService — named-pipe messaging)
└── CircleSearch.Installer/    ← WPF installer (self-contained single-file .exe)
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

### CircleSearch.Installer

- Single-file WPF installer: all app binaries and `CircleSearch.Installer.exe` itself are embedded as a ZIP resource inside the installer executable.
- On install: extracts the payload to the chosen directory, creates shortcuts, registers with Windows (Add/Remove Programs), and optionally sets up autostart.
- On uninstall: kills running processes, removes files, shortcuts, startup entry, and registry keys; schedules self-deletion via a `cmd.exe` deferred command.
- Debugging the installer project in Visual Studio works without a `payload.zip` present. The ZIP is only required (and enforced) when publishing in Release via `build.bat`.

---

## Prerequisites

### 1. .NET 10 SDK

Download from: https://dotnet.microsoft.com/download/dotnet/10.0

### 2. Tesseract traineddata files (required for text recognition)

The Overlay project uses the `Tesseract` NuGet package (v5.2.0) — no manual DLL installation needed. However, you still need to provide the language data files. Place them in a `tessdata/` folder next to `CircleSearch.Overlay.exe`:

```
<install dir>/
├── CircleSearch.Overlay.exe
└── tessdata/
    ├── eng.traineddata          ← English (required)
    └── vie.traineddata          ← Vietnamese (optional)
```

Download `.traineddata` files for any languages you need from the official repository: https://github.com/tesseract-ocr/tessdata

---

## Build

### Release — single-file installer (recommended)

Run `build.bat` from the repository root:

```
build.bat
```

The script performs the following steps automatically:

1. Publishes `CircleSearch`, `CircleSearch.Overlay`, `CircleSearch.Tray`, and `CircleSearch.Core` into a temporary `installer-output\publish\` folder.
2. Creates an empty `payload.zip` placeholder so the installer project compiles on pass 1.
3. Publishes `CircleSearch.Installer` (pass 1) to obtain the installer executable.
4. Copies `CircleSearch.Installer.exe` into `publish\` so it is included in the payload.
5. Zips the entire `publish\` folder into `CircleSearch.Installer\Resources\payload.zip`.
6. Republishes `CircleSearch.Installer` (pass 2) with the real payload embedded.
7. Removes all intermediate files (`publish\` folder and `payload.zip`).

Result: a single **`installer-output\CircleSearch.Installer.exe`** that contains every binary needed to install the application.

> **Note:** `payload.zip` is a build-time artifact and is not committed to the repository. It is created and deleted automatically by `build.bat`.

### Debug — Visual Studio

Open `CircleSearch.sln` in Visual Studio 2022 or later and set `CircleSearch.Installer` as the startup project (or any other project you want to debug). Building and running in `Debug` configuration works normally without `payload.zip` present — the payload check is skipped and the embedded resource is omitted in non-Release builds.

---

## Running

### Via installer

Run `CircleSearch.Installer.exe`, choose an install directory, and click **Install**. The installer will:

1. Extract all application files to the chosen directory.
2. Optionally create a Desktop and/or Start Menu shortcut.
3. Optionally register `CircleSearch.exe` to run at Windows startup.
4. Register the application in **Add or Remove Programs** for clean uninstallation.

After installation, launch **CircleSearch** from the shortcut or Start Menu. `CircleSearch.exe` is the single entry point — it automatically spawns Core, Tray, and all background services.

### Manually (without installer)

1. Publish all four app projects to the same output folder (or run `build.bat` and copy `publish\` contents).
2. Place Tesseract `tessdata/` folder next to `CircleSearch.Overlay.exe`.
3. Launch `CircleSearch.exe` — this is the single entry point. It automatically starts Core and Tray in the background.
4. Press the hotkey (default **Ctrl + Win + Z**) to activate the overlay.

> **Note:** `CircleSearch.exe` can be set to run at Windows startup via the Settings UI. When the **View At Boot** option is disabled, the settings window will not appear on startup — Core and Tray continue running silently in the background.

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
| Overlay does not appear | Core not running | Make sure `CircleSearch.exe` was launched — it spawns Core automatically |
| Poor OCR results | Region too small or blurry | Draw a larger area; ensure the text is legible on screen |
| Settings window won't open twice | Single-instance guard | The first instance will be focused automatically |
| Installer shows wrong size (~5 MB) | `payload.zip` was not embedded | Delete any leftover `payload.zip` and re-run `build.bat` from scratch |

---

## Why four processes?

| Process | Reason for isolation |
|---|---|
| **Core** | Needs to stay alive in the background without a UI window; owns the hotkey Win32 message loop on a dedicated thread |
| **Settings UI** | Heavy WPF-UI / Fluent Design stack; only needed when the user wants to change settings |
| **Tray** | Must remain persistent in the system tray even when the settings window is closed |
| **Overlay** | Spawned fresh per capture so any crash or resource leak is self-contained; terminates immediately after use |

Inter-process communication is handled entirely by `ConfluxService` (named pipes), keeping each process decoupled and independently deployable.
