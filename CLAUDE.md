# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Purpose

SnapSiphon processes Snapchat "My Data" exports (memories, metadata) for users who want to self-host their media. The target audience is non-technical end users (friends, family, coworkers), so UX clarity matters. The app is not a web service — it runs locally against a folder of exported Snapchat data.

## Build & Run

Primary development target is desktop:

```powershell
# Build
dotnet build SnapSiphon.slnx

# Run (desktop)
dotnet run --project SnapSiphon.Desktop

# Run with hot reload
dotnet watch --project SnapSiphon.Desktop run

# Build release
dotnet build SnapSiphon.slnx -c Release
```

There are no tests yet. When tests are added, the convention will be `dotnet test`.

## Solution Structure

Five projects in `SnapSiphon.slnx`:

| Project | Role |
|---|---|
| `SnapSiphon/` | Shared UI library — all views, viewmodels, and business logic live here |
| `SnapSiphon.Desktop/` | Desktop entry point (Windows/macOS/Linux); thin — just `Program.cs` |
| `SnapSiphon.Android/` | Android entry point |
| `SnapSiphon.iOS/` | iOS entry point |
| `SnapSiphon.Browser/` | WebAssembly entry point |

All platform entry points reference `SnapSiphon/` and add nothing beyond bootstrapping. New features go in `SnapSiphon/` only.

## Architecture

**MVVM via CommunityToolkit.Mvvm.** ViewModels inherit `ViewModelBase` (which extends `ObservableObject`). Use `[ObservableProperty]` on private backing fields to generate public properties with change notification.

**ViewLocator convention:** `App.axaml` registers a `ViewLocator` that resolves views from viewmodels by replacing `"ViewModel"` with `"View"` in the fully-qualified type name. To wire a new view: create `Views/FooView.axaml` + `ViewModels/FooViewModel.cs` — they connect automatically.

**Desktop vs mobile layout:** `MainWindow` (desktop) wraps `MainView` (shared `UserControl`). Mobile and browser platforms use `MainView` directly via `ISingleViewApplicationLifetime`. Avoid putting logic in `MainWindow`; keep everything in `MainView` and its viewmodel so it works across all platforms.

**Compiled bindings** are enabled project-wide (`AvaloniaUseCompiledBindingsByDefault=true`). Every AXAML file must declare `x:DataType` to get compile-time binding checks. Don't use `{Binding}` without a data type — it will warn or fail at compile time.

**Package versions** are centrally managed in `Directory.Packages.props`. Do not specify `Version=` in individual `.csproj` files; add or update versions only in `Directory.Packages.props`. All Avalonia packages must stay on the same version.

## Key Packages

- **Avalonia 12.0.3** + FluentTheme + Inter font — cross-platform UI
- **CommunityToolkit.Mvvm 8.4.0** — MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **TagLibSharp 2.2.0** — lossless metadata read/write for JPEG, PNG, and MP4 (GPS EXIF + QuickTime `©xyz`)
- **AvaloniaUI.DiagnosticsSupport** — debug-only DevTools overlay (excluded from Release builds)

## Processing Pipeline

`SnapSiphon/Services/` contains the full pipeline, called from `MainViewModel.StartProcessing`:

1. **`SnapchatDiscoveryService`** — finds `mydata~*` dirs, extracts any `.zip` archives, locates `memories_history.json`
2. **`MemoriesJsonParser`** — deserializes `memories_history.json` into `List<MemoryEntry>` (Date UTC, MediaType, Latitude, Longitude)
3. **`FileDiscoveryService`** — walks all `memories/`, `chat_media/`, `shared_story/` subfolders; returns `DiscoveredFile` lists. Skips `-overlay` files.
4. **`MemoryMatchingService`** — correlates memory files to JSON entries by grouping on UTC date (from filename prefix) and pairing by sorted order within each day
5. **`MetadataWriterService`** — sets `LastWriteTime` and writes GPS: JPEG/PNG via `TagLib.Image.File.ImageTag`, MP4 via `AppleTag.SetText(©xyz key, ISO 6709 string)`
6. **`MediaProcessingService`** — orchestrates all steps; copies files to `{inputRoot}/{outputFolderName}/{memories|chat_media|shared_story}/`

## Snapchat Data Format

Seven ZIPs extracted to `mydata~{id}` through `mydata~{id}-7`. Only the first directory contains `json/memories_history.json` (3,447 entries, newest-first). Memory files live in `memories/` across directories 2–7, named `{YYYY-MM-DD}_{UUID}-(main|overlay).{ext}`. The `CreationTime` on files holds the correct original timestamp; `LastWriteTime` must be fixed from JSON. All JSON `Download Link` fields are empty (URLs expired) — processing is purely local. GPS coordinates are present on every entry.
