# Changelog

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
