# Roadmap after Phase 1

## Phase 2 — Creator workspace
- Project edit/archive/delete with confirmation and undo where possible
- Idea capture, tags, favorites, filters and project templates
- Workflow checklists and content calendar
- Search across project notes and metadata

## Phase 3 — Production tools
- Script editor with sections and version history
- Brand kits per channel
- Thumbnail canvas with text, shapes, layers and image export
- Media library with file references and missing-file checks

## Phase 4 — Reliability
- Versioned backup/restore format with manifest and integrity checks
- Import/export and migration tests
- Accessibility and localization pass across all screens
- UI automation and performance checks

## Phase 5 — Optional integrations
- Creator metrics entered manually first; platform APIs only with explicit authorization
- Optional AI provider adapters with clear privacy disclosures and no mandatory paid service
- Optional cloud sync only after conflict resolution, encryption, account security and deletion behavior are designed

The roadmap items above are planned work, not features already present in the Phase 1 code.


## Implemented in the current starter
- Project title/channel and notes editing.
- Archive, restore and confirmed delete.
- Favorites, tags and tag-aware search.
- Scheduled dates and a date-ordered content view.
- Starter templates for YouTube videos, short-form videos, tutorials and livestreams.
- Editable per-project production checklists.

These features still require successful CI runs and hands-on Android phone, Android tablet and Windows testing before being considered release-ready.

- Multiline script studio for hooks, scenes, dialogue and calls to action.
- Per-project media reference list for filenames, locations and URLs (references only; files are not uploaded or copied).

- Thumbnail designer with live preview, headline, subtitle, color themes, user-selected background image, left/center/right text alignment and PNG/JPG export at 1280×720.

- Native file picker to import media into app-private CreatorNexus storage and save file references per project. Media-library deletion and broader media preview remain follow-up work.

- Thumbnail-editor enhancement: persisted small/medium/large headline and subtitle sizes, plus centered, top-banner and bottom-banner layouts, applied to preview and PNG/JPG export.

- Thumbnail-editor enhancement: selectable white/yellow/black text color, persisted per project and reflected in preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: selectable no/black/white text outline, saved per project and applied to preview and PNG/JPG exports.
- Thumbnail-editor enhancement: optional text background panel with None, Dark, Light and Theme styles; saved per project and applied to live preview and PNG/JPG exports.

- Thumbnail-editor enhancement: independently selectable Top, Center or Bottom text placement, saved per project and applied to preview and PNG/JPG exports.

- Thumbnail-editor enhancement: independently adjustable text-outline thickness (Thin, Medium, Thick), saved per project and applied to live preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: selectable Arial, Georgia or Courier New font family, persisted per project and used in the live preview and PNG/JPG exports.

- Thumbnail-editor enhancement: selectable text-panel opacity (Subtle, Medium, Strong), persisted per project and applied to preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: configurable text-panel padding (Compact, Normal, Spacious), saved per project and reflected in the live preview and PNG/JPG exports.

- Thumbnail-editor enhancement: configurable text-panel corners (Square, Soft, Rounded), saved per project and reflected in preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: optional text-panel border (None, Subtle, Strong), persisted per project and reflected in the live preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: independently selectable text-panel border color (White, Black, Theme), saved per project and applied to live preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: independently configurable text-panel border thickness (Thin, Medium, Thick), persisted per project and reflected in the live preview plus PNG/JPG exports.

- Thumbnail-editor enhancement: independently configurable text-panel border opacity (Subtle, Medium, Strong), saved per project and applied in live preview plus PNG/JPG exports.


## Reliability follow-up
- Project storage now preserves the previous valid file as an automatic `.bak` before replacing the primary JSON file.
- Malformed project JSON raises a data-integrity error instead of silently returning an empty project list.
- Full user-facing backup/restore, backup validation and migration tests remain planned work.
