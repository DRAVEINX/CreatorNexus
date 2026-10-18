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

## Record each test

For every checkbox, record: date, app commit SHA, artifact/run URL, device or Windows version, result (pass/fail/not tested), and a short note for failures. A CI pass should never automatically mark manual acceptance as passed.

## Release gate

Do not call a build production-ready until both platform CI runs for the exact commit are green, manual acceptance has been recorded, backup/restore safety passes, and the relevant signing/store or installer requirements are satisfied.
