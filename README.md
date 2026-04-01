# CharlzTech Accounting

Enterprise-style accounting workspace: **ASP.NET Core Web API** backend and **WPF** desktop client. The API hosts business logic and data access; the desktop app calls the API with session-based authentication.

---

## Screenshots

UI captures for the installer and client live in **[`Screenshots/`](Screenshots/)** (see also [`Screenshots/README.md`](Screenshots/README.md)). Images are referenced below for quick viewing on GitHub.

![Setup wizard — welcome](Screenshots/Screenshot%202026-04-01%20114903.png)

![Setup wizard — configuration](Screenshots/Screenshot%202026-04-01%20114959.png)

![Desktop — sign in](Screenshots/Screenshot%202026-04-01%20115115.png)

---

## Table of contents

1. [Requirements](#requirements)
2. [Solution layout](#solution-layout)
3. [Quick start (development)](#quick-start-development)
4. [Configuration](#configuration)
5. [Database](#database)
6. [Build and run](#build-and-run)
7. [First sign-in and sample users](#first-sign-in-and-sample-users)
8. [Desktop application](#desktop-application)
9. [API endpoints (overview)](#api-endpoints-overview)
10. [Backup and restore](#backup-and-restore)
11. [Belts legacy import](#belts-legacy-import)
12. [Swagger](#swagger)
13. [Troubleshooting](#troubleshooting)
14. [Security notes](#security-notes)
15. [Support and development](#support-and-development)

---

## Requirements

| Component | Notes |
|-----------|--------|
| **.NET SDK** | 8.0 or later (`dotnet --version`) |
| **OS** | Windows for **Accounting.Desktop** (WPF). The API runs on Windows or Linux. |
| **Database** | **SQL Server** (LocalDB, full SQL Server, or Express) for the default repo settings, or **SQLite** for a single-file portable database. |
| **Optional** | SQL Server Management Studio (SSMS) if you manage databases manually. |

---

## Solution layout

| Project | Role |
|---------|------|
| `Accounting.Api` | HTTP API, Kestrel host, authentication middleware, controllers. |
| `Accounting.Desktop` | WPF client; connects to the API base URL from config. |
| `Accounting.Application` | Use cases, DTOs, service interfaces. |
| `Accounting.Domain` | Entities and domain rules. |
| `Accounting.Infrastructure` | EF Core `DbContext`, repositories, external integrations. |
| `Accounting.Bootstrapper` / `Accounting.Setup` | Installer / setup-related assets (see installer docs if you ship offline installs). |

**Solution file:** `Accounting.sln` (use this to build the whole system).

---

## Quick start (development)

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Ensure SQL Server **LocalDB** is available **or** change configuration to **SQLite** (see [Database](#database)).
3. From the repository root:

   ```bash
   dotnet build Accounting.sln -c Release
   ```

4. Start the API (see [Build and run](#build-and-run)).
5. Start the desktop app and sign in (see [First sign-in](#first-sign-in-and-sample-users)).

The API listens on **`http://localhost:5151`** in Development unless you override URLs or use another launch profile.

---

## Configuration

### API (`src/Accounting.Api/appsettings.json`)

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server or SQLite connection string. |
| `Database:Provider` | `SqlServer` or `Sqlite` (if omitted, the host infers from the connection string). |
| `Database:SqlitePath` | Relative path for SQLite file (under the API’s base directory), default `Data/Accounting.db`. |
| `Api:EnableSwagger` | Set `true` to expose Swagger UI in non-Development environments. |
| `Belts:SourceConnectionString` | SQL Server connection for **Belts** master-data import (legacy database). |

**Local overrides:** The API loads **`appsettings.Local.json`** if present (optional, not committed). Use it for machine-specific connection strings or secrets.

### Desktop (`src/Accounting.Desktop/appsettings.json`)

| Key | Purpose |
|-----|---------|
| `AccountingApi:BaseUrl` | Base URL of the API (default `http://localhost:5151`). Must match where the API is running. |
| `ConnectionDisplay` | Labels shown in the status bar (instance / database name); informational only. |

Copy `appsettings.json` beside the desktop executable or ensure it is published with the app. Change **`BaseUrl`** if the API uses another host or port.

---

## Database

### SQL Server (default in repo `appsettings.json`)

- Default example uses **LocalDB**: `Server=(localdb)\mssqllocaldb;Database=AccountingDb;...`
- On first run, startup code can create the database and apply the EF model; **schema patches** run after `EnsureCreated` where applicable.
- Adjust **`DefaultConnection`** for your instance (e.g. `.\SQLEXPRESS`) and database name.

### SQLite

Set:

```json
"Database": {
  "Provider": "Sqlite",
  "SqlitePath": "Data/Accounting.db"
}
```

and provide a SQLite-style `DefaultConnection` if your hosting expects it, or rely on the infrastructure configuration that builds `Data Source=...` from `SqlitePath`. The API creates the data directory if needed.

**Important:** Logical backup ZIP import/export is **provider-specific**—do not import a SQLite export into SQL Server or the reverse without migrating.

---

## Build and run

### Build the full solution

From the repository root:

```bash
dotnet build Accounting.sln -c Release
```

Use **`Accounting.sln`** (not multiple solution files in the same folder).

### Run the API

```bash
cd src/Accounting.Api
dotnet run
```

Or run the **Accounting.Api** project from Visual Studio / Rider using the launch profile **`Accounting.Api`** (HTTP **`http://localhost:5151`**).

- **Production-style URL** (see `Program.cs`): non-Development may bind to `http://0.0.0.0:8080`.
- **Health check (no auth):** `GET /api/health` — useful to verify the service is up.

### Run the desktop client

```bash
cd src/Accounting.Desktop
dotnet run
```

Ensure the API is already running and **`AccountingApi:BaseUrl`** matches.

### Build order / file locks

If **`dotnet build`** fails with **MSB3026/3027** (DLL copy errors), stop running instances of **Accounting.Api** or **Accounting.Desktop** so files in `bin` are not locked. The API project may include a pre-build step to reduce port conflicts on Windows.

---

## First sign-in and sample users

On a fresh database, the seeder creates roles and optional default users.

| User | Password (initial) | Notes |
|------|-------------------|--------|
| `admin` | `Admin123!` | **Administrator** role (full permissions). **Change this password in production.** |
| `agent` | `Agent123!` | **Agent** role (limited navigation), if seeded. |

Permissions are **role-based** (e.g. `nav.*`, `security.users.manage`, `security.backup.manage`). The default **User** role does not include destructive admin modules such as **Backup & restore** unless you assign them in **Roles & permissions**.

---

## Desktop application

1. Launch **Accounting.Desktop**.
2. Enter API URL if prompted or confirm it matches `appsettings.json` (status bar shows reachability).
3. Sign in with a valid user.
4. Select a **company** in the header when the module requires a company context.
5. Use the **Modules** tree to open workspaces (GL, AR, AP, inventory, etc.).

**About / developer info:** **Help → About** shows product text and developer contact details.

---

## API endpoints (overview)

- **Authentication:** `POST /api/auth/login`, session cookie/header as implemented by `SessionAuthMiddleware`.
- **Companies, journals, inquiries:** under `api/companies/{companyId}/...` and related routes.
- **Administration:** users, roles, audit settings, fiscal periods, etc., under respective controllers.
- **Database backup:** `api/database-backup/*` (authenticated; requires backup permission). See [Backup and restore](#backup-and-restore).

Refer to Swagger (when enabled) or the `Controllers` folder for the full list.

---

## Backup and restore

Requires permission **`security.backup.manage`** (and navigation entry **Backup & restore** for the desktop).

| Action | HTTP | Notes |
|--------|------|--------|
| Logical export | `GET /api/database-backup/export-json` | ZIP with JSON per table (portable format). |
| Logical import | `POST /api/database-backup/import-json` | Multipart form field **`file`** (ZIP from export). **Replaces data** in the current database. Same provider as export (SQLite vs SQL Server). |
| Native file | `GET /api/database-backup/native` | **SQLite:** copies the `.db` file. **SQL Server:** runs `BACKUP DATABASE` to a temporary `.bak` and returns bytes (requires server permission). |

**Procedure (desktop):** open **Administration → Backup & restore** — export ZIP, import ZIP, or download native backup. Large uploads are allowed up to the configured Kestrel/form limits (default order of hundreds of MB).

**Before import:** take a separate backup. Import is destructive for existing rows in the target database.

---

## Belts legacy import

The **Belts import** module pulls master data from a legacy SQL Server database whose connection is set in **`Belts:SourceConnectionString`**.

1. Ensure the legacy database is reachable from the machine running the **API** (not only the desktop).
2. Configure the connection string in `appsettings.json` or `appsettings.Local.json`.
3. In the desktop, select a company, open **Belts import**, choose options (stock, customers, suppliers, overwrite), and run **Run import**.

---

## Swagger

- In **Development**, Swagger UI is enabled by default.
- In other environments, set **`Api:EnableSwagger`** to `true` in configuration.
- Typical URL: `http://localhost:5151/swagger` (when using the default dev port).

---

## Troubleshooting

| Symptom | What to check |
|---------|----------------|
| Desktop closes or cannot connect | API not running; wrong **BaseUrl**; firewall. Test `GET http://localhost:5151/api/health`. |
| Login fails | User exists, password correct, user active; API logs. |
| Build fails copying DLLs | Stop **Accounting.Api** / **Accounting.Desktop** processes. |
| Port 5151 already in use | Change `applicationUrl` in `launchSettings.json` or set `ASPNETCORE_URLS`; update desktop **BaseUrl** to match. |
| SQL Server connection errors | Connection string, instance name, TCP enabled, database created, permissions. |
| Backup import rejected | ZIP from a different database **provider** (SQLite vs SQL Server). |

---

## Security notes

- Replace default **admin** credentials before production use.
- Run the API behind HTTPS and a reverse proxy in production; restrict network access.
- Treat **backup** and **import** endpoints as highly sensitive; grant only to trusted administrators.
- Review **CORS**, **AllowedHosts**, and session settings if you expose the API beyond localhost.

---

## Support and development

**CharlzTech Web Developers (Charles Mabhande)**  

- Email: [charliemabhande@gmail.com](mailto:charliemabhande@gmail.com)  
- Phone: +263 776 318 768  

Product and licensing information appear in the desktop **About** dialog.

---

*This README describes the development and operational flow for the CharlzTech Accounting codebase. Adjust paths, ports, and connection strings to match your deployment environment.*
