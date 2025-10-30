---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FwResources

## Purpose
Shared resources (images, strings, assets) for FieldWorks applications and libraries. Centralizes resource management to ensure consistent UI appearance and localization across all applications.

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
