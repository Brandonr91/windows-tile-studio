# Appearance and manager navigation

Local 0.3.0-beta.3 implements **General → Appearance**:

- **System — follow Windows** (default for existing and new settings): Windows
  app light/dark preference and DWM accent color, refreshed by user-preference events.
- **Light** and **Dark**: explicit app-mode overrides; accent still follows Windows.
- Windows high contrast overrides all modes.

The manager previews changes within its own draft. Save persists Appearance;
Cancel/discard or a failed save leaves the live preference unchanged. Secondary
managed dialogs inherit the owner's draft; flyout/bar use the saved preference.
Appearance is local settings data, excluded from portable layout export. Older
settings omit it and default to System. Unknown mode values enter preserved-file
Recovery. Older binaries cannot read the new field: keep using the guarded
installer/rollback route rather than directly launching an old EXE on newer data.

The manager uses a left navigation rail, a General category strip, consistent
surfaces, themed grids/inputs, rounded action buttons, native focus/keyboard
behavior and Windows 11 dark-titlebar/rounded-window hints where supported.
Native TabControl selection and accessible Button/ComboBox providers remain;
no new framework, package or web runtime was introduced. Some OS-owned widgets
(scrollbars, numeric arrows, file dialogs and message boxes) retain Windows'
rendering; this is not a migration to WinUI. No Mica dependency is required.

UiTheme owns palette resolution, per-form cached colors, UI-thread event refresh
and subscription cleanup. It does not write Windows appearance settings. Custom
painting retains focus cues and keeps fonts alive through control disposal.

Verification: compiled geometry (198564), AccessibilityUi including three-mode
resolution/clone/persistence/legacy/invalid-value Recovery/draft Cancel/failed Save,
WorkspaceUi, LauncherUi and EditorUi pass. Windows PowerShell 5.1 STA source-hosted
AccessibilityUi, WorkspaceUi and LauncherUi pass. Rendered Light/Dark manager and
Appearance screens were inspected. Live Windows settings changes, Narrator speech
and physical multi-monitor DPI transitions are separate manual coverage.

Microsoft API references: [Win32 light/dark support](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-windows-themes)
and [Windows 11 rounded corners](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners).

This is a local development version. Published beta.2 and its assets stay immutable.
Recovery hardening and the broader six-slot overview redesign remain outstanding.

Native integration follow-up: compiled full Integration passed. Hosted full native tests remain incomplete after two flyout failures; the second explicitly observed outside mouse input/foreground changes. Focused hosted UI suites pass. Final local deployment retained all six handles/bounds and original startup shortcut bytes; no sampled app crash events. See HANDOFF for evidence paths and limits.

Local beta.4 adds clickable theme-preview cards synchronized with the dropdown, rounded General panels and the reference-driven navy/white visual system. Save/Cancel and Windows mode/accent following are unchanged. See ../DESIGN.md and the latest HANDOFF.
