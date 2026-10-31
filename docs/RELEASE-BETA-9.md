# Windows Tile Studio 0.3.0-beta.9

Unsigned prerelease preparation, authorized October 31, 2026. Exact frozen source,
ZIP hashes and public verification will be recorded after packaging/publication.
Do not overwrite older beta.2, beta.6 or beta.7 tags/assets.

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
