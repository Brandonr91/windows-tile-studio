# Phase 1: quiet defaults and saved geometry

Local **0.3.0-beta.9**, file version **0.3.0.17**, deployed with explicit user
approval on October 31, 2026. The existing installation now runs this build;
GitHub beta.7 downloads remain unchanged. Nothing was pushed or published.

## Visibility

Fresh settings now hide the floating Layouts bar. The tray menu and configured
flyout shortcut remain available; the selector stays nonactivating. Use tray
**Show Layouts bar**, or General → Layouts bar → Show and Save, to enable it.
Existing explicit true/false preferences are preserved. Older valid XML that
omits ShowLauncherTab keeps the historical visible default. Defaults returned
for a missing/corrupt file are hidden; corrupt-file preservation and Recovery UI
remain in place. No new schema fields, startup registration or migration writes.

The native API minimize path now also hides to tray and persists visibility off;
previously only close and system-command minimize routes did so.

Existing guarded launcher paths remain: restart/settings refresh use the saved
preference, and flyout dismissal only shows a bar whose preference is enabled.
Closing/minimizing a shown bar still hides it to tray and remembers that choice.

## Saved proportions win during geometric recovery

Startup and display reconnection only reconnect normal windows whose visible
frames match a saved slot within the existing physical-pixel tolerance (less
than 16 pixels per edge). Recovery no longer infers new ratios from matching
outer column edges and complementary top/bottom windows. It neither changes
row/column weights nor moves windows to adopt them. More substantial differences
remain unassigned; explicitly snap those windows into their saved zones.

This is ownership recovery, not automatic application reattachment or workspace
restoration. Matching by geometry can still be ambiguous when overlapping
windows fit the same zone. Identity/ambiguity policy belongs to a later phase.
There is no new adopt-current-geometry action in phase 1. Explicit visual editing,
linked resizing and existing reset controls remain the ways to change proportions.

Work-area changes retain their existing same-monitor reflow behavior. Bounded
recovery policies, unavailable placement retention and automatic application
reattachment remain future work. This phase does not claim complete docking or
sleep/wake support.

## Verification

Run without stopping the daily controller:

```powershell
.\Build.ps1 -Test -OutputDirectory bin\phase1-review
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File tests\GridTests.ps1 -WorkDirectory bin\phase1-review\hosted-geometry
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests\LauncherTests.ps1 -WorkDirectory "$env:TEMP\WtsPhase1Launch"
```

Compiled and in-memory PowerShell 5.1 geometry/settings suites each passed
199137 checks. New cases cover fresh/missing settings, explicit true/false,
legacy omitted visibility, no writes on load, and recovery matching across
1–4 rows/columns with negative coordinates, nonuniform saved ratios, physical
rounding, moved windows and wrong row splits. Existing adjacent-pair and width
regressions remain. Offline hosted compilation/install/data-preservation checks
also passed. The added CI hosted geometry step is configured, not remotely run.

Executed after the user authorized local deployment (commands retained for
future approved test intervals):

```powershell
# Only after approval for an interval without the daily controller:
.\Build.ps1 -PhaseOneNative -OutputDirectory bin\phase1-review
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File tests\PhaseOneNativeTests.ps1
.\Build.ps1 -Integration -HostedIntegration -OutputDirectory bin\phase1-review
```

Both compiled and hosted focused native lifecycle fixtures and the 70-transition
launcher paint/DPI suites passed on October 31. Both focused native Compatibility
suites also passed, including independent 3/4-row dividers, rejected resize
first-failure evidence, min/max/aspect/hung apps and isolated Chrome. The full
compiled integration attempt reached its input-sensitive flyout persistence
check but failed while foreground/mouse activity was observed; it is not a pass.
Full hosted integration was not run after that interruption.

The focused fixture refuses an existing controller before creating anything,
uses isolated data, no-recover controller contexts and one disposable window,
and checks hidden startup/restart, saved visible preference, bar close/minimize,
nonactivating selector/dismissal, unrelated settings saves and synthetic display/
work-area/resume messages. Full integration gained an explicit hidden-bar assertion
on display/resume refresh. No-recover fixtures avoid adopting user windows; actual
startup/reconnect geometry recovery with disposable-only windows still needs a
controlled native acceptance interval. Pure recovery tests alone are not proof of the
Windows startup callback sequence. The authorized daily restart did reconnect
the same six actual windows, with identical outer frames and settings bytes,
hidden bar, healthy placement and two owned helpers; no physical topology change
was made. Startup/menu shortcuts were unchanged and beta.8 was backed up.

Physical acceptance remains separate: real restart/re-snap on the installed
candidate; two monitors with different scales/negative origin; resolution and
taskbar work-area changes; unplug/reconnect/docking; sleep/wake. Confirm unchanged
saved weights, hidden visibility, correct per-display ownership and no unrelated
movement. Synthetic messages do not establish these physical results.

## Validation corrections retained

The new fixture originally used asynchronous PostMessage for WM_SETTINGCHANGE;
Windows rejected that post. It now uses the existing integration suite's bounded
SendMessageTimeout route and waits up to five seconds for inventory refresh.
The next check exposed native SW_MINIMIZE bypassing the system-command handler;
LauncherTab now handles WM_SIZE/SIZE_MINIMIZED too. Both modes pass that case.
Failed runs remain local evidence, not successful acceptance. Old daily beta.8
was restarted automatically after each unsuccessful validation attempt.
