# Published beta.7 — 2026-10-30

Public unsigned prerelease:
https://github.com/Brandonr91/windows-tile-studio/releases/tag/v0.3.0-beta.7

Release ID 429537038, published 2026-10-30T22:12:33Z. Clean source
**19ea78e3da85aa195eb41eb5ca03b7ff6deca236**, annotated tag object
`edd6493d59bbd0303cc8b712d9c2c937143bc967`, product beta.7/file 0.3.0.15.
Later documentation commits do not change the frozen release source/assets.

| Asset | SHA-256 |
| --- | --- |
| WindowsTileStudio-0.3.0-beta.7-Source.zip | e53886ffa183ebaafebc29403c02afcbf8cd791f98161904e9d769ba6691c596 |
| WindowsTileStudio-0.3.0-beta.7-PortableCompiled.zip | a201104966e6e56ad5a7984aa510cd3f7667c1d3c9eb0bc98b00771b68ba9a2c |
| SHA256SUMS.txt | fa2d7a3c8cb281aaf782dcd8701df77700cce6de77b3a874800c2ca99fc79a5a |
| release-info.json | 139a50a1081a3b3634e22258e7a257d59d3b1d68cd15d9c2a8afa5dcb159f569 |

Source: 1024378 bytes/98 entries/asset 693573342. Compiled: 1268013 bytes/
99 entries/asset 693573421. Sidecars: 226 and 1040 bytes/assets 693573470
and 693573492. Server digests verified before publication; all four public
downloads anonymously fetched and hash-verified afterward. Frozen local assets
are ignored bin/beta7-final; private snapshots and logs are excluded.

The exact source passed GitHub Windows CI
[41118442748](https://github.com/Brandonr91/windows-tile-studio/actions/runs/41118442748),
including 198571 geometry/settings/regression checks, deep build paths and offline
hosting/install/package tests. Local evidence additionally covers compiled/hosted
compatibility, startup Recovery, retained warning logs and zero test popup calls.
Exact clean archives passed ReleaseTests, actual installed Hosted/Compiled/rollback
launch and Setup GUI installation. Source remains EXE-free; manifests and source
ZIP repeatability verify. The published files are distinct from CI artifacts.

## Existing installation

User authorized local deployment followed by publication. Daily bin/SixSlotSnap.exe
now matches the released compiled EXE:
`4c20b75f4c49885eaad1433345b5f3176bee6a06ff2beb55f45eca774be15259`.
Actual process/runtime checks verify beta.7, six identical assigned handles/outer
frames, exact settings bytes and original Startup shortcut bytes, hidden bar,
placementReady=true, no Recovery, two owned helpers and normal notification mode.
Current controller-run log has zero ERROR entries and zero notification popups.
No matching Application Error/.NET Runtime crash appeared in the 30-minute sample.
Hosted delivery mirrors all seventeen sources and Start-Hosted.ps1; data retained.

Private evidence: bin/beta7-deployment.json and bin/beta7-daily-backup; isolated
package fixtures are under temporary WtsR7Final/WtsR7Launch/WtsSetupUi folders.
An initial private verifier raced an atomic runtime-file update; a shared
read/write/delete stream completed verification. This was a verifier read issue,
not an app crash or failed deployment. No application source change was required.

See [notification policy](NOTIFICATIONS.md), [build/log behavior](BUILD-AND-LOG-RELIABILITY.md),
[setup](SETUP.md) and [placement limits](PLACEMENT-RECOVERY.md). Signing remains
pending publisher credentials. Classic file-path bounds, broader physical
DPI/docking/power/privilege/competing-manager, assistive-technology and long-duration
coverage remain limits. Preserve beta.2, beta.6 and beta.7 tags/assets unchanged.
