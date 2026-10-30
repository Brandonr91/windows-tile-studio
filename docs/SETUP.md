# Setup and distribution

Extract a reviewed Windows Tile Studio release ZIP and run **Setup.cmd**.
Choose a dedicated folder, Compiled or Hosted mode, an optional Start menu shortcut,
and whether to launch. Setup installs for the current account without elevation or
changing login startup. Close Tile Studio before updating. The existing safe
installer verifies/validates a candidate before selecting it and retains settings.

From a release extraction, setup repacks only verified manifest entries into a
temporary archive and deletes that archive after use. Source delivery remains
source-only; selecting Compiled explicitly builds an app EXE. Setup from an ordinary
checkout without a package manifest asks for the downloaded release ZIP. Local
hash verification detects corruption; it is not proof of publisher identity.

The installed folder contains **Start.cmd**, **Rollback.cmd**, and **Uninstall.cmd**.
Rollback verifies compatibility and keeps data. Uninstall requires the app to be
closed, confirms removal, and retains data unless RemoveData is explicitly selected
through Manage-Installation.ps1. Unrelated files/shortcuts are preserved.

Start menu shortcuts point to the stable verified launcher. CLI equivalents:

```powershell
.\Manage-Installation.ps1 -InstallRoot "$env:LOCALAPPDATA\WindowsTileStudio" -Action AddShortcut
.\Manage-Installation.ps1 -InstallRoot "$env:LOCALAPPDATA\WindowsTileStudio" -Action RemoveShortcut
```

These operations respect ShouldProcess/WhatIf. An existing shortcut for a different
command is a conflict, never silently overwritten. Uninstall removes only the
matching installation shortcut. Existing account startup is a separate General
setting; setup does not migrate or enable it.

## Signing remains a publisher decision

This prerelease is **unsigned**. No signing certificate or publisher identity has
been supplied. Do not call checksums, Windows-generated icons or a passing CI build
a signature. A later signed distribution requires a verified publisher certificate
or signing service, protected signing credentials, timestamping, signature checks
and regenerated manifests/checksums after signing. Source-hosted compilation and
its resulting in-memory assemblies are a separate trust path. Do not disable
SmartScreen or Windows security controls to make installation appear trusted.

The installer retains its existing current-user, dedicated-folder model; no MSI,
machine-wide registration, automatic updater or automatic startup is introduced.
Long paths remain subject to the legacy Framework compiler's temporary-path limits.
