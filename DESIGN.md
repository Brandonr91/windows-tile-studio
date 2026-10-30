# Manager design system

Local beta.4 implements the direction from the user's supplied dashboard reference:
rounded surfaces, a slim icon rail, quiet borders, spacious grouping, a large visual
preview and an adjacent configuration panel. This uses the existing local WinForms
build and source-hosted delivery path.

## Tokens and composition

- Dark: background #081219, cards #0E1D26, text #F0F6F8, muted #A4BBC6,
  borders #2B414D, selected surface #1B3541. Light keeps soft gray/white surfaces.
- Selection and action accents continue to use the Windows accent. System follows
  Windows app mode. Light/Dark override app mode; high contrast takes priority.
- Main content spacing 24 logical pixels, card gaps 20, card radius 14; action and
  navigation corners 8–10. Segoe UI at 9pt body, 12pt section and 19pt app heading.
- Native resizable frame/titlebar, draft footer, scrollable pages on smaller work
  areas. Existing keyboard and accessible providers remain in place.

Layouts uses a large passive numbered preview, thumbnail list and grouped settings
inspector. Edit visually opens the existing interactive divider editor. The preview
shows the selected draft geometry, not live app occupancy. Its accessible summary
states columns/zones and how to edit. The precise numeric fallback remains available.

General and Share layouts use matching rounded surfaces. Hotkeys keeps its native
accessible grid inside a card. Appearance adds clickable System/Light/Dark preview
cards synchronized with the existing selector and draft. All selection actions are
real and retain Save/Cancel; opening Appearance does not save a preference.

## Implementation and validation

StudioPanel, StudioDrawing, LayoutOverview and ThemeChoice live in UiSupport.cs.
ConfigureStudioLayout/StudioWrap in GeneralSettings.cs rearrange existing controls
without replacing the draft model or native tiling engine. LayoutOverview derives
rectangles from the same LayoutModel, and the native ListBox draws layout thumbnails.
Paint resources are disposed locally; fonts remain alive for their controls.

Compiled geometry, AccessibilityUi, WorkspaceUi, EditorUi and LauncherUi pass.
Source-hosted AccessibilityUi passes on retry after an initial keyboard focus
failure; source-hosted EditorUi, WorkspaceUi and LauncherUi pass. New tests exercise
clickable appearance cards, selector synchronization, accessible selected feedback
and overview rendering at small/normal/large sizes, including 16 zones. Light/Dark
screens were visually inspected. See HANDOFF for native integration/deployment
results and remaining manual hardware/accessibility coverage.

Published beta.2 remains unchanged. Recovery hardening and a live occupancy/status
overview are separate future work; this change supplies the visual composition.

Final native checks: Compatibility and focused StartupRecovery pass. Fixed and regression-tested initialization-only dirty notifications from control reparenting. Full Integration remains incomplete after external input dismissed the flyout; see HANDOFF. Daily beta.4 retained six window assignments/bounds and original startup bytes.

## Beta.6 expansion

Live windows is the running-manager default: display selector, slot cards with local app process labels/status, last outcome and explicit Retry. It is separate from layout drafts. Layouts embeds the divider canvas as the primary control; Advanced reveals numeric weights. Larger modal editing remains reachable. Rounded canvas zones and shared hover/pressed/selected buttons extend to flyout, editor and workspaces. See docs/LIVE-MANAGER.md.
