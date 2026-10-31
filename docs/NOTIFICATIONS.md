# Notifications and automated tests

The five warnings reported after beta.6 testing were recoverable application
outcomes. Their exact text appears in the disposable startup/compatibility logs;
the daily installation had no matching warnings and reported recoveryRequired=false.

| Warning | Test trigger / meaning |
| --- | --- |
| Settings recovery; invalid root data | Test writes deliberately invalid XML into its isolated data folder. Defaults run, original bytes stay preserved, Recovery remains available. Real occurrences require reviewing the preserved file/backups, not silently replacing them. |
| Divider reached size limit | Minimum-size boundary blocks further shrinking. Keep the clamp; use another boundary/larger tile. |
| App needs at least N pixels | Oversized fixture reports a minimum larger than the zone. Exact snapping is refused to avoid a false assignment. |
| Size information timeout/access blocked | Slow/blocked fixture or actual Windows privilege/access restriction. Reject safely and retry explicitly after it responds. |
| App refused requested frame | Maximum-size/aspect/frame fixture cannot accept the rectangle. Previous bounds are requested again; no false assignment. |

These are distinct from an unhandled .NET exception or application startup failure.
Do not hide fatal failures or remove sizing/recovery protections to silence tests.

Beta.7 adds per-process **--quiet-notifications** in compiled mode and
**-QuietNotifications** to Start-Hosted.ps1 (forwarded into a background host too).
All native failure-generating controller fixtures opt in. NotificationDelivery
always writes `Notification [category] message` to that controller's **status.log**;
quiet mode bypasses the toast API. Existing placementIssue/live dashboard outcomes,
settings Recovery, original-file preservation and fatal startup handling remain.
Default interactive launches still show notifications. The option is not persisted
or copied into Startup, and does not alter Windows notification preferences.

Runtime aggregates notificationsQuiet/notificationsLogged/notificationPopups let
tests assert that expected failures were logged with zero toast calls. Unit checks
cover both quiet and default delivery; startup/compatibility fixtures verify real
compiled/hosted behavior. Private fixture logs remain under ignored bin/, distinct
from the daily data/status.log. General → Support → Open local log opens the daily
log. Logs are local and may contain private context; no automatic upload is added.

Beta.7 was explicitly deployed/published on October 30; earlier beta.6 downloads remain unchanged. Future tests must use beta.7 or newer source/binaries and the quiet option. Old beta.6 binaries do not recognize the new flag. See [release evidence](RELEASE-BETA-7.md).

## Detailed local resize evidence (local beta.8)

The first failure in each resize gesture is written as **RESIZE_FIRST_FAILURE**
before any baseline restoration. **RESIZE_ABORTED** retains that same immutable
record; subsequent baseline restoration failures use **RESIZE_RESTORE_FAILED**.
**WINDOW_PLACEMENT_FAILED** also records individual rejected placement attempts.
Successful frames do not produce these entries. Existing user warnings, size
constraints, helper cancellation, source detachment and explicit re-snap remain.

Each evidence payload is a single XML element in the existing status.log line.
It includes the gesture ID (correlated with controller run/version/UTC), executable
process name, PID, transient HWND, one-based zone/column/row, physical-pixel
requested and observed visible rectangles (left,top,right,bottom), source zone
and moved edges, pinned height boundary, collected minimum sizes and failure
stage/detail. Zero means an unassigned/unknown zone; unavailable frames are marked
explicitly. Height boundary -1 means no pinned height divider. Query failures
remain timeout-or-access outcomes, not proof of a hung app. Constraint and missing
probe stages identify layout/probe failures, not a proven rejecting application.

No window/document titles, executable paths, network upload or new saved settings
are added. These raw local records contain process/window identifiers and must
stay out of the aggregate diagnostic report and portable layouts. General →
Support → Open local log opens them. Share only after reviewing/redacting context.
A generic user warning can still follow; use the correlated first-failure entry
for the cause. Evidence capture failures are contained and log a fixed type name
under FAILURE_CONTEXT_UNAVAILABLE instead of changing the placement decision.
