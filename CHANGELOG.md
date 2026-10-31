# 0.3.0-beta.9 (deployed locally 2026-10-31, unpublished)

- Hide the floating bar for fresh configurations; preserve explicit saved visibility and historical defaults for legacy XML with an omitted field.
- Recover only windows matching saved slot frames, preserving saved proportions instead of adopting current top/bottom splits.
- Handle native API minimization through hide-to-tray as well as the existing close/system-command routes.
- Add compiled/source-hosted geometry/settings and native lifecycle regressions; lifecycle, paint/DPI and focused compatibility checks pass in both delivery modes. Full input-sensitive integration remains incomplete.

# 0.3.0-beta.8 (local, unreleased)

- Preserve the first resize failure before rollback, including affected app/zone, source boundary, physical requested/actual frames and underlying stage.
- Correlate abort/restoration diagnostics in the local log; retain geometry, helper cancellation, explicit re-snap and notification behavior.
- Validate refusing-neighbor evidence and baseline restoration in compiled/source-hosted compatibility tests; raw identifiers stay outside aggregate reports.

# 0.3.0-beta.7 (published 2026-10-30)

- Route automated controller warning notifications to local logs with a per-process quiet switch; normal launches retain popups.
- Keep settings Recovery, sizing protections, live outcomes and fatal startup handling active.
- Compiled/hosted native fixtures assert logged warnings and zero popup calls; default/quiet delivery is regression-tested.
- Stage app compilation in short temporary paths and atomically publish success, preserving prior EXEs on failure; add deep-path CI regression.
- Give integration checks unique run directories and latest status/provenance; retain historical artifacts.
- Add UTC severity/outcome/version/run context to local logs; isolate Chrome test profile/stderr.
- Deployed to the existing installation and published as an unsigned prerelease; previous beta.6 downloads remain unchanged.

# 0.3.0-beta.6

- Live per-display dashboard with occupancy, process labels, disconnected/minimized/maximized states and explicit validated retry.
- Embedded visual editing, one grid undo per gesture, numeric Advanced and larger editor.
- Rounded controls and shared hover/pressed/selection styling across manager, flyout and dialogs.
- Per-user setup GUI, optional stable Start menu shortcut and conflict-safe removal. Startup stays separately opt-in; data retained.
- Includes beta.3/.4 appearance/redesign and beta.5 bounded placement recovery since published beta.2.
- Unsigned prerelease; broader hardware/accessibility/long-duration coverage remains limited.

# Changelog

## 0.3.0-beta.5 — local bounded placement recovery

- Isolate native placement/restoration in owned, kill-on-close helper jobs with
  per-call deadlines, current-user IPC, version/peer/lease checks and explicit retry.
- Preserve healthy windows through a warm spare; detach uncertain assignments.
  Prevent late additional movement in tested query/frame/restoration freeze cases.
- Serialize reentrant geometry callbacks without holding modal UI loops busy.
- Add compiled/hosted external-process freeze, linked resize and parent-exit tests;
  atomically publish the blocked-app fixture's ready marker.
- Already committed target frames can remain changed; broader OS/app behaviors
  and hardware/privilege cases retain their documented limits. Local-only build.


## 0.3.0-beta.4 — local reference-inspired manager

- Rounded cards, icon navigation, large draft layout preview, thumbnail library,
  grouped inspector and clickable appearance previews; native editing retained.
- Fix initialization-only dirty notifications from recomposing the UI; verify
  pristine close and native startup Recovery quit on isolated fixtures.
- Local development build; published beta.2 assets remain unchanged.

## 0.3.0-beta.3 — local appearance controls

- System/Light/Dark with Windows accent following, draft preview, Save/Cancel,
  high-contrast priority and matching supported native titlebar hints.
- Shared palette and manager navigation; compiled/source-hosted delivery retained.

## 0.3.0-beta.2 — settings accessibility labels

- Name the main settings page selector and Hotkeys Change/Clear columns so their
  accessible headers and cells carry action context. Verified from the running
  installation's accessibility tree; shortcuts and layout behavior are unchanged.
- Add accessibility-object regression assertions. Fresh-account testing remains
  waived; manual screen-reader speech and Windows appearance checks remain unverified.

## 0.3.0-beta.1 — local beta validation

- Prevent silent settings loss when selecting an older installed version; probe
  a snapshot before activation/rollback/launch and preserve active pointers/data
  on incompatibility. Current settings enter Recovery on unknown fields/attributes.
- Bound settings XML size/depth and prohibit DTD resolution.
- Add actual public-source upgrade/guarded rollback tests and bounded compiled/
  source-hosted resize/dialog/resource soak coverage.
- Add beta test instructions and a privacy-conscious feedback template. Manual
  hardware/login/accessibility, fresh-account, signing and publication gates remain.

## 0.3.0-preview.8 — local production-delivery preparation

- Versioned source and optional compiled portable ZIPs, per-file SHA-256 manifests
  and archive checksums; deterministic source ZIP layout, reviewed input list.
- Windows Tile Studio executable product/version metadata and owned six-tile icon;
  compatible SixSlotSnap.exe/controller names retained.
- Installer candidate validation before activation; optional expected archive
  hash, cached-source integrity checks and previous-version pointer.
- Guarded rollback/uninstall with data preserved by default; linked-path refusal
  and owned-startup removal. Stable installed-launcher startup for new opt-ins.
- Windows CI configuration for geometry, offline hosting/installer and package
  lifecycle checks. No automated public release or startup enablement.
- Unsigned preview; manual clean-account, assistive-technology, hardware/login,
  elevated/hung-app and external-beta validation remain production gates.

## 0.3.0-preview.7

Keyboard visual-divider editing, accessible selection/value feedback, constrained
resizable/scrolled dialogs, system contrast colors and local aggregate diagnostic
preview/export. See docs/ACCESSIBILITY-AND-SUPPORT.md for verification limits.

## 0.3.0-preview.6.1

General settings for startup/tray, bar visibility/reset/lock, shared flyout shortcut
and local support access. Startup enable/remove remains explicitly confirmed.

## Earlier previews

See docs/POLISH-PHASES.md and HANDOFF.md for phased workspace/startup, portable
layouts/visual editing, displays, compatibility, editing safeguards and tray work.
