# Removing a file from a patch line

A patch must keep every file-backed component under MSI APPFOLDER (application output
files) shipped by the base or by the immediately previous published patch. Windows
Installer sees the base and newest patch together, so those file-backed components must
remain in the new patch.

## What CI checks

`patch-installer-cd.yml` runs the file-backed component ledger check before signing or publishing.
The check compares the new Update MSI with the union of:

- the file-backed components under MSI APPFOLDER in the Master/base MSI; and
- the complete file-backed update-minus-base ledger published beside the immediately previous MSP.

The first published patch on a patch line creates the initial S3 ledger. After a
ledger-bearing patch exists, the immediately previous MSP must have its matching
`*_components.tsv` file. Version filtering keeps a release branch from consuming a ledger
for a later patch. Ledgers are release artifacts in S3 and are not stored in this repository.

Each successful patch writes its complete update-minus-base file-backed component set under MSI APPFOLDER to a ledger beside
the MSP. A later patch uses the ledger beside its immediately previous MSP.

## When the check fails

The diagnostic names every missing file-backed component and file, regardless of whether it came from
the base MSI or the previous-patch ledger:

```
Patch 9.3.12.2761 drops file-backed component {B3A225EB-3642-5FE4-8ED4-B2DAE6F8AC9B}
  file:    Avalonia.Themes.Fluent.dll   (feature Complete)
  base:    1452
Remediation:
Add each missing file's output path to RemovedSinceLastBase in Build/Installer.legacy.targets,
preserving its relative output path:
  <RemovedSinceLastBase Include="$(dir-outputBase)/Avalonia.Themes.Fluent.dll" />
Create an issue to remove the placeholder before the next base build.
```

The `RemovedSinceLastBase` entry makes the build write a zero-byte stand-in at that path.
That file-backed component remains in the patch, so machines that already have the real file keep
working while the removal issue is completed.

## Before publishing a base

A scheduled base verification build warns while any `RemovedSinceLastBase` entries remain.
A base release build fails. Both messages list the stand-in paths and require two cleanup
actions:

1. Remove each `RemovedSinceLastBase` entry from `Build/Installer.legacy.targets`.
2. Remove each corresponding zero-byte file from the build output.

Complete the removal issue before creating the base. A new base establishes the file-backed component
set under MSI APPFOLDER that future patches must preserve.

## Running the check locally

```powershell
.\scripts\Installer\Test-PatchComponentLedger.ps1 -MasterMsi <base.msi> -UpdateMsi <update.msi> `
    -BaseBuildNumber 1452 -PatchVersion 9.3.12.2761 `
    -OutLedger out.tsv
```
