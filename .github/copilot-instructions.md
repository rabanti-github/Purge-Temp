Purpose
-------
This file gives concise, actionable guidance for AI coding agents (and humans) who need to make productive changes in this repository. Focus on discoverable, reproducible patterns and the small set of locations that matter when implementing or debugging features.

Quick Overview
--------------
- **Solution root:** `PurgeTemp.sln` — open this in Visual Studio on Windows for the smoothest experience.
- **Main app project:** `PurgeTemp/` — contains `Program.cs`, `appsettings.json`, `PurgeTemp.csproj` and folders `Controller/`, `Interface/`, `Logger/`, `Utils/`.
- **Tests:** `PurgeTempTest/` — contains unit tests such as `PurgeTest.cs`, `SettingsTest.cs` and related fixtures.
- **Vendored packages:** `packages/` — many NuGet packages are checked in; restoring packages may not require network if local paths are preserved.

Architecture & Key Flows
------------------------
- The app is a small command-line/utility project. `Program.cs` is the entry point — follow it to understand start-up, configuration loading (reads `appsettings.json`) and the orchestration of controller classes.
- `Controller/` contains the main domain logic (the "what to purge" decisions and orchestration). `Utils/` holds helper functions used across controllers and tests.
- `Logger/` wraps logging configuration and Serilog usage. The repository includes Serilog-related packages under `packages/` — changes to logging should be reflected here and validated by running the app.
- Settings flow: configuration comes from `appsettings.json` and flows into domain types used by controllers. `PurgeTempTest/SettingsTest.cs` demonstrates how settings are parsed and validated — use it as a concrete example when changing configuration shapes.

Build / Test / Debug Workflow
----------------------------
- Recommended dev environment: Visual Studio (Windows) with .NET targeting packs for the referenced framework(s).
- Restore packages (if necessary):

  - Using dotnet (may work for SDK-style projects):

    `dotnet restore PurgeTemp.sln`

  - Using NuGet/MSBuild (for older .NET Framework projects):

    `nuget restore PurgeTemp.sln`
    `msbuild PurgeTemp.sln /t:Build /p:Configuration=Debug`

- Build with: `dotnet build PurgeTemp.sln` or open `PurgeTemp.sln` and build in Visual Studio.
- Run tests: `dotnet test PurgeTemp.sln` (or run tests in Visual Studio Test Explorer). If the test project targets an older framework, use the Visual Studio test runner or `vstest.console.exe`.
- Run the app from repo root (example):

  `dotnet run --project PurgeTemp/PurgeTemp.csproj`

- Debugging: open `PurgeTemp.sln` in Visual Studio, set `PurgeTemp` as the startup project, set breakpoints in `Program.cs` or `Controller/` classes.

Project-Specific Conventions
----------------------------
- Tests live in `PurgeTempTest/` and are named `*Test.cs` or `*Tests.cs` — follow existing naming for new tests.
- Keep changes localized: prefer adding helper methods in `Utils/` or small methods in `Controller/` rather than large API reshapes.
- Logging is done via `Logger/` wrappers and Serilog; change only `Logger/` or config entries in `appsettings.json` unless you intend to adjust logging across the project.
- The repository vendors NuGet packages in `packages/`. Do not remove them without ensuring a reliable external restore source.

Integration Points & External Dependencies
-----------------------------------------
- File system: the app purges temp files — expect many interactions with the file system. Unit tests may mock or create temp files under `PurgeTempTest/`.
- NuGet packages are stored in `packages/` (local copy). If CI or a developer machine lacks these, run `nuget restore` or add a `NuGet.Config` pointing to the local `packages` folder.
- Serilog and related sinks are used (see `packages/Serilog*` and `Logger/`). When changing logging, validate that sinks (Console/File) still initialize correctly.

How to Edit Safely (for AI agents)
----------------------------------
- Start by running the unit tests: `dotnet test PurgeTemp.sln`. Fixes should keep all existing tests green.
- For behavioral changes, add focused unit tests in `PurgeTempTest/` mirroring existing patterns (see `SettingsTest.cs` and `PurgeTest.cs`).
- Preserve public APIs and file layout unless the change explicitly requires refactoring across multiple files. If you do refactor, update tests accordingly.
- When changing configuration shapes, update `appsettings.json` and the tests that parse configuration (e.g., `SettingsTest.cs`).

Files To Inspect First (examples)
---------------------------------
- `PurgeTemp/Program.cs` — application entry and configuration bootstrap.
- `PurgeTemp/appsettings.json` — example/default configuration.
- `PurgeTemp/Controller/` — core purge logic.
- `PurgeTemp/Logger/` — logging initialization and Serilog wiring.
- `PurgeTempTest/SettingsTest.cs` — how configuration is validated in tests.

If Something Is Unclear
-----------------------
- Ask for the desired behavior change and a failing test or reproduction steps. If tests are missing for the area you change, add them.

Quick examples (searchable):
- Where to change startup behavior: `PurgeTemp/Program.cs`.
- Where to change purge logic: files under `PurgeTemp/Controller/`.
- Where to add small helpers: `PurgeTemp/Utils/`.

Feedback
--------
If any section is unclear or you want more detail about CI, runtime targets, or test runner specifics, tell me which area and I'll expand this file.
