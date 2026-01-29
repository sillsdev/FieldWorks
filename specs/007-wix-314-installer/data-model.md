# Data Model: WiX 3.14 Installer Upgrade

**Feature**: 007-wix-314-installer | **Date**: December 2, 2025

## Overview

This feature does not introduce or modify any data models. It is a CI infrastructure and documentation update.

## Entities

N/A - No application entities are affected.

## Installer Artifacts (Reference Only)

The following artifacts are produced by the installer build process (unchanged by this feature):

| Artifact | Description | Build Target |
|----------|-------------|--------------|
| `FieldWorks_*_Offline_x64.exe` | Full offline installer bundle | `BuildBaseInstaller` |
| `FieldWorks_*_Online_x64.exe` | Online installer (downloads prerequisites) | `BuildBaseInstaller` |
| `FieldWorks_*.msp` | Patch installer (incremental update) | `BuildPatchInstaller` |
| `BuildDir.zip` | Build artifacts for patch generation | `BuildBaseInstaller` (release) |
| `ProcRunner.zip` | Patch runner utilities | `BuildBaseInstaller` (release) |

## Configuration Files (Reference Only)

| File | Purpose |
|------|---------|
| `FLExInstaller/Overrides.wxi` | Version and product configuration |
| `FLExInstaller/CustomComponents.wxi` | Component definitions |
| `FLExInstaller/CustomFeatures.wxi` | Feature tree definition |
| `FLExInstaller/Redistributables.wxi` | Prerequisites and dependencies |

These files are not modified by this feature.
