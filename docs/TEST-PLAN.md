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

## Startup error handling
- When the saved project file is malformed, oversized, or fails validation, launch should show a localized, actionable error instead of an unhandled startup exception.
- Confirm the app explains that the original file is preserved and points to local recovery or portable-backup restore.
- Confirm valid project data still loads normally and no project list is overwritten by the error handler.

## Storage integrity and recovery checks
- Export a JSON backup from the app using the platform share sheet.
- Restore a selected valid JSON backup from the app; verify the project list refreshes.
- Cancel file selection and restore confirmation; verify existing projects remain unchanged.
- Try malformed JSON and duplicate/empty project IDs; verify restore is rejected and existing projects remain unchanged.
- Verify export and restore on a real Android device and a Windows installation, including cross-platform backup transfer.

- Saving writes a complete temporary JSON snapshot before replacing the primary project file.
- When a primary file already exists, preserve its previous contents as a `.bak` file before replacement.
- If saved JSON is malformed, surface a clear data-integrity error rather than treating the library as empty.
- If the saved project file exceeds the 50 MB safety limit, surface a clear data-integrity error without loading or replacing the original file.
- Confirm a failed save leaves the existing primary file intact and temporary files are cleaned up. 
- Verify backup retention and recovery behavior on Android and Windows before release.

- Export the saved project collection to a portable JSON backup file.
- Restore only after parsing and validating the selected backup; reject malformed JSON, null project entries, duplicate/empty project IDs, blank or overlong titles, and undefined status values without replacing existing projects.
- Verify export and restore manually on Android and Windows, including a backup made on one platform and restored on another.

- Automated backup-rule tests reject collections over 10,000 projects and explicit null text fields before restore can replace saved data.
- Backup restore rejects files larger than 50 MB before parsing; automated tests cover the exact limit, a negative length, and the first byte over it.
- Backup export checks the completed temporary file against the same 50 MB limit before replacing the destination, so it never creates an export the restore flow would reject.
- Backup validation explicitly rejects a null project collection; keep this guard covered by automated tests as the backup format evolves.
- Versioned backup exports include a format version, UTC creation timestamp and SHA-256 checksum.
- Restore rejects unsupported versions and modified payloads before replacing current projects.
- Older bare JSON-array backups remain importable for compatibility.
- Missing and malformed checksum values, plus a missing project collection, are rejected.
- Attempt an oversized export and confirm the previous destination file remains unchanged and temporary files are cleaned up.

- The app and automated tests share a single parse-and-validation gate; restore must not call the save/replacement path unless the gate returns a validated project list.


## Project duplication
- Duplicate an active or archived project and verify a new unique ID is generated.
- Confirm script, checklist, notes, tags, media references and thumbnail settings are copied.
- Confirm the duplicate starts as a new draft, is active and unfavorited, and has no scheduled date.
- Verify a 120-character source title is truncated safely to leave room for the localized copy suffix.

## Local recovery from the .bak snapshot
- Create at least two projects so a save produces a previous local `.bak` snapshot.
- Use **Herstel lokale back-up** and confirm; verify the project list is restored and refreshed.
- Verify the local `.bak` remains present after successful recovery; recovery must not copy a possibly damaged primary file over it.
- With no `.bak` file, malformed JSON, duplicate IDs, invalid project fields, or a file over 50 MB, recovery must show an error and must not replace the primary project file.
- Confirm cancel leaves all files and visible projects unchanged.


## Accessibility labels
- With TalkBack or another screen reader, verify icon-only project actions announce meaningful descriptions for status, editing, notes, favorites, scheduling, and delete.
- Confirm the accessible descriptions do not change the visible layout and each action still invokes its original handler.
