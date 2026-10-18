# Phase 1 test plan

## Automated tests
- Reject null, blank and whitespace-only project titles.
- Accept a valid title and reject titles longer than 120 characters.
- Search titles case-insensitively.
- Return all projects for an empty search query.

## Manual acceptance checks
1. Install and launch on an Android phone; confirm content fits in portrait and landscape.
2. Install and launch on an Android tablet; confirm wider layout uses available screen space and controls remain usable.
3. Launch on Windows; confirm the window opens, can be resized, and no controls are clipped.
4. Create a project, close and reopen the app, and confirm it remains.
5. Search by title and confirm results update.
6. Advance status through the workflow and confirm it persists after restart.
7. Toggle Dutch/English and confirm dashboard labels change.
8. Check TalkBack/screen-reader labels, keyboard navigation, focus visibility, and text scaling.
9. Inspect app-data isolation and confirm no MoodPress/KlaarKompas path or package ID is referenced.

## Release gate
Do not mark a release ready until automated tests and all applicable platform checks pass in CI and at least one real device per form factor is tested. A successful compile alone is not a substitute for manual acceptance checks.


## Extended search coverage
- Search project title, channel, notes and tags without case sensitivity.
- Search production checklist, script, media references, and thumbnail headline/subtitle.
- Confirm empty search still returns all projects and searches do not modify saved data.


## Storage integrity and recovery checks
- Export a JSON backup from the app using the platform share sheet.
- Restore a selected valid JSON backup from the app; verify the project list refreshes.
- Cancel file selection and restore confirmation; verify existing projects remain unchanged.
- Try malformed JSON and duplicate/empty project IDs; verify restore is rejected and existing projects remain unchanged.
- Verify export and restore on a real Android device and a Windows installation, including cross-platform backup transfer.

- Saving writes a complete temporary JSON snapshot before replacing the primary project file.
- When a primary file already exists, preserve its previous contents as a `.bak` file before replacement.
- If saved JSON is malformed, surface a clear data-integrity error rather than treating the library as empty.
- Confirm a failed save leaves the existing primary file intact and temporary files are cleaned up.
- Verify backup retention and recovery behavior on Android and Windows before release.

- Export the saved project collection to a portable JSON backup file.
- Restore only after parsing and validating the selected backup; reject malformed JSON, null project entries, duplicate/empty project IDs, blank or overlong titles, and undefined status values without replacing existing projects.
- Verify export and restore manually on Android and Windows, including a backup made on one platform and restored on another.
