-- Optional manual step if you prefer SSMS over automatic creation.
-- The API also creates the database on startup when possible.
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'AccountingDb')
BEGIN
    CREATE DATABASE [AccountingDb];
END
GO
