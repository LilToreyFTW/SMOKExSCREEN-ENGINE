# ENGINE — License Key Management System

## Overview

ENGINE is a self-hosted, offline-capable license key system built in C# / .NET 8.
It runs as a local API server + admin console and manages keys tied to user HWIDs.

---

## Project Structure

```
ENGINE/
├── Program.cs                  ← Entry point (boots API + admin console)
├── ENGINE.csproj
├── engine.db                   ← SQLite database (auto-created on first run)
│
├── Models/
│   └── Models.cs               ← LicenseKey, AnalyticsEvent, HwidBan
│
├── Data/
│   └── Database.cs             ← All DB operations (insert, validate, revoke, analytics)
│
├── Services/
│   ├── KeyService.cs           ← Key generation (1000 per duration)
│   └── AdminConsole.cs        ← Interactive admin UI (Spectre.Console)
│
├── Api/
│   └── ApiServer.cs           ← Local REST API (http://localhost:5150)
│
└── EngineClient.cs            ← Drop into client app for validation
```

---

## Setup

### Requirements
- .NET 8 SDK
- Windows (for HWID via WMI) or Linux/Mac (fallback HWID used)

### Build & Run

```bash
cd ENGINE
dotnet restore
dotnet run
```

On first run, ENGINE will:
1. Create `engine.db` (SQLite database)
2. Start the local API on `http://localhost:5150`
3. Open the interactive admin console

---

## Admin Console Features

| Feature                  | Description                                      |
|--------------------------|--------------------------------------------------|
| Generate Keys            | Create 1000 keys per duration (4000 total)       |
| View All Keys            | Filter by status: Unused / Active / Expired etc. |
| Lookup Key               | Full details for any single key                  |
| Revoke Key               | Instantly disable a key with a reason            |
| Ban HWID                 | Block a device + revoke all its keys             |
| Export Keys to .txt      | Export unused/active/all keys grouped by duration|
| Analytics Dashboard      | Summary stats, daily activity, top HWIDs         |
| Recent Events Log        | Last 100 auth events with success/failure status |

---

## API Endpoints

The ENGINE runs a local HTTP server on port **5150**.

### `POST /validate`
Used by client apps to validate a license key.

**Request:**
```json
{
  "key": "ABCD-EFGH-IJKL-MNOP",
  "hwid": "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4",
  "version": "1.0.0"
}
```

**Response:**
```json
{
  "success": true,
  "reason": "OK",
  "timestamp": "2025-01-01T00:00:00Z"
}
```

**Failure reasons:**
| Reason         | Meaning                          |
|----------------|----------------------------------|
| INVALID_KEY    | Key does not exist               |
| KEY_REVOKED    | Key was manually revoked         |
| KEY_EXPIRED    | Key's duration has passed        |
| HWID_MISMATCH  | Key bound to a different device  |
| HWID_BANNED    | Device is banned                 |

### `GET /ping`
Health check — returns `{ "status": "ENGINE_ONLINE" }`.

---

## Key Durations

| Label    | Duration  | Keys Generated |
|----------|-----------|----------------|
| 1 Day    | 24 hours  | 1000           |
| 7 Days   | 7 days    | 1000           |
| 30 Days  | 30 days   | 1000           |
| Lifetime | Never exp | 1000           |

---

## Client App Integration

Copy `EngineClient.cs` into your client application.

```csharp
using EngineClient;

// On startup:
var (valid, reason) = await LicenseValidator.ValidateAsync(userKey, "1.0.0");

if (!valid)
{
    MessageBox.Show(LicenseValidator.DescribeReason(reason));
    return;
}
// App continues...
```

**HWID Generation:** Uses CPU ID + Motherboard serial → SHA256 → 32 hex chars.
Requires `System.Management` NuGet on Windows.

---

## How Key Activation Works

1. User enters key in your client app
2. Client calls `POST /validate` with key + HWID
3. If key is **Unused**, ENGINE activates it and binds it to that HWID
4. All future validations must match the same HWID
5. Key is marked Expired automatically when duration runs out
6. Admin can Revoke a key or Ban a HWID at any time

---

## Notes

- `engine.db` is the source of truth — back it up regularly
- ENGINE must be running for client validation to work
- Port 5150 can be changed in `Program.cs` and `EngineClient.cs`
- Analytics log every validation attempt (success and failure)
