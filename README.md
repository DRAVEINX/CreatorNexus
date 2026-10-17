# CreatorNexus

**A free-first creator workspace for Android phones, Android tablets and Windows.**

Phase 1 starter source package. It includes a dark dashboard, Dutch/English UI toggle, local project creation, listing, status updates, search, shared domain rules, unit tests, and separate Android/Windows GitHub Actions workflows.

## Requirements
- .NET 10 SDK
- .NET MAUI workload (dotnet workload install maui)
- Android SDK/JDK for Android targets
- Windows runner and Windows App SDK workload for Windows targets

## Build and test
Run these commands from the repository root:
- dotnet workload install maui
- dotnet restore CreatorNexus.sln
- dotnet test tests/CreatorNexus.Core.Tests/CreatorNexus.Core.Tests.csproj --configuration Release
- dotnet build src/CreatorNexus.App/CreatorNexus.App.csproj -f net10.0-android --configuration Release

Run the Windows build command on a Windows runner:
- dotnet build src/CreatorNexus.App/CreatorNexus.App.csproj -f net10.0-windows10.0.19041.0 --configuration Release

## CI downloads
Each successful Android workflow publishes a downloadable APK/AAB test artifact. Each successful Windows workflow publishes a portable Windows test-build ZIP. Open **Actions**, select the successful run, and download its artifact from the **Artifacts** section. These artifacts are retained for 14 days.

**Release-signing note:** CI artifacts are build/test outputs, not yet store-ready releases. Android APK/AAB files need signing with your private release keystore before distribution or upload to Google Play. The Windows ZIP is an unpackaged portable build, not a signed MSIX installer. Do not publish either as a public release until signing, installation, and device testing are complete.

## Separation
This is a standalone solution with its own app ID (com.draveinx.creatornexus), storage file and workflows. It contains no references to MoodPress or KlaarKompas and does not modify them.

## Phase 1 limitations
This is a starter implementation, not a store-ready release. Advanced modules such as a full thumbnail editor, script studio, media library, backup UI, analytics, cloud sync, platform APIs and AI are planned, not yet implemented.
