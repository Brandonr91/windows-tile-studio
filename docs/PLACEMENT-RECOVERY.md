# Bounded placement and freeze recovery

Local beta.5 addresses an app freezing after a successful size query, during a
position change, or during attempted restoration. It preserves the existing
geometry/assignment model and requires explicit re-snap after a failed operation.

## Reproduced baseline

A disposable external WinForms app responds to WM_GETMINMAXINFO using ReplyMessage
and then waits on a native event with a 15-second rescue deadline. Beta.4 cannot
open settings through its queued controller command while SetWindowPos is blocked.
After the app resumes, the old placement arrives. The other five windows stay put.
A sent-message ping alone was insufficient evidence: Windows can dispatch incoming
sent messages while blocked in a native call. The regression uses an actual queued
settings command and a visible, closable settings window.

The retained pre-change daily beta.4 EXE has SHA256
61c27e5272130946efa64b6e7e01cd25659f2b6b8d168febec875a805112655f.
Private baseline fixtures/results remain under ignored bin/recovery-*.

## Implemented containment

PlacementBroker owns two hidden local helpers: one active and one warm spare so
healthy frames can be restored promptly if a target blocks. They are the same EXE
in compiled mode; hosted mode starts Windows PowerShell 5.1 and compiles the same
source in memory. There is no second tray/controller, network service or app EXE
written by hosted mode. Startup warms helpers with a bounded wait; unavailable
helpers produce retry guidance, without falling back to unbounded in-process calls.

Each helper belongs to its own kill-on-close Windows job. A local named pipe is
restricted to the current account, both ends validate the other process's actual
PID, and protocol/product-version handshakes reject mismatches. Requests are fixed
size and serial. A per-request window property, PID and GUI-thread check reject
stale ownership. The temporary property is removed when the operation ends.

SetWindowPos and external window-state restoration run only in a helper with a
250ms IPC/native-operation deadline. On timeout the manager closes the owned job,
terminates/verifies that helper's exit, releases uncertain assignment and requires
explicit retry. It does not enqueue SWP_ASYNCWINDOWPOS requests, abandon a blocked
thread inside the controller, kill the user's app, auto-adopt or relaunch it.
The spare serves subsequent safe operations; replacement warming queues no window
movement. If helper exit cannot be confirmed, further placement pauses. Job handles
also close when the manager process terminates, preventing orphaned helper calls.
Window-placement/frame verification and restoration still use physical DPI pixels.

Runtime's placementReady is a current helper-availability boolean. The settings
schema and portable layout format are unchanged; pipe names, worker IDs and leases
are not saved in preferences or diagnostic exports.

## Scope and verification

Tests cover post-query, pre-frame, post-frame and restoration freezes; settings
open/close; no false assignment; explicit retry; unchanged unrelated windows;
linked resize rollback; normal helper shutdown; abrupt parent exit and no delayed
move. The post-frame/restoration cases distinguish a frame already committed
before the timeout from any additional move arriving after resume. Windows cannot
undo application code already executed; a failed attempt can leave that target at
its current frame. It is detached for explicit retry. Application-owned deferred
behavior and OS/DWM failures are outside this cancellation guarantee.

Commands (stop the daily controller first):

- Build.ps1 -PlacementRecovery
- Build.ps1 -HostedPlacementRecovery
- Build.ps1 -Integration -HostedIntegration
- Build.ps1 -WorkspaceIntegration -StartupRecovery

The new fixture's --baseline mode must use a retained pre-fix EXE. Its
--cancel-experiment mode separately tests sender-process cancellation with a
native wait in the disposable target. It never kills a real app. Helper discovery
filters live owned application/PowerShell children, excluding transient framework
compiler processes. Full results and deployment state are recorded in HANDOFF.

The 250ms deadline is per placement call; startup, helper cleanup (up to 500ms),
UI scheduling, batches and parent-side frame reads have separate costs. This does
not assert a universal whole-gesture latency or immunity to every Windows API hang.
Privileged apps, physical multi-monitor/DPI/power changes and competing tilers
still need broader manual testing. Two helper processes add local memory overhead.

## Windows API evidence

[SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)
posts work when SWP_ASYNCWINDOWPOS is used, rather than proving it completed.
[SendMessageTimeout completion](https://devblogs.microsoft.com/oldnewthing/20110915-00/?p=9643)
distinguishes an undelivered message from an already executing handler. These
semantics motivated process containment rather than a timed-out abandoned thread.
See [job limits](https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-setinformationjobobject),
[extended job information](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information),
[Framework pipe constructors](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.namedpipeserverstream.-ctor?view=netframework-4.8.1)
and [window properties](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setpropw).

STA serialization: native waits can deliver WinEvent callbacks reentrantly. The controller holds a geometry transaction, posts callback replay after completion and skips nested timer work; modal settings/workspace message loops remain available. Public synchronous placement/save/restore calls return a retry outcome while busy. The broker defensively rejects overlapping calls. Compatibility now checks that no such internal reentry error was logged.

Final local verification: both full integration suites, compiled/hosted UI suites, all freeze/linked/parent-exit cases, native workspace/startup, offline release and installed launch/rollback checks pass. Original six daily handles/bounds and Startup shortcut bytes are unchanged after beta.5 deployment. Measured helper private memory at startup was about 37MiB combined in compiled mode and 167MiB in hosted mode; a two-second compiled idle sample recorded 0 CPU milliseconds for main/helpers. These are local bounded samples. Deep legacy compiler staging paths failed; short isolated test paths passed. See HANDOFF for exact evidence and outstanding manual scope.
