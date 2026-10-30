# Build paths, test records and local logs

Implemented locally in the beta.7 candidate, October 30, 2026. Published beta.6
and the existing daily installation remain unchanged.

## Compiler path handling

The original Framework compiler failure was reproduced with a 218-character
project root: CS1619 reported that it could not create a temporary file.
Build.ps1 now copies application compiler inputs to a short, unique WtsBuild
directory under the current user's temporary folder. Metadata, icon and EXE are
generated there; TEMP/TMP are scoped to that directory and restored in finally.
Cleanup verifies the exact resolved parent/name before removing owned staging.

Only a successful compilation is copied to a unique sibling and atomically
replaces the requested EXE. Compilation/publication failure preserves a previous
executable. An excessively deep final output or temporary base receives a clear
preflight rejection. The change does not enable Windows long paths or promise
arbitrarily long source/output paths; legacy file APIs still impose their limits.
Optional native/test compiler outputs should use a normal-length output directory.

tests/BuildPathTests.ps1 checks deep-path initial/repeat builds, failed compile,
locked-target publication, output/temp rejection and caller environment retention.
Expected failure diagnostics go to files in that disposable test directory. CI
includes this noninteractive regression alongside existing geometry/install tests.

## Integration evidence

Each IntegrationTests invocation creates `<requested-root>/runs/<run-id>`.
Its own run.xml includes suite, mode, product version, source revision/dirty flag,
UTC start/finish and status. Build.ps1 writes test-source.xml beside the test
binary using Git provenance or a release manifest; absent provenance is unknown.
An uncommitted working tree is explicitly marked; it is not a clean-release claim.

The root's atomic latest-run.xml points to the newest run, initially **running**.
Only completed checks and cleanup produce passed/failed. Abrupt interruption
remains running; inspect the process before interpreting it. A bounded file lock
prevents overlapping publications, and an older run finishing cannot replace a
newer run's latest record. Historical legacy files and earlier run folders stay
intact. Never infer current success/failure from an old top-level text file.

LauncherTests and ReleaseTests already create unique GUID folders. This change
addresses IntegrationTests' reused folders; it does not claim every external
tool/test now uses this XML record format. The isolated Chrome fixture uses its
own short WtsChrome profile under TEMP, records that path privately and redirects
stderr to chrome-stderr.log in the run folder. It never uses a real browser profile.

## Runtime logs

Each new status.log line includes UTC timestamp, INFO/WARN/ERROR, a stable outcome
code, product version and a controller-run ID. Runtime state exposes logRun for
local correlation. Newlines in exception/message text are escaped so each entry
retains its own context. Original message text remains searchable; existing logs
are appended, not converted or truncated. Notification codes distinguish settings
Recovery, rejected placement and unavailable resize hooks. Unexpected command,
resize/display/save failures carry ERROR codes and exception details.

Expected test warnings remain log-only with the existing quiet switch. Normal
launches keep their notifications, Recovery and fatal startup dialogs. I/O/access
failure in the logger does not crash the controller or prevent fatal UI feedback.
No logging/telemetry upload, automatic log deletion or diagnostic-export expansion
is introduced. These local logs can contain private context; review before sharing.
