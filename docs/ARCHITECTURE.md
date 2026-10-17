# Architecture

## Dependency direction

`CreatorNexus.App` depends on `CreatorNexus.Core`. The core contains platform-independent project models and validation/search rules. Platform-specific UI and local storage stay in the MAUI application project.

## Data ownership

The app writes its project list to `creator-projects.json` in the operating system's app-data directory. This file is separate from any MoodPress or KlaarKompas data. The app identifier is `com.draveinx.creatornexus`.

## Responsive layout

The first dashboard adjusts outer spacing for narrow, medium and wide windows. This is a responsive starting point, not yet a full multi-pane tablet/desktop workspace. A later phase should add a navigation rail and multi-column content layout for wider widths.

## Localization

The first screen includes a runtime Dutch/English toggle. The current implementation localizes dashboard labels only; localization resources and all feature screens should be added before a public release.

## Privacy and network

The phase-one code does not require a user account or cloud service. Project data is local to the app. A future release should add explicit export/restore, integrity checks, deletion controls, and a privacy review before handling creator credentials or external platform tokens.
