# Published beta.6 — 2026-10-29

Public unsigned prerelease:
https://github.com/Brandonr91/windows-tile-studio/releases/tag/v0.3.0-beta.6

Release ID 428553482, published 2026-10-30T01:14:01Z (October 29 locally).
Annotated tag object `8da13c50bdece76c590ffd0f9406af687269c681` targets clean
source **b052f8377a73105ef8e9d75c9a1400ad02d88a07**, product beta.6/file 0.3.0.14.
Later context documentation does not change this source or the frozen assets.

| Asset | SHA-256 |
| --- | --- |
| WindowsTileStudio-0.3.0-beta.6-Source.zip | 674fb01e39add7953c6350d904f436f561e410f15163a132f74b0cc876b5f01b |
| WindowsTileStudio-0.3.0-beta.6-PortableCompiled.zip | 77066bc74fa5b62209d78603e216abcc2b632790b6d52745fd62702877db3e40 |
| SHA256SUMS.txt | 12ffc1ba106d18e298159bcbd125a82d3aaf4479a31b76043991403a08eacfb3 |
| release-info.json | a3f284d1fae387d3dfd7accb1a8c21ff421119ab35e81266b5404b1998719a03 |

Source: 979165 bytes/92 entries/asset 690482476. PortableCompiled: 1219728
bytes/93 entries/asset 690482515. Sidecars: 226 and 1040 bytes/assets 690482549
and 690482572. Server digests matched before publication; all four anonymous
public downloads independently matched afterward. Archives remain frozen under
ignored bin/beta6-final; private data/snapshots are excluded.

Validation: 198564 geometry/settings checks; compiled and PowerShell5.1 STA
EditorUi, WorkspaceUi, AccessibilityUi, LauncherUi; full native Integration and
HostedIntegration; startup Recovery; both placement recovery suites; native
workspace/live snapshot/retry safety; LauncherTests. Exact clean package passed
ReleaseTests, InstalledLaunchTests Hosted/Compiled/rollback, and SetupUiTests.
Source remains EXE-free, manifests verify and source ZIP repeatability passes.
GitHub Windows CI [40984354491](https://github.com/Brandonr91/windows-tile-studio/actions/runs/40984354491)
passed on exact source b052f83 before publication. CI workflow artifacts are
separate from the frozen release files.

Existing daily bin now uses the release's compiled EXE, SHA-256
7bb0ed4d87a363c47cd6061658ffbe462c2080888bc97054c07d346cb882f4af.
Actual process/runtime verification retained six handles and identical outer
frames, original Startup shortcut bytes, placementReady=true, no Recovery and
hidden bar. Hosted delivery mirrors all sixteen sources and Start-Hosted.ps1.
No sampled app-specific Application Error/.NET Runtime crash event appeared in
the preceding 40-minute local sample. This is not long-duration certification.

See [manager behavior](LIVE-MANAGER.md), [setup/signing limits](SETUP.md) and
[placement containment limits](PLACEMENT-RECOVERY.md). Signing requires publisher
credentials. Broader hardware/DPI/power/privilege/competing-manager, actual assistive
technology and long-duration coverage remain limited. Preserve beta.2 and beta.6
tags/assets; future application changes require a new version.
