# BMS MySQL 3307 Setup

## Purpose

The existing shared MySQL instance remains on port `3306`. The BMS database runs in an isolated MySQL instance on port `3307`, with separate root credentials.

## Final Configuration

| Item | Value |
| --- | --- |
| Existing MySQL | Port `3306` — unchanged |
| BMS MySQL service | `MySQL84-BMS` |
| BMS MySQL port | `3307` |
| BMS data directory | `C:\ProgramData\MySQL\BMS-Data` |
| BMS config file | `C:\ProgramData\MySQL\BMS-my.ini` |
| MySQL binary | `C:\Data\SETUP\mysql-8.4.1-winx64\mysql-8.4.1-winx64\bin\mysqld.exe` |
| BMS database | `aryamanbms` |
| Application MySQL user | `aryamanbms@localhost` |

Passwords must not be stored in this document.

## BMS MySQL Configuration File

`C:\ProgramData\MySQL\BMS-my.ini`:

```ini
[mysqld]
basedir=C:/Data/SETUP/mysql-8.4.1-winx64/mysql-8.4.1-winx64
datadir=C:/ProgramData/MySQL/BMS-Data/
port=3307
mysqlx-port=33070
```

## Database Users

- `root@localhost`: private administration for the BMS instance only.
- `aryamanbms@localhost`: used by the BMS application only; granted access to `aryamanbms.*`.

Do not use `root` in the BMS application connection string.

## Application Connection String

The deployed BMS `appsettings.json` connection value must include port `3307`:

```json
"DefaultConnection": "server=127.0.0.1;port=3307;database=aryamanbms;user=aryamanbms;password=REPLACE_WITH_SECRET;"
```

After changing it, restart the BMS IIS application pool.

## Migration Completed

1. Exported the old BMS database from the existing `3306` instance using MySQL Workbench **Server > Data Export**.
2. Imported that self-contained SQL file into the new `3307` instance using **Server > Data Import**.
3. Verified tables and table count on the new instance.

## Folder Permissions

`C:\ProgramData\MySQL\BMS-Data` is restricted to:

- `SYSTEM` — Full control (the `MySQL84-BMS` service runs as `LocalSystem`).
- `Administrators` — Full control.

Removed entries:

- `Users`
- `CREATOR OWNER`
- Any other ordinary-user or employee-specific entries

Inheritance is disabled. Child files and folders use the same restricted permissions.

Windows Administrators can always administer or recover the database. Standard server users cannot open or copy the BMS data files.

## Workbench Connections

- Existing/shared MySQL: port `3306`.
- BMS administration: `127.0.0.1:3307`, user `root`.

Do not save BMS passwords in MySQL Workbench Vault. Workbench should prompt for the password when a new connection is opened.

### Raw-File Access

Raw files in `BMS-Data` are protected by Windows permissions, not by MySQL usernames or passwords.

- A MySQL `root` or `aryamanbms` password does not permit someone to open `BMS-Data`.
- Only `SYSTEM` (the MySQL service) and Windows `Administrators` can read or copy raw BMS database files.
- Standard Windows users cannot access those files.

## Errors Resolved

### New MySQL service would not start

Cause: `datadir` in `BMS-my.ini` used incorrect paths with spaces:

```ini
datadir=C:/Program Data/MySQL/BMS Data/
```

Resolution: changed it to the actual folder path:

```ini
datadir=C:/ProgramData/MySQL/BMS-Data/
```

### IIS BMS startup error (HTTP 500.30)

Cause: the BMS connection string did not specify port `3307`, so it tried the old MySQL instance on the default port `3306`.

Resolution: added `port=3307` to `DefaultConnection`, then restarted the IIS application pool.

## Recovery / Handover

Keep regular encrypted database backups in separate secure storage. Use MySQL Workbench Data Export or `mysqldump`; do not copy the live data directory as a backup.

For a handover, provide an authorized administrator with secure access to the server, BMS MySQL root credentials, application MySQL credentials, IIS details, and backup/restore procedure. Rotate all credentials after handover.
