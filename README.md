# Purge-Temp

A lightweight Windows command-line utility that keeps a folder clean through **staged deletion**: files age through a configurable number of "stage" folders and are permanently deleted only when they reach the oldest stage. This gives you a built-in grace period — files are never lost immediately.

---

## How It Works

Instead of deleting files straight away, Purge-Temp maintains a sequence of numbered stage folders under a configurable root:

```
C:\purge-temp-new\
  purge-temp-new\          ← Stage 1 (newest — drop your files here)
  purge-temp-new-2\        ← Stage 2
  purge-temp-new-3\        ← Stage 3
  purge-temp-new-LAST\     ← Stage 4 / last (permanently deleted on next run)
```

Each time the application runs (subject to a configurable minimum delay):

1. **Delete** everything inside the `LAST` folder.
2. **Rotate** each folder one stage forward (`stage 3 → LAST`, `2 → 3`, `1 → 2`).
3. **Create** a fresh empty stage 1 folder, ready for new files.
4. **Record** the current timestamp so the delay can be enforced next time.

With the default settings (4 stages, 6-hour minimum delay), a file placed in stage 1 survives at least **3 more purge cycles** — providing roughly **18 hours** of retention when purge runs every 6 hours.

:exclamation: **Important :exclamation: : Purge-Temp does not schedule purge cycles by itself**. See section [Scheduling](#scheduling) for further details

---

## Requirements

- Windows (desktop notifications use the Windows API)
- .NET 6 or later

---

## Build

```bash
# Restore and build
dotnet build PurgeTemp.sln

# Run tests
dotnet test PurgeTemp.sln
```

---

## Configuration

All settings live in `appsettings.json` next to the executable. Every setting can also be overridden via a CLI flag (see next section).

```jsonc
{
  "AppSettings": {

    // ── Staging ────────────────────────────────────────────────────────────
    "StageRootFolder": "C:\\purge-temp-new",   // Root directory that holds all stage folders
    "StageVersions": 4,                         // Total number of stage folders (must be ≥ 1)
    "StageNamePrefix": "purge-temp-new",        // Prefix shared by all stage folder names
    "AppendNumberOnFirstStage": false,          // If true, stage 1 gets a "-1" suffix too
    "StageVersionDelimiter": "-",               // Separator between prefix and stage number
    "StageLastNameSuffix": "LAST",              // Suffix applied to the last stage folder, before files will be deleted for good
    "StagingDelaySeconds": 21600,               // Min. seconds between purge runs (default 6 h)
    "RemoveEmptyStageFolders": false,           // Delete intermediate stage folders when empty

    // ── Token / timestamp files ────────────────────────────────────────────
    "ConfigFolder": ".\\config",               // Folder where administrative files are kept
    "StagingTimestampFile": ".\\last-purge.txt",// Records the time of the last successful purge
    "SkipTokenFile": ".\\SKIP.txt",            // Place this file in stage 1 to skip the next run
    "TimeStampFormat": "yyyy-MM-dd HH:mm:ss",  // DateTime format used in the timestamp file

    // ── Working / temp folder ──────────────────────────────────────────────
    "TempFolder": ".\\temp",                   // Internal working temp folder for the app

    // ── Logging ───────────────────────────────────────────────────────────
    "LogEnabled": true,                         // Enable/disable file logging (Serilog)
    "LoggingFolder": ".\\log",                 // Directory for log files
    "LogRotationBytes": 10485760,              // Max log file size in bytes (0 = unlimited)
    "LogRotationVersions": 10,                 // Max number of retained log files (0 = unlimited)
    "LogAllFiles": true,                        // Log every file processed during a purge
    "FileLogAmountThreshold": 1000,            // After this many files, log only the remainder count

    // ── Desktop notifications ──────────────────────────────────────────────
    "ShowPurgeMessage": true,                   // Show Windows toast/balloon notifications
    "PurgeMessageLogoFile": ""                 // Optional icon file shown in notifications
  }
}
```

> **Relative paths** in `ConfigFolder`, `LoggingFolder`, `TempFolder`, `StagingTimestampFile`, and `SkipTokenFile` are resolved relative to the executable's working directory.

---

## CLI Flags

All flags are optional. When provided they override the corresponding value in `appsettings.json`.

| Short | Long flag | Description |
|-------|-----------|-------------|
| `-s` | `--settings-file` | Path to an alternate settings file (overrides `appsettings.json`; individual CLI flags can still override settings in that file) |
| `-r` | `--stage-root-folder` | Stage root folder |
| `-v` | `--stage-versions` | Number of stage versions (≥ 1) |
| `-p` | `--stage-name-prefix` | Prefix for stage folder names |
| `-a` | `--append-number-on-first-stage` | Append stage number to the first stage folder name |
| `-d` | `--stage-version-delimiter` | Delimiter between prefix and stage number |
| `-l` | `--stage-last-name-suffix` | Suffix for the last/oldest stage folder |
| `-t` | `--staging-delay-seconds` | Minimum seconds between two purge runs |
| `-u` | `--remove-empty-stage-folders` | Remove intermediate stage folders when empty |
| `-g` | `--staging-timestamp-file` | Path/name of the last-purge timestamp file |
| `-k` | `--skip-token-file` | Name of the skip token file |
| `-y` | `--timestamp-format` | DateTime format string for the timestamp file |
| `-c` | `--config-folder` | Administrative config folder |
| `-z` | `--temp-folder` | Internal working temp folder |
| `-e` | `--log-enabled` | Enable (`true`) or disable (`false`) file logging |
| `-f` | `--logging-folder` | Directory for log output |
| `-b` | `--log-rotation-bytes` | Max log file size in bytes (0 = unlimited) |
| `-o` | `--log-rotation-versions` | Max retained log file versions (0 = unlimited) |
| `-n` | `--log-all-files` | Log every file processed during a purge |
| `-q` | `--file-log-amount-threshold` | Log threshold — after N files, only log the remaining count (−1 = log all) |
| `-m` | `--show-purge-message` | Show desktop notifications |
| `-i` | `--purge-message-logo-file` | Icon file for desktop notifications |

Pass `--help` to print the full help text with version information.

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Purge completed successfully |
| `1` | Skipped — minimum delay since last purge not yet elapsed |
| `2` | Skipped — skip token file was found in stage 1 |
| `100–114` | Invalid arguments or configuration |
| `200–205` | Runtime errors (folder deletion, rename, or write failures) |

---

## Usage Examples

### Run with default settings

```bat
PurgeTemp.exe
```

### Override the stage root and number of stages

```bat
PurgeTemp.exe --stage-root-folder "D:\MyStaging" --stage-versions 3
```

### Use a custom settings file, then override the delay

```bat
PurgeTemp.exe --settings-file "C:\configs\purge-config.json" --staging-delay-seconds 3600
```

### Disable logging and notifications for a silent, scheduled run

```bat
PurgeTemp.exe --log-enabled false --show-purge-message false
```

### Skip the next run manually

Create a file named `SKIP.txt` (or whatever `SkipTokenFile` is set to) directly inside stage 1. The application will detect it, skip the purge, and exit with code `2`. Delete the file to re-enable purging.

---

## Scheduling

Purge-Temp is designed to be invoked by a scheduler. The built-in `StagingDelaySeconds` guard means it is safe to schedule it more frequently than the intended purge interval — extra invocations simply exit with code `1`.

**Windows Task Scheduler example** (run every hour, effective purge every 6 hours):

1. Create a new Basic Task.
2. Set the trigger to **Daily**, repeat every **1 hour**.
3. Set the action to run `PurgeTemp.exe` with any desired arguments.
4. Configure `StagingDelaySeconds: 21600` (6 hours) in `appsettings.json`.

**Windows Task Scheduler example 2** (run always on user logon):

1. Create a new Basic Task.
2. Set the trigger type to **At log on**.
3. Set the action to run `PurgeTemp.exe` with any desired arguments.
4. Configure `StagingDelaySeconds: 21600` (6 hours) in `appsettings.json`. So Purge-Temp will only be purging files if the last logon was more than 6 hours ago (prevents deletion of files on multiple reboots)

Further usage possibilities:

- A Shell execution in an application
- A shell script executed in the Windows Autostart folder
- Manual execution by CMD / Powershell

---

## Scenarios

### 1. Self-cleaning temporary folder

Point your applications to `<StageRootFolder>\<StageNamePrefix>` as their working temp directory. Purge-Temp will automatically cycle old files out after the configured number of stages, giving a rolling retention window without manual cleanup.

**Recommended settings:**
```jsonc
"StageVersions": 4,
"StagingDelaySeconds": 21600,   // purge every 6 h → ~18 h retention
"RemoveEmptyStageFolders": true
```

### 2. Rolling log archive / cleanup

Use a stage root inside a log output directory. Move or copy log files to stage 1 after each run of your application. Purge-Temp will keep the last N batches and delete older ones automatically.

**Recommended settings:**
```jsonc
"StageVersions": 7,             // keep one week of daily log batches
"StagingDelaySeconds": 86400,   // enforce one purge per day
"LogAllFiles": true             // audit every deleted log in the purge log
```

### 3. Scheduled download / export cleanup

If an automated job writes exports or downloads to a folder, Purge-Temp can ensure that files older than N cycles are removed — providing a configurable retention without date-based logic in the producing application.

**Recommended settings:**
```jsonc
"StageVersions": 3,
"StagingDelaySeconds": 604800,  // weekly purge → 2-week retention
"ShowPurgeMessage": false       // headless server — no notifications needed
```

### 4. Emergency pause via skip token

Place a `SKIP.txt` file in stage 1 before a scheduled task runs to temporarily suspend purging — useful during a deployment window or incident investigation. Remove the file when it is safe to resume.

The name of the skip token file can be configured either by the CLI argument `-k`/ `--skip-token-file` or `SkipTokenFile` in the settings file

### 5. Quiet server deployment

Disable all interactive features for a headless server:

```bat
PurgeTemp.exe --show-purge-message false --log-enabled true --logging-folder "C:\logs\purge"
```

Check the exit code in your wrapper script to detect skipped or failed runs.

---

## License

MIT License — see [http://opensource.org/licenses/MIT](http://opensource.org/licenses/MIT)

Copyright Raphael Stoeckli © 2026
