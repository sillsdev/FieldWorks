---
name: update-patch-base
description: >
  Retarget the FieldWorks patch installer workflow to a new base build.
  Use whenever the user asks to update, retarget, or bump the patch base,
  base release, or base build number (e.g. "retarget patching to base 1440",
  "build patches on top of build-1442", "update the patch installer base"),
  or when a new base installer release has been published and patches must
  be rebuilt on top of it.
version: "1.0.0"
---

# Update Patch Installer Base Build

The Patch Installer workflow (`.github/workflows/patch-installer-cd.yml`)
builds a FieldWorks `.msp` patch on top of artifacts from a previously
published base build. The base is identified by a GitHub release tag
(`build-NNNN`) on `sillsdev/FieldWorks`. When a new base installer ships,
the workflow defaults must be retargeted so scheduled and push-triggered
patch builds pick up the new base.

## Why two separate values exist

`base_release` (the release tag) and `base_build_number` (the bare number)
are independent inputs only to support bootstrapping patches on bases that
were built by the old Jenkins system. After FieldWorks 9.3 is the stable
release, the release tag can be computed from the number and the inputs
can be consolidated — see the comment block above `base_release` in the
workflow.

`FW_BUILD_NUMBER` for a patch must always be **higher** than its base
build number, or Windows Installer will not treat the patch as an upgrade.
The workflow computes this from `GITHUB_RUN_NUMBER` plus an offset; you do
not normally need to touch it, but keep the invariant in mind if the base
number ever jumps dramatically.

## Procedure

1. **Verify the new base release exists and is complete** before changing
   anything. The workflow downloads two assets from the release:

   ```powershell
   gh release view build-NNNN --repo sillsdev/FieldWorks --json tagName,assets --jq '{tag: .tagName, assets: [.assets[].name]}'
   ```

   Both `BuildDir.zip` and `ProcRunner.zip` must be present. If either is
   missing, stop and report — retargeting to an incomplete release breaks
   every subsequent patch build.

2. **Update all four references** in
   `.github/workflows/patch-installer-cd.yml`. Searching the file for the
   old number finds them all:

   - `base_release` input `default:` — `'build-NNNN'`
   - `base_build_number` input `default:` — `'NNNN'`
   - `BASE_BUILD_NUMBER` env fallback — `${{ inputs.base_build_number || 'NNNN' }}`
   - artifact download fallback in the *Import Base Build Artifacts* step —
   `${{ inputs.base_release || 'build-NNNN' }}`

   The fallbacks matter as much as the input defaults: scheduled (cron)
   and push-triggered runs have no inputs, so they use the `||` fallbacks
   exclusively.

3. **Confirm nothing else references the old number.** Search the repo for
   the bare number; ignore coincidental matches inside GUIDs in
   `TestLangProj/*.fwdata`. Only the workflow file should need changes.

4. **Commit and open a PR.** Use a subject like
   `Retarget patch installer to base build NNNN`, note in the body that
   the release was verified to contain both artifacts, and run
   `git diff --check --cached` before committing. This is a workflow-only
   change — no local build or test run is required; the Patch Installer
   workflow itself is the validation.

## Validation after merge

The next scheduled or push-triggered Patch Installer run should show the
*Import Base Build Artifacts* step downloading from the new release. A
manual `workflow_dispatch` with `make_release: false` is the safe way to
smoke-test without publishing to S3.
