# Live manager and visual editing

Implemented in 0.3.0-beta.6. The running manager opens on **Live windows**.
Choose a display to see its current layout and occupied, empty, minimized,
maximized, nonresponding or disconnected zones. App process names appear locally;
window titles are not collected for this page. Refresh runs once per second only
while the page is visible; Refresh status also requests a snapshot. The dropdown
keeps its selection and pauses refresh while open. No new background service,
telemetry, settings schema or persisted window inventory is introduced.

After a failed snap or source resize, **Retry last placement** offers an explicit
retry of the last known window/zone. A transient token, window property, process
start time, PID/thread and current layout/display/occupancy checks reject stale
targets and prevent taking another occupied slot. Closing/recreating the app or
changing its display/layout requires an explicit new snap. Retry never launches
an app. Windows can deny the property/access needed for retry; the flyout remains
the manual path. Retry is immediate and separate from draft Save/Cancel.

**Layouts** now embeds the divider editor. Drag a vertical divider for widths;
drag a horizontal divider for the adjacent two rows in that column. Page Up/Down
selects a divider; arrows adjust 1%, Shift+arrows 0.1%, Ctrl+Z/Y undo/redo, Escape
cancels an active drag. Undo grid change restores a complete pointer gesture.
Open larger editor retains the modal editing workflow. Numeric weights are under
**Advanced: numeric weights**. Save changes applies drafts; Cancel discards them.
Live status uses the controller's saved/session geometry, independently of drafts.

Rounded previews, flyout zone buttons, editor/workspace actions and shared hover,
pressed and selected states follow System/Light/Dark and Windows accent. High
contrast and native accessibility remain authoritative. Small windows scroll;
OS titlebars, menus and file/message dialogs retain Windows rendering.

The dashboard reports current assignments, not durable app/document restoration.
It does not apply layouts by clicking an empty tile, automatically repair an app,
adopt a recreated window, or reset all windows sharing a horizontal line.
