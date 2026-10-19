# CreatorNexus

**A free-first creator workspace for Android phones, Android tablets and Windows.**

CreatorNexus is a standalone .NET MAUI app with a dark interface and Dutch/English labels. It keeps creator project data locally on the device.

## Current features

- Create and edit creator projects with workflow status, channel, notes and tags.
- Search project details, including notes and tags.
- Mark favorites; archive, restore and delete projects.
- Schedule content by date and view scheduled projects in date order.
- Start from reusable templates for YouTube videos, short-form videos, tutorials and livestreams.
- Track per-project production checklists.
- Edit thumbnail headline/subtitle and visual options, preview the design, use a custom image, and export PNG/JPG.
- Save reusable thumbnail style presets; search, duplicate, replace, delete, import and export individual presets or the full style library as JSON.
- Validate the saved style library before use and reject malformed, unsupported or duplicate stored styles rather than silently replacing the library.
- Keep script/content notes and media references with a project.
- Export projects to a portable JSON backup and restore a selected backup.
- Validate backup version, checksum, size and project data before replacing saved projects.
- Preserve a previous local project snapshot as a `.bak` file when saving.
- Recover from the local `.bak` snapshot through a guided, confirmation-based action that validates the backup and preserves the recovery source.
- Separate Android and Windows CI workflows with automated domain and backup-validation tests.

## Requirements

- .NET 10 SDK
- .NET MAUI workload (`dotnet workload install maui`)
- Android SDK/JDK for Android targets
- A Windows runner and Windows App SDK workload for Windows targets

## Build and test

Run from the repository root:

```sh
dotnet workload install maui
dotnet restore CreatorNexus.sln
dotnet test tests/CreatorNexus.Core.Tests/CreatorNexus.Core.Tests.csproj --configuration Release
dotnet build src/CreatorNexus.App/CreatorNexus.App.csproj -f net10.0-android --configuration Release
```

Run the Windows build command on a Windows runner:

```sh
dotnet build src/CreatorNexus.App/CreatorNexus.App.csproj -f net10.0-windows10.0.19041.0 --configuration Release
```

## CI downloads

Each successful Android workflow publishes APK/AAB build artifacts. Each successful Windows workflow publishes a portable Windows test-build ZIP. Open **Actions**, select the successful run, and download its artifact from **Artifacts**. Artifacts are retained for 14 days.

**Release note:** CI artifacts are build outputs, not automatically store-ready releases. Android distribution files need the appropriate release signing and must be tested on real devices before publishing. The Windows ZIP is a portable build, not a signed MSI/MSIX installer. Do not treat a successful compile as proof of installation or runtime behavior.

## Data safety

- Backups use a versioned JSON envelope with a SHA-256 content checksum. This detects accidental changes but is **not a digital signature** and does not prove who created a backup.
- Older bare JSON-array backups remain supported.
- Invalid or oversized restore files are rejected before existing projects are replaced.
- If the saved project file is malformed or fails validation, CreatorNexus reports a data-integrity error and preserves the original file rather than silently opening an empty library.
- The thumbnail style library is validated before loading or modifying it. Bulk imports are validated as a batch and written atomically; invalid stored styles or duplicate stored names are rejected so a failed operation does not silently overwrite the original library.
- A previous saved snapshot is retained as `creator-projects.json.bak` when a new snapshot is saved. The guided local recovery action validates the `.bak` snapshot before replacing the primary file and leaves the `.bak` recovery source intact. The action has CI build coverage, but still needs hands-on testing on real devices.

## Separation

CreatorNexus is a standalone solution with app ID `com.draveinx.creatornexus`, its own storage file and workflows. It does not modify MoodPress or KlaarKompas.

## Not yet included / release work remaining

CreatorNexus is still under active development. Features not yet provided as complete production services include user accounts and cloud synchronization across devices, analytics, platform integrations, AI-assisted workflows, release signing/distribution setup, and production release signing/distribution setup. Accessibility, responsive layout, backup transfer, and installation still need hands-on acceptance testing on actual Android phone/tablet hardware and Windows.
