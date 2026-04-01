Accounting API — redistributable package
==========================================

DEFAULT: SQL Server + SSMS (recommended if you use Management Studio)
-----------------------------------------------------------------------
The app connects to SQL Server (default: LocalDB). You use the SAME server and database in SSMS.

Windows Authentication (default in examples): Trusted_Connection=True in the connection string.

SQL Server Authentication: use User Id= and Password= in ConnectionStrings:DefaultConnection.
See appsettings.Local.SqlAuth.json.example and Connect-with-SSMS.txt.

  Install-Accounting.ps1 -SqlUser "AccountingApp" -SqlPassword "..." -Server ".\SQLEXPRESS" -Database AccountingDb
  Install-Accounting.ps1 -SqlUser "AccountingApp" -SqlPasswordFile "C:\path\password.txt" ...

OFFLINE BUNDLE (USB / air-gapped PCs)
-------------------------------------
On a build machine with the .NET 8 SDK:
  compile-offline.bat
  OR:  build\Build-OfflineBundle.ps1

Output:
  dist\AccountingOffline\AccountingOfflineSetup.exe  — wizard: optional redist\ installers, then copy + configure
  dist\Accounting-Offline-Bundle.zip                   — entire folder for USB

Run Prepare-Offline.ps1 once while ONLINE so .\packages is filled; then builds work without Internet.
Add optional VC++ / SQL LocalDB MSIs to dist\AccountingOffline\redist\ (see redist\README.txt in the bundle).

FULL INSTALLER (GUI Setup.exe)
------------------------------
After building on a developer PC:
  build\Build-Setup.ps1

This creates dist\AccountingInstaller\ containing Setup.exe + Accounting.Api + Desktop\Accounting.Desktop.exe + scripts.
On the target PC: double-click Setup.exe (CharlzTech-branded icon). The wizard will:
  - Welcome: what is installed (API + desktop + config).
  - Installation folder: default %LocalAppData%\Accounting (no admin). Optionally copy all files from the USB/zip folder.
  - SQL Server: instance name; optional skip SQL probe / skip firewall.
  - Progress: runs Install-Accounting.ps1 (appsettings.Local.json + optional firewall).
  - Finish: start API, start desktop, shortcuts, open Swagger (http://localhost:8080/swagger).
  Desktop is preconfigured for http://localhost:8080 (production API port).

Alternatively (no GUI):
1. Copy the published folder (e.g. dist\AccountingApi\ or dist\AccountingInstaller\) to the PC.
2. Run SETUP.bat (writes appsettings.Local.json for SQL Server + optional firewall).
   Or set Database:Provider to SqlServer and ConnectionStrings:DefaultConnection yourself.
   Optional: create the empty SQL database before starting the API (requires sqlcmd, e.g. from SSMS):
     Install-SqlServerDatabase.ps1 -Server "(localdb)\mssqllocaldb" -Database AccountingDb
   Or: Install-Accounting.ps1 ... -CreateDatabase
3. Start the API once (Run-Accounting.cmd) so tables and seed data are created (if the DB was empty).
4. Open SSMS → connect to (localdb)\mssqllocaldb → database AccountingDb.
   Details: Connect-with-SSMS.txt

5. API: http://localhost:8080/swagger

No internet required. No .NET install required (self-contained build).
SQL Server / LocalDB / Express must be installed separately (SSMS usually means you already have tools).

OPTIONAL: SQLite (portable file, no SSMS)
----------------------------------------
Single file Data\Accounting.db — no SQL Server. SSMS does not apply.

  powershell -ExecutionPolicy Bypass -File .\Install-Accounting.ps1 -Provider Sqlite

Or in appsettings.json / appsettings.Local.json:

  "Database": { "Provider": "Sqlite", "SqlitePath": "Data/Accounting.db" }

WHAT IS NOT INCLUDED IN THIS ZIP
---------------------------------
- Microsoft SQL Server setup media (use an existing install or LocalDB).
- Internet access (not required after you copy this folder).

BUILD THIS PACKAGE (developer machine, once)
--------------------------------------------
  build\Publish-SelfContained.ps1
  build\Build-Redistributable.ps1

Output: dist\Accounting-Redistributable.zip
