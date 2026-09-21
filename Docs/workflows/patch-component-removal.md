# Removing a file from a patch line

A patch must keep every component that the base **or any patch on that base** has
shipped. If a file drops out of the build output while a base is live, the next patch
installs nothing, returns success, and advances the version users see
([LT-22801](https://jira.sil.org/browse/LT-22801)). This page explains why, what CI
checks, and what to do when a check fails.

## Why a dropped file breaks patching

Every FieldWorks patch is a diff against the base, and each new patch supersedes the
previous one. Windows Installer therefore sees only *base + newest patch*. It still
records every component an earlier patch installed. When one of those components is
missing from the newest patch, it logs:

```
SELMGR: ComponentId '{...}' is registered to feature 'Complete', but is not present
in the Component table.  Removal of components from a feature is not supported!
```

It then treats the feature as advertised rather than installed. Reinstalling an
advertised feature copies no files, so the patch exits 0 and changes nothing on disk.
Microsoft documents removing a component from a feature as something only a new
ProductCode (a new base) may do.

Component identity comes from the file's install path, so **moving or renaming a file
counts as removing it**.

## What CI checks

`patch-installer-cd.yml` runs two checks before it signs or publishes a patch.

- **Component ledger** (`scripts/Installer/Test-PatchComponentLedger.ps1`). Compares the
  new patch against every component earlier patches on the base have added: the
  committed seed in `FLExInstaller/PatchComponentLedger/b<base>.tsv` plus the
  `*_components.tsv` ledger published next to each `.msp`. `pyro` already rejects
  dropping a file the base itself ships (`PYRO0305`), so this check covers the
  patch-added files `pyro` cannot see.
- **Install test** (`scripts/Installer/Test-PatchInstall.ps1`). Installs the published
  base, applies the latest published patch, then applies the new patch with
  `MSIENFORCEUPGRADECOMPONENTRULES=1`, which turns the silent failure into error 2771.
  It also checks that `FieldWorks.exe` on disk carries the new patch's version.

Both name the dropped file:

```
Patch 9.3.12.2754 drops component {B3A225EB-3642-5FE4-8ED4-B2DAE6F8AC9B}
  file:    Avalonia.Themes.Fluent.dll   (feature Complete)
  shipped: patches 9.3.12.2718 to 9.3.12.2718 on base 1452
Fix: restore the file to the output, or add a component stand-in (see
     Docs/workflows/patch-component-removal.md), as well as an issue to remove
     the file before cutting the next base build.
```

## When a check fails

Do both of the following.

### 1. Keep the component in the patch

**Restore the file (the default).** Add it to `RemovedSinceLastBase` in the
`RescuePatching` target of `Build/Installer.legacy.targets`, at the **same path** it
shipped from:

```xml
<RemovedSinceLastBase Include="$(dir-outputBase)/Avalonia.Themes.Fluent.dll" />
```

The target writes a zero-byte placeholder there, so the component stays in every patch.
Machines that already have the real file keep it, because Windows Installer does not
replace a versioned file with an unversioned one. The dead file costs a few bytes until
the next base.

**Component stand-in (only when the old file must not stay on disk).** Use this when
leaving the file behind is unsafe, for example a plugin that FieldWorks finds by
scanning a directory. Keep the placeholder above, and author a `RemoveFile` entry for
that path in `FLExInstaller/CustomComponents.wxi` so the real file is deleted. This
has not yet been validated on a patch line. Test it by installing the base, the
previous patch and the new patch before relying on it. Cutting a new base is the safe
alternative.

### 2. File an issue to remove the file before the next base

Create an LT issue with the label `remove-before-next-base`:

```
Summary: Remove <file> placeholder before the next base build
Labels:  remove-before-next-base

<file> (component <GUID>, feature <feature>) left the build output in <commit>,
after patches <first> to <last> on base <base> shipped it. It is kept alive by a
RemovedSinceLastBase placeholder in Build/Installer.legacy.targets so patches on
base <base> keep working.

Before the next base build:
- Remove the RemovedSinceLastBase entry.
- If any published patch on base <base> already dropped the component, machines
  that applied it keep an orphaned copy that an upgrade will not remove; add a
  RemoveFile for its path to the new base.
```

## Before cutting a new base

A new base is the one point where dropping files is legal: it has a new ProductCode,
and it removes the old product completely before installing (`RemoveExistingProducts`
runs before `InstallInitialize`).

1. Resolve every open issue labelled `remove-before-next-base`. The base build warns
   while `RemovedSinceLastBase` still has entries.
2. Add a `RemoveFile` for every file an already-published patch orphaned. An upgrade
   only removes what the old product's current patch still lists.
3. A new base needs no seed ledger: its patches publish their own ledgers from the
   first patch on.

## Running the checks locally

```powershell
.\scripts\Installer\Test-PatchComponentLedger.ps1 -MasterMsi <base.msi> -UpdateMsi <update.msi> `
    -BaseBuildNumber 1452 -PatchVersion 9.3.12.2761 `
    -SeedLedger FLExInstaller\PatchComponentLedger\b1452.tsv -OutLedger out.tsv
```

The install test changes the machine's FieldWorks installation; run it only on a
disposable machine, from an elevated prompt.
