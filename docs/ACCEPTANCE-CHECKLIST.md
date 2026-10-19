# CreatorNexus acceptance checklist

This checklist separates CI success from real-device acceptance. A green compile is not proof that every interaction works.

## Current verification boundary

- Automated Android CI: builds and shared core tests must pass for the exact commit under review.
- Automated Windows CI: builds and shared core tests must pass for the exact commit under review.
- Manual acceptance: not complete until each item below is tested on the target device and recorded with device/OS and date.
- Distribution: current CI artifacts are test builds (Android APK/AAB and Windows portable ZIP); do not describe them as store-ready or signed production releases unless the release pipeline proves that.

## Android phone

- [ ] Fresh install opens to the dashboard without a crash.
- [ ] Create, edit, search, favorite, archive, restore, duplicate and delete a project.
- [ ] Schedule a project; confirm scheduled view ordering and local date.
- [ ] Create a project from each built-in template.
- [ ] Edit a checklist, mark the next open item complete, reopen a completed item, and verify progress.
- [ ] Edit a long script, leave and reopen the editor, and test draft recovery after an interrupted edit.
- [ ] Import a media file; confirm it remains available after app restart.
- [ ] Preview and export a thumbnail as PNG and JPG; inspect the resulting files.
- [ ] Export a backup, modify test data, restore the backup, and confirm expected projects return.
- [ ] Test Dutch/English labels, large text, TalkBack, keyboard/focus where applicable, contrast and touch targets.
- [ ] Rotate the device and check for clipped controls or horizontal overflow.

## Android tablet

- [ ] Repeat the phone workflow at tablet resolution in portrait and landscape.
- [ ] Confirm the layout uses the available width and does not render as a narrow phone-sized column.
- [ ] Confirm dialogs, long text fields, file pickers and thumbnail preview remain usable.

## Windows

- [ ] Fresh portable build launches from its extracted folder.
- [ ] Resize the window to narrow and wide sizes; check for clipping and horizontal overflow.
- [ ] Repeat project, calendar, checklist, script, media, thumbnail and backup workflows.
- [ ] Confirm app restart preserves projects and media references.
- [ ] Verify a failed or invalid backup is rejected without replacing valid local data.
- [ ] Test keyboard-only navigation, visible focus and readable scaling.
- [ ] Confirm the artifact can be extracted and run on a clean Windows 11 user profile.

## Thumbnail style library import/export

- [ ] Save a thumbnail style preset, restart the app, and confirm the preset is still available.
- [ ] Export one preset to JSON and verify the file can be selected and imported again.
- [ ] Import a valid style-library JSON file containing multiple presets; confirm all expected styles appear.
- [ ] Export the full style library and verify the exported JSON contains the expected presets.
- [ ] Try malformed JSON and an empty library; confirm the app reports the problem and keeps existing presets.
- [ ] Try a library containing invalid preset values; confirm it is rejected safely.
- [ ] Try duplicate preset names inside one import file; confirm the import is rejected with a clear message.
- [ ] Import presets whose names already exist; cancel the replacement prompt and confirm existing styles remain unchanged.
- [ ] Repeat the name-collision import and approve replacement; confirm the intended styles are replaced.
- [ ] For an import containing both new and existing names, verify the completion message reports the correct number added and replaced.
- [ ] Import a library with only new names; verify the message reports all as added and zero replaced.
- [ ] Import a library with only existing names and approve replacement; verify the message reports zero added and the correct replacement count.
- [ ] Verify that canceling replacement leaves existing styles unchanged and no misleading completion message appears.
- [ ] Verify a rejected/invalid batch does not partially add or replace styles.
- [ ] Verify the export/share flow and file picker on Android phone, Android tablet and Windows.
- [ ] Restart the app after import/replacement and confirm the saved library persists.

## Saved thumbnail style name-collision protection

- [ ] Save a new style with a unique name; confirm it appears in the saved library.
- [ ] Save another style using an existing name with the same casing; confirm a replacement prompt appears before any change.
- [ ] Repeat with different letter casing; confirm name matching is case-insensitive.
- [ ] Cancel the replacement prompt; reload the style library and confirm the original style is unchanged.
- [ ] Approve replacement; reload the style library and confirm the style now contains the new settings.
- [ ] Duplicate a style using an existing name; confirm the app rejects the duplicate name and does not overwrite the original.
- [ ] Confirm collision messages and replacement buttons are correctly localized in Dutch and English on Android phone, Android tablet and Windows.

## Saved thumbnail style search

- [ ] Search styles to load using a partial name; confirm only matching styles are offered.
- [ ] Search with different letter casing; confirm matching is case-insensitive.
- [ ] Leave the search field blank or choose “Show all” / “Alles tonen”; confirm all saved styles are offered.
- [ ] Cancel the search prompt; confirm the action exits without changing anything.
- [ ] Search for a name that does not exist; confirm a clear no-matches message appears and no style is changed or deleted.
- [ ] Repeat partial search for load, delete, duplicate and export; confirm each action only offers matching styles.
- [ ] Confirm Dutch and English labels are correct on Android phone, Android tablet and Windows.

## Record each test

For every checkbox, record: date, app commit SHA, artifact/run URL, device or Windows version, result (pass/fail/not tested), and a short note for failures. A CI pass should never automatically mark manual acceptance as passed.

## Release gate

Do not call a build production-ready until both platform CI runs for the exact commit are green, manual acceptance has been recorded, backup/restore safety passes, and the relevant signing/store or installer requirements are satisfied.
