# SignPath application preparation

**Draft only. Not submitted. No provider account, certificate, MFA enrollment or
GitHub App authorization was created.** Apply at https://signpath.org/apply.
The form was inspected on October 30, 2026. It requires contact details, agreement
to Foundation terms and consent to personal-data processing. The maintainer must
enter/approve those fields directly. Optional marketing consent is separate.

## Ready-to-copy project fields

| Field | Draft value |
| --- | --- |
| Project Name | Windows Tile Studio |
| Repository URL | https://github.com/Brandonr91/windows-tile-studio |
| Homepage URL | https://github.com/Brandonr91/windows-tile-studio |
| Download URL | https://github.com/Brandonr91/windows-tile-studio/releases |
| Privacy Policy URL | https://github.com/Brandonr91/windows-tile-studio/blob/main/docs/CODE-SIGNING-POLICY.md#privacy-and-installation |
| Category | Productivity |
| Maintainer Type | Individual maintainer(s) |
| Build System | GitHub Actions |
| Primary Discovery Channel | AI / LLM tools |
| Exact source (optional) | Codex / ChatGPT, using the official SignPath Foundation documentation |
| Wikipedia URL | Leave empty; none is claimed |
| First Name / Last Name / Email | Enter your own account/contact details directly in SignPath |
| Company Name | Leave empty unless you actually represent a company |

**Tagline:** Custom Windows window layouts with connected resizing and independent
row dividers.

**Description:** Windows Tile Studio is a local Windows desktop utility for
arranging application windows into configurable layouts. It provides a visual
layout selector, customizable hotkeys, direct divider editing, live slot status
and connected resizing. Each column keeps its own row dividers so changing one
pair of window heights does not resize every window across the screen. It is an
open-source project with no runtime cloud or AI dependency.

**Reputation — honest draft:** This is a new individual-maintainer project,
created publicly on October 26, 2026. The repository provides MIT-licensed source,
released source/compiled packages, checksum/provenance records and passing Windows
CI. The maintainer reports extensive use of its six-slot layout and linked
resizing. We do not claim widespread adoption, independent audits, press coverage
or established community reputation. Public release download counters include
our own verification traffic and must not be presented as user counts. Evidence:
[repository](https://github.com/Brandonr91/windows-tile-studio),
[releases](https://github.com/Brandonr91/windows-tile-studio/releases),
[CI](https://github.com/Brandonr91/windows-tile-studio/actions), and
[beta.7 verification](RELEASE-BETA-7.md).

The public snapshot on October 30 showed zero stars and zero forks. Foundation
terms require verifiable reputation for executable programs; the project may
need more independent adoption before acceptance. Do not invent endorsements or
describe generated/download-verification traffic as external usage.

## Before submission

1. Review and publish the [Code signing policy](CODE-SIGNING-POLICY.md) with a
   README link. The prepared policy is published in the repository; existing releases remain unsigned.
2. Ensure GitHub MFA is enabled; enrollment in SignPath must also use MFA. This
   preparation has not inspected or changed account security settings.
3. Add a Code signing policy link to the public downloads/release page without
   claiming that the already-published beta.7 is signed or provider-approved.
4. Review all project and reputation statements above. Complete the form using
   your own contact details, and personally decide the required terms/privacy
   consent and separate optional marketing choice.
5. Submit the application. No submission is automated by this repository.

## After acceptance

Configure the approved project, trusted GitHub build system, artifact configuration,
human approval policy and certificate in SignPath. Grant the provider GitHub App
access only to this repository when required; review its permissions yourself.
Add its least-privilege submitter token directly to GitHub Actions Secrets as
SIGNPATH_API_TOKEN, never to source control or chat. Organization/project/policy
identifiers and the approved signer thumbprint belong in repository variables.

The inert template is [examples/signpath/signing-workflow.yml](../examples/signpath/signing-workflow.yml).
It is not loaded by GitHub Actions. After review/configuration, a first manual
signing test can upload the built EXE, await provider approval and verify the
returned signature. It does not publish releases or repack signed downloads.
Production integration must consume that verified signed EXE and regenerate
archive manifests/checksums after signing; Build-Release.ps1 currently rebuilds
an unsigned EXE and must not be reused unchanged as a signed-packaging step.

Sources: [application](https://signpath.org/apply), [Foundation terms](https://signpath.org/terms),
[GitHub integration](https://docs.signpath.io/trusted-build-systems/github).
