# Production release gates

Do not label a preview production-ready solely because local automated checks pass.
Record the artifact hash, source revision, machine/account and evidence for each gate.

## Local automation available

- Geometry/configuration and compiled/source-hosted UI/native regressions.
- Version metadata/icon, explicit package inputs and per-file/archive hashes.
- Repeatable source archives; compiled artifacts have per-build hashes.
- Offline fresh-directory install/repeat/update/rollback and failed update tests.
- Pointer lock rollback, source/binary tamper, unsafe ZIP and linked-path refusal.
- Default data retention, synthetic explicit purge/reinstall, owned/unrelated startup.
- Actual installed Hosted/Compiled launch, rollback launch and running-install refusal.
- Source-only hosting and legacy guarded restoration tests.
- Read-only Windows CI configured; hosted CI execution still needs evidence.
- Beta.1 actual public cfdb2df upgrade with synthetic preferences, blocked lossy
  install/rollback/launch and compatible backup rollback; separate empty workspace
  library retained byte-for-byte. Representative personal captures remain untested.
- Thirty repeated resize cycles plus dialog churn in each mode; bounded GDI/private
  growth and 15-second idle samples. Multi-day physical-input testing remains open.

## Manual/beta gates still open

- [ ] Fresh Windows account or VM: source and portable install, launch, update,
      rollback, uninstall, retained-data reinstall and explicit removal.
- [ ] Upgrade from an actual previously published release with representative
      settings/workspaces and recovery backups; no accidental schema downgrade loss.
- [ ] Actual logout/login for Hosted and Compiled/stable installed startup;
      migration/removal of older version-specific entries and Windows policy behavior.
- [ ] Physical mixed-DPI displays, negative origins, work-area/taskbar changes,
      docking/unplug/reconnect and sleep/resume.
- [ ] Narrator/another screen reader, actual OS contrast/theme/text-size changes,
      keyboard-only operations, small displays and mouse fallback with conflicts.
- [ ] Elevated, inaccessible, fully hung and unusual/max/aspect-restricted apps;
      competing tilers, virtual desktops and sustained native mouse resizing.
- [ ] Longer daily-use CPU/memory/GDI/startup/resize observations and external beta
      feedback, with concrete failure reproduction and no unresolved crash regressions.
- [ ] Signing/publisher strategy and certificate availability; no private signing
      material in source. Unsigned packages remain clearly labeled previews.
- [ ] Final package inventory/checksums/license/screenshots/help/version/release notes
      match the artifact; backups and rollback instructions were exercised.
- [ ] Reconcile local/public histories without force push; preserve six-slot-snap.
      User explicitly authorizes the intended push/tag/release after review.

## Scope decisions to resolve before claiming all optional polish

Separate user-selectable light/dark themes and text-size controls are not implemented;
current support uses Windows controls, DPI scaling and system high contrast. Broader
app session/document recreation, arbitrary freeform overlapping zones and universal
app compatibility are also not implemented. Do not imply those capabilities in a
release listing. Keep the user's original adjacent-pair height and local-only scope.
