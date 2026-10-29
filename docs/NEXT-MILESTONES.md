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

Design a cohesive manager UI with clear sidebar navigation, a visual six-slot
dashboard, consistent typography/spacing, well-defined focus/hover states and
readable per-zone status/recovery feedback. Define a small reusable design system
before replacing screens. Scale appropriately on the user's 5K2K/DPI setup and
constrained displays; retain system contrast support and keyboard accessibility.

This is the manager interface; preserve the validated resizing behavior and the
nonactivating flyout's targeting/shortcut contract. No framework migration,
embedded web runtime, new dependency or removal of source-hosted delivery has
been selected. Review a concrete visual prototype and implementation tradeoffs
before committing to a new UI stack. User-selectable themes/text sizes remain
planned unless actually implemented and verified.

Acceptance: approved visual direction, complete everyday and General flows,
consistent components, accessible navigation and DPI/paint regressions checked.
No new version/tag/asset publication follows from this plan alone; beta.2 remains
immutable. Recovery work should lead this sequence.
