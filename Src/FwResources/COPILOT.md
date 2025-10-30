---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FwResources

## Purpose
Centralized resource management for FieldWorks applications.
Contains shared images, icons, localized strings, and other UI assets used throughout the application suite.
Provides consistent branding and enables efficient resource packaging and localization workflows.

## Key Components
### Key Classes
- **ResourceHelper**
- **FwFileExtensions**
- **SearchingAnimation**
- **ResourceHelperImpl**

## Technology Stack
- C# resource files (.resx)
- Image files and icons
- Localization infrastructure

## Dependencies
- Depends on: Minimal (provides resources to others)
- Used by: All FieldWorks applications and UI libraries

## Build Information
- C# resource library
- Build with MSBuild or Visual Studio
- Compiled into satellite assemblies for localization

## Entry Points
- Provides centralized resource access
- Used via resource managers throughout applications

## Related Folders
- **Common/** - UI infrastructure that uses FwResources
- **xWorks/** - Consumes shared resources for UI
- **LexText/** - Uses shared icons and strings
- **FwCoreDlgs/** - Dialogs that use shared resources
- **Transforms/** - May include XSLT resources

## Code Evidence
*Analysis based on scanning 5 source files*

- **Classes found**: 4 public classes
- **Namespaces**: SIL.FieldWorks.Resources

## Interfaces and Data Models

- **FwFileExtensions** (class)
  - Path: `FwFileExtensions.cs`
  - Public class implementation

- **Images** (class)
  - Path: `Images.Designer.cs`
  - Public class implementation

- **ResourceHelper** (class)
  - Path: `ResourceHelper.cs`
  - Public class implementation

- **FileFilterType** (enum)
  - Path: `ResourceHelper.cs`

## References

- **Project files**: FwResources.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, FwFileExtensions.cs, FwStrings.Designer.cs, FwTMStrings.Designer.cs, HelpTopicPaths.Designer.cs, Images.Designer.cs, ResourceHelper.cs, ResourceHelperImpl.Designer.cs, SearchingAnimation.cs, ToolBarSystemStrings.Designer.cs
- **Source file count**: 12 files
- **Data file count**: 7 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\FwResources\FwResources.csproj
- Key C# files:
  - Src\FwResources\AssemblyInfo.cs
  - Src\FwResources\FwFileExtensions.cs
  - Src\FwResources\FwStrings.Designer.cs
  - Src\FwResources\FwTMStrings.Designer.cs
  - Src\FwResources\HelpTopicPaths.Designer.cs
  - Src\FwResources\Images.Designer.cs
  - Src\FwResources\ResourceHelper.cs
  - Src\FwResources\ResourceHelperImpl.Designer.cs
  - Src\FwResources\ResourceHelperImpl.cs
  - Src\FwResources\SearchingAnimation.Designer.cs
  - Src\FwResources\SearchingAnimation.cs
  - Src\FwResources\ToolBarSystemStrings.Designer.cs
- Data contracts/transforms:
  - Src\FwResources\FwStrings.resx
  - Src\FwResources\FwTMStrings.resx
  - Src\FwResources\HelpTopicPaths.resx
  - Src\FwResources\Images.resx
  - Src\FwResources\ResourceHelperImpl.resx
  - Src\FwResources\SearchingAnimation.resx
  - Src\FwResources\ToolBarSystemStrings.resx
