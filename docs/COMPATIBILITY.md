# Compatibility checkpoint: phase 3

2026-10-27, local `0.3.0-preview.3`. These are representative checks, not a
claim that all Windows applications or display configurations are supported.
The original six-slot repository and existing browser profiles are unchanged.

## Implemented behavior

- Check reported app minimum sizes before snapping; verify the visible frame
  afterward. Do not record failed placement as a successful assignment.
- Constrain shared column widths using the reported minimums of assigned rows;
  constrain height using only the two adjacent windows. Keep a gesture pinned
  to its first observed height boundary, including while probes are pending.
- Run gesture size probes in the background with physical-pixel DPI context,
  50 ms per-message timeout and cancellation checks between windows. UI events
  retain the original frame and latest cumulative delta until results arrive.
- Preserve the source connection when a no-op drag reaches an existing limit.
  On unexpected rejection, request baseline frames and stop rather than repeatedly
  force unsupported bounds. Feedback tells the user when a re-snap is needed.
- Skip minimized/maximized neighbors, release closed windows through lifecycle
  events, and require an explicit snap for restored/recreated windows.

## Verification matrix

| Case | Recorded result |
| --- | --- |
| Existing six-window linked widths/independent heights, stale ratios, simulated native grouping | Hosted suite PASS. |
| Oversized app | Rejected without assignment or changed original frame. |
| Delayed WM_GETMINMAXINFO response | Timed out without moving/assigning the window; actionable feedback recorded. |
| Reported neighbor width/height minimums | Dividers clamp while uninvolved heights remain unchanged; impossible-pair math leaves the model unchanged. |
| New gesture already at a size limit | Original frames and source assignment preserved. |
| Two columns with three rows | All internal divider sides PASS; opposite-edge movement during one gesture cannot reach a third row. |
| Two columns with four rows | All internal divider sides PASS, including borderless and fixed-tool frames. |
| Twenty scripted corner updates | Pair-only heights retained; no growing controller GDI-object count. |
| Minimize/maximize and explicit restore/re-snap | Inactive neighbors not forced; restored windows reconnect explicitly. |
| Close and recreate | Closed assignment released; recreated window remains unassigned until explicitly snapped. |
| Chrome app window | Isolated profile, about:blank, four-row tile, visible-frame verification and close PASS; existing Chrome windows were not moved. |
| Settings/recovery/keyboard from phases 1–2 | Full hosted regression suite PASS. Startup recovery subcases use a compiled controller. |

`Build.ps1 -Test`: PASS, 186177 geometry/configuration checks.
`Build.ps1 -Compatibility`: focused desktop suite, daily controller must be stopped.
`Build.ps1 -HostedIntegration`: full hosted suite including compiled compatibility
and recovery subcases. Chrome check is skipped explicitly if Chrome is unavailable.
Tests wait for resize ownership/completion rather than assuming event delivery
within a guessed delay. Grouped-neighbor duplicate starts remain intentionally raw.

Latest full-suite sample: 20 corner updates in 1487 ms including deliberate
message-pump delays and settling, controller CPU 47 ms; one-second idle sample
16 ms CPU. Focused samples also passed. CPU accounting has finite resolution;
short samples are not performance guarantees or evidence of zero idle cost.
Longer warm/cold benchmarks and broader beta coverage remain release work.

## Remaining limits

- Elevated apps were not manually tested. Access can be denied; matching
  privileges may be required. The app does not automatically elevate itself.
- A delayed sizing reply is not a completely hung app. Sizing queries have a
  timeout; synchronous SetWindowPos/DWM operations do not have an overall hard
  deadline. A target becoming unresponsive after a successful probe remains a
  compatibility risk. Restoration requests may also be rejected.
- Advertised minimums are advisory. Maximum sizes, fixed aspect ratios, unusual
  frame behavior and arbitrary app-specific restrictions are checked through
  actual placement, not exhaustively negotiated ahead of time.
- Native mouse timing, competing tilers, irregular app frames, very fast separate
  gestures and recreated handles need broader real-world testing. This suite
  uses controlled notifications and a small app sample.
- One work area is still active at a time. Mixed DPI across monitors, monitor
  reconnection and sleep/resume recovery belong to phase 4.

## Windows API evidence

The implementation follows the documented [WM_GETMINMAXINFO message](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-getminmaxinfo)
and [MINMAXINFO tracking-size fields](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-minmaxinfo).
[SendMessageTimeout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendmessagetimeoutw)
documents system-message marshalling, return-status semantics and shared-queue
timeout limitations. [SetThreadDpiAwarenessContext](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setthreaddpiawarenesscontext)
returns the previous thread context for restoration. Background workers must
set their own context; the UI thread's setting does not configure them.
