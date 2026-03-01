# ENGINE.exe — Current C# Source Files

**Current folder:** `i:\UI_GUI\ENGINE` (this is the one to edit; see repo root `CURRENT_SOURCES.md`.)

---

## Projects & outputs

| Project | Output | Purpose |
|--------|--------|--------|
| **SmokeScreenEngineGUI.csproj** | `SmokeScreenEngine.exe` (GUI) | WinForms app: Hub, keys, Discord, Cloud Dashboard, etc. |
| **ENGINE.csproj** | `ENGINE.exe` (console) | Headless/API build (no WinForms). |

Build from this folder:
```bat
dotnet build SmokeScreenEngineGUI.csproj -c Release   → bin\Release\net8.0-windows\SmokeScreenEngine.exe
dotnet build ENGINE.csproj -c Release                 → bin\Release\net8.0\ENGINE.exe
```

---

## Current C# files (by area)

### Entry & shell
- **GUIProgram.cs** — WinForms entry (SmokeScreenEngine GUI).
- **Program.cs** / **EngineProgram.cs** — Console entry (ENGINE.exe).
- **HubForm.cs** — Main hub (tabs, login, keys, redeem, ping, sync).

### Auth
- **ClerkAuth.cs** — Clerk (web) auth.
- **DiscordAuth.cs** — Discord OAuth for ENGINE.

### Keys & sync
- **KeyCache.cs** — Local key cache.
- **KeyExtension.cs** — Key extension / TSync send.
- **KeyGenerator.cs** — Key generation UI.
- **KeySender.cs** — Send keys to bot/API.
- **KeyService.cs** — Key service (admin add, etc.).
- **TSyncListener.cs** — Background sync from website.
- **SyncResponse.cs** — DTO for /api/sync.

### UI / forms
- **Theme.cs** — Theme/colors.
- **EnginePage.cs** — ENGINE tab/page.
- **CloudDashboardForm.cs** — Cloud dashboard.
- **MarketplaceForm.cs** — Marketplace.
- **MainForm.cs** — Legacy main form.
- **SpooferForm.cs** — Spoofer UI.
- **ChartControl.cs** — Chart control.
- **MsPingStatus.cs** — Ping status control.

### API / server (ENGINE.exe console)
- **ApiServer.cs** — API server.
- **ApiService.cs** — API service.
- **Database.cs** — DB access.
- **Models.cs** — Data models.
- **AdminConsole.cs** — Admin console.
- **KeyService.cs** — Key operations.

### Other
- **HardwareId.cs** — HWID.
- **EngineClient.cs** — HTTP client for website/API.

---

## Config / build

- **SmokeScreenEngineGUI.csproj** — GUI project (WinForms, net8.0-windows).
- **ENGINE.csproj** — Console project (net8.0); excludes GUI-only files.
- **Donny.AI/** — AI manager manifest.
- **DonnySync.LLM.AI** — Sync/events spec.

Edit only under `i:\UI_GUI\ENGINE`; ignore the duplicate under `SmokeScreen-ENGINE\ENGINE` unless you intentionally sync.
