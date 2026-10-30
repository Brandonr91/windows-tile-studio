# Code signing policy

## Current status

Windows Tile Studio's published releases through beta.7 are **unsigned**.
SignPath preparation is in progress; no application has been submitted, no
certificate has been granted and no signing service is active. Do not interpret
this policy, a checksum or a passing build as a signature or SignPath endorsement.

If accepted by [SignPath Foundation](https://signpath.org/), the intended
attribution is: **Free code signing provided by [SignPath.io](https://signpath.io/),
certificate by [SignPath Foundation](https://signpath.org/).** This attribution is
conditional until approval. The Foundation would be the certificate publisher;
it would not be a certificate issued in the maintainer's personal name.

## Roles and release approval

- Source maintainer/committer and reviewer: [Brandonr91](https://github.com/Brandonr91).
- Proposed signing approver: [Brandonr91](https://github.com/Brandonr91), subject
  to completing SignPath enrollment and MFA. This is not an assertion that a
  SignPath account or signing role already exists.
- External contributions require maintainer review before entering a signing build.
- Every signing request requires explicit human approval under the configured
  SignPath policy. Automation may submit eligible build artifacts; it must not
  replace the human approver or automatically approve requests.
- All people with repository/signing access must use MFA. Account MFA has not
  been verified by this preparation work.

## Eligible artifacts and origin

Signing is restricted to Windows Tile Studio binaries built from this public
[MIT-licensed repository](https://github.com/Brandonr91/windows-tile-studio).
Builds must run on GitHub-hosted runners, upload their artifacts to GitHub Actions,
and pass SignPath's trusted-build origin verification. Local EXEs, arbitrary
uploaded artifacts and unrelated third-party binaries are not eligible.

Product metadata must identify Windows Tile Studio and match the reviewed release
version. Verify Authenticode chain/status, the approved certificate thumbprint,
timestamp and product metadata before accepting a signed artifact. Generate
package manifests and checksums **after** signing, retain exact source/build
provenance, and require review before release publication. Never overwrite an
earlier published tag or archive merely to add a signature.

Source-hosted PowerShell delivery remains a separate path. Signing one prebuilt
EXE does not authenticate later local source edits or in-memory assemblies.
The repository's current packaging pipeline still builds unsigned archives; a
disabled proof-of-signing workflow template is preparatory, not a signed-release
pipeline or authorization to publish.

## Privacy and installation

The running controller does not upload telemetry or contact an AI/cloud service.
Local settings/logs may contain app paths, display identities and optional saved
window titles; diagnostic and portable exports have separate privacy safeguards.
Repository downloads initiated by an installer or the user contact GitHub, and
code-signing requests by the build pipeline contact SignPath/GitHub. The existing
per-user setup, startup opt-in and owned uninstall/data-retention behavior remain.

This program will not transfer any information to other networked systems unless
specifically requested by the user or the person installing or operating it.
Signing-service account data is governed by [SignPath's privacy policy](https://signpath.io/privacy-policy).

## Requirements reference

Prepared against [Foundation terms](https://signpath.org/terms) and
[GitHub trusted-build documentation](https://docs.signpath.io/trusted-build-systems/github)
on October 30, 2026. Acceptance is discretionary and includes verifiable project
reputation. Enrollment, personal-data/terms consent and every signing approval
must be performed by the maintainer.
