## Appearance implementation update, 2026-10-29

The user explicitly requested immediate implementation. Local beta.3 now includes System/Light/Dark, draft previews, shared styling and manager navigation rails. See [appearance](APPEARANCE.md). This supersedes older statements below that these controls are only planned. Recovery hardening and the fuller six-slot overview/simplification remain pending; broader UI/OS validation remains separate. Published beta.2 is unchanged.

# Next milestones after beta.2

User priorities recorded 2026-10-29. These are planned changes, not claims of
implementation or new test results. The user extensively tested the six-slot
layout/dynamic resizing successfully and does not personally use other layouts.

## Scope decisions

- The proposed extra column-height reset (#1) is not a priority: the user reports
  the existing behavior already meets that need. Do not add a duplicate control
  without establishing a concrete missing interaction.
- Saved-proportion conveniences (#2) and fixed app-to-slot assignment (#3) are
  optional for other users, not this user's immediate priority. Current geometry
  persistence and explicit workspace capture/restore already exist; distinguish
  them from any future favorites or assignment rules.
- Recovery (#4) is mandatory; a simpler manager (#5) is wanted; a very modern
  manager interface (#6) is an explicit additional requirement.

## 1. Recovery hardening — first priority

Preserve the six-slot behavior while making failure handling predictable. Closing
one app must release its assignment without disturbing the other five. Refused
frames must not produce false assignments or repeated forcing. A hung target
must not prevent the manager from responding, closing settings or handling tray
actions. Present a clear slot/status explanation and an explicit safe retry.
Do not kill/relaunch third-party apps or silently adopt recreated/ambiguous windows.

Verified existing behavior: bounded size queries, rejection of an already-hung
target, explicit re-snap after response, closed-window release, frame verification
and settings Recovery. The open risk is a target freezing after a successful
size probe, while synchronous placement/restoration is running. Do not mark
this solved by repeating the already-hung-target check.

Exact first step: extend the disposable external-process fixture so it replies to
the size query and then blocks before placement; measure the controller heartbeat
and unrelated-window bounds. Establish the before-fix result before changing
placement. Evaluate bounded/cancellable handling, including recovery after target
resume. Simply abandoning a timed-out worker can leave a queued placement that
moves a window later; late writes, stale handles and obsolete gesture results
must be considered explicitly.

Acceptance: responsive controller under that race; clear outcome/retry; no false
assignment, stale application or late unrelated reflow; preserve all prior pair-only
height, width-height isolation, font/close-to-tray, startup and persistence checks.

## 2. Simplify manager navigation

Make the common six-slot workflow prominent: one main overview of the six zones,
current state and relevant actions. Group custom layouts, portable sharing and
workspace tools under an accessible advanced area. Keep General easy to find.
Do not delete other layouts, migrate preferences silently or hide features so
deeply that keyboard users cannot reach them. Preserve draft Save/Cancel and
separate immediate startup confirmations.

Acceptance: the everyday six-slot task is easier to find and understand; advanced
features remain available; draft cancellation leaves live windows/preferences
unchanged; keyboard paths, accessible names and hidden-bar behavior remain sound.

## 3. Modern manager interface

The user clarified that modern means matching Windows 11's clean visual style,
not an unspecified custom dashboard aesthetic. Provide an Appearance choice:

- System (default): follow Windows light/dark appearance and relevant system/accent
  colors, updating when Windows appearance changes while the app is running.
- Light: an explicit light appearance independent of the Windows light/dark choice.
- Dark: an explicit dark appearance independent of the Windows light/dark choice.

Use clean native-looking navigation/controls, consistent typography, restrained
colors, clear grouping, comfortable spacing and subtle borders/rounded geometry.
Sidebar navigation, the six-slot overview and status/recovery feedback must feel
like one cohesive Windows 11-style application. Define reusable design tokens
before replacing screens; avoid decorative density and inconsistent legacy panels.
Keep titlebar, dialogs and custom surfaces visually consistent where supported.
Retain system high contrast as the accessibility override, readable focus/hover
states, keyboard navigation and scaling on 5K2K/DPI and constrained displays.
Theme preference edits must respect settings Save/Cancel and never reflow managed
windows. Visual matching does not itself select a UI framework or require Mica.

This is the manager interface; preserve the validated resizing behavior and the
nonactivating flyout's targeting/shortcut contract. No framework migration,
embedded web runtime, new dependency or removal of source-hosted delivery has
been selected. Review a concrete visual prototype and implementation tradeoffs
before committing to a new UI stack. The three appearance modes are required
planned functionality, not current implementation. Separate custom text-size
controls have not been requested by this clarification.

Acceptance: approved visual direction, complete everyday and General flows,
consistent components, accessible navigation and DPI/paint regressions checked.
No new version/tag/asset publication follows from this plan alone; beta.2 remains
immutable. Recovery work should lead this sequence.
