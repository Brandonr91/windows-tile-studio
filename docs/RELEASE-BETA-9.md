# Windows Tile Studio 0.3.0-beta.9

Published October 31, 2026 as an **unsigned prerelease**:
[download beta.9](https://github.com/Brandonr91/windows-tile-studio/releases/tag/v0.3.0-beta.9).
Release ID 430015651; published 2026-10-31T15:24:49Z. Frozen clean source
`c220d7fb67a0bb6e95e94c77bbc96c89c929cba4`; annotated tag object
`75e50fa0cf87b28173cab7f96ceaf15908ed574b`. Later documentation commits do not
change this source or the downloads. Never move the tag or replace these assets.
Earlier beta.2, beta.6 and beta.7 tags/assets remain unchanged.

## Frozen assets

| Asset | Bytes | SHA256 |
| --- | ---: | --- |
| Source ZIP | 1112105 | `3802e664ffef96f2d38a122a450998689dee9bdea7768abe7e03079c3faa5e7c` |
| PortableCompiled ZIP | 1359836 | `d503026159ee3e33986bfd40de5591f6f43a2a665a9e7bccf14db7b4eafc3ff7` |
| SHA256SUMS.txt | 226 | `ef39059b6c59d8437f5810fa11b391ce5edbfc459cde33f8667885f73e1b40c2` |
| release-info.json | 1042 | `6dcaa2a01d9b18809b151825f8f2f883a37c97cfba11a0f5ff149edd2e985b24` |

Both archives have verified manifests and sourceRevision above/sourceDirty=false.
Source has 107 ZIP entries and no EXE; PortableCompiled has 108 entries. All four
GitHub server digests matched before publication; four independent anonymous
public downloads matched afterward. Local frozen assets are bin/beta9-final;
verification copies are bin/beta9-public-verification (both ignored).

## Changes since beta.7

- Preserve the first resize failure before rollback: app/zone/source edge,
  requested and actual frames, stage/outcome and gesture correlation in local logs.
- Fresh configurations hide the floating bar; explicit existing visibility and
  historical legacy XML defaults are preserved. Tray/hotkey access remains.
- Startup/reconnect geometry matching preserves saved proportions instead of
  inferring new row ratios from current complementary window frames.
- Direct native API minimize follows the persisted hide-to-tray behavior too.
- Add compiled/source-hosted geometry tests and focused native lifecycle fixtures.

## Verification and limits

Exact frozen source passed [GitHub CI 41214424109](https://github.com/Brandonr91/windows-tile-studio/actions/runs/41214424109).
Exact clean packages passed offline ReleaseTests: manifests, source repeatability,
product/icon metadata, guided setup payload, isolated menu actions, install/update/
rollback, rejected corrupt candidates, owned uninstall/default data preservation,
and synthetic explicit purge. Private result:
TEMP/WtsR9Release/b9a9e4f7a3a74938986efea825e81414. These checks do not launch a
second daily controller or modify real Startup. No new live installation/restart
was performed merely for publication.

Daily deployed source matches all 17 reviewed application files. Compiled geometry
and settings: 199137 checks passed. Source-hosted geometry/settings and offline
installer checks passed during phase-1 preparation. Final-source native lifecycle,
70-transition launcher paint/DPI and focused Compatibility passed compiled and
PowerShell-hosted. Compatibility includes 3/4-row pair-only dividers, width/height
independence, first-failure evidence, min/max/aspect/hung/timeouts and isolated
Chrome. Actual daily restart preserved six window handles/frames, byte-identical
settings and original Startup/menu shortcuts with hidden bar/ready helpers.

The full compiled input-sensitive integration attempt was interrupted at flyout
persistence while external foreground/mouse activity was observed. It is not a
pass, and the full hosted integration suite was not reached. Physical mixed-DPI,
monitor disconnect/docking, work-area changes and sleep/wake acceptance remain
unverified. Geometric recovery can still be ambiguous for overlapping windows;
nonmatching windows require explicit re-snap. No automatic app reattachment,
relaunch, spanning or new constraint solver is introduced.

The desktop remains on the previously tested local beta.9 build. Release packaging
recompiles the same application sources; Framework binaries need not be byte
identical. Publication does not replace the running app or alter preferences.

## Code signing

This prerelease is unsigned. See [code signing policy](CODE-SIGNING-POLICY.md).
No SignPath approval/certificate is claimed. Checksums establish byte integrity,
not a verified publisher identity.
