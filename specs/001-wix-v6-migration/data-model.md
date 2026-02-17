# Installer Data Model

## Features

The installer exposes the following features to the user:

- **FieldWorks** (Root)
    - **Core** (Hidden, Mandatory): Main application binaries.
    - **Writing Systems** (Hidden, Mandatory): Icu, Graphite, etc.
    - **Keyman** (Optional): Keyman keyboard integration.
    - **Movies** (Optional): Sample movies.
    - **Samples** (Optional): Sample data.
    - **Localization** (Optional):
        - **French**
        - **Spanish**
        - **Portuguese**
        - ... (others as defined in `CustomFeatures.wxi`)

## Directory Structure

The installer manages two primary directories:

1.  **Application Directory** (`WIXUI_INSTALLDIR`):
    - Default: `%ProgramFiles%\SIL\FieldWorks`
    - Contains: Binaries, DLLs, Resources.

2.  **Project Data Directory** (`WIXUI_PROJECTSDIR`):
    - Default: `%ProgramData%\SIL\FieldWorks\Projects` (or similar, need to verify)
    - Contains: User project data, shared resources.

## Components

Key components include:

- **FieldWorks.exe**: Main entry point.
- **FwData.dll**: Core data access.
- **Icu.dll**: ICU support.
- **Redistributables**:
    - .NET Framework 4.8
    - VC++ Redistributable (x64/x86)
    - WebView2 Runtime (if applicable)

## Registry Keys

- `HKLM\Software\SIL\FieldWorks`: Installation paths, version info.
- `HKCU\Software\SIL\FieldWorks`: User preferences.
