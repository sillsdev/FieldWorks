# FwResources

## Purpose
Shared resources (images, strings, assets) for FieldWorks applications and libraries. Centralizes resource management to ensure consistent UI appearance and localization across all applications.

## Key Components
- **FwResources.csproj** - Resource library project
- Images, icons, and graphical assets
- Localized strings and translations
- Shared UI assets

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
