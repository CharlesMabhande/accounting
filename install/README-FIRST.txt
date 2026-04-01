CharlzTech Accounting — bundled installation
==============================================

ONE-CLICK INSTALL (recommended)
---------------------------------
1. Open this folder (AccountingInstaller).
2. Double-click:  INSTALL.bat   OR   CharlzTech.exe
3. Click Next on the welcome page.
4. On the single configuration page:
   - Leave the install folder as suggested, or choose another folder.
   - Paste your SQL Server connection string (same as in SSMS).
5. Click Install.

Setup will:
  • Write configuration (appsettings.Local.json)
  • Start the Accounting API (if needed)
  • Open the desktop login window
  • Register the API to start when you sign in to Windows (optional)

After installation, you may open the desktop app from:
  Desktop\Accounting.Desktop.exe

The desktop client will start the API automatically on the login screen when using a local URL.

OTHER FILES
-----------
CharlzTech.exe   — GUI installer (same as INSTALL.bat; CharlzTech logo icon)
Run-Accounting.cmd — Start the API manually if needed
Uninstall-Accounting.cmd — Remove from Programs and Features

Technical support: use the same connection string in SSMS to verify the database.
