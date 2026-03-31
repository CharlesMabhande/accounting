Offline / air-gapped installation bundle
========================================

This accounting app can run without internet in two ways:

1) RECOMMENDED: Self-contained API (no .NET install on the target PC)
   - Default database is SQL Server (LocalDB) — use SSMS with Connect-with-SSMS.txt.
   - On a PC with the .NET 8 SDK (one-time, can be online), run:
       build\Build-Redistributable.ps1
   - Ship dist\Accounting-Redistributable.zip or copy the whole folder dist\AccountingApi\
   - On the target PC (offline): extract, double-click SETUP.bat or Run-Accounting.cmd
     (SETUP.bat uses -Provider SqlServer; optional -Provider Sqlite for a file DB only).

2) Build from source offline
   - While online once, run: build\Prepare-Offline.ps1
   - This fills the .\packages folder (see nuget.config: globalPackagesFolder) with every NuGet dependency.
   - Zip the whole repository INCLUDING the "packages" folder.
   - Offline: install .NET 8 SDK using a Microsoft offline installer you copy into this redist folder,
     then dotnet restore / dotnet build as usual (restore uses the local packages folder).

Optional redistributables (place installers here for reference)
----------------------------------------------------------------
- .NET 8 SDK (x64) offline installer — only if you build from source offline.
- .NET 8 ASP.NET Core Runtime / Hosting Bundle — NOT required if you use self-contained publish.

SQL Server
----------
The application does not bundle Microsoft SQL Server. You need one of:
- SQL Server Express (local or network)
- LocalDB (often installed with Visual Studio / Build Tools)
- Full SQL Server (SSMS indicates you likely already have a compatible instance)

The API creates the database automatically on first run if the login can CREATE DATABASE.
