---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText/FlexPathwayPlugin

## Purpose
Pathway publishing integration plugin for FLEx. Enables export and publishing of lexicon and text data using SIL's Pathway publishing system.

## Key Components
- **FlexPathwayPlugin.csproj** - Main plugin library
- **FlexPathwayPlugin.cs** - Plugin implementation
- **myFolders.cs** - Folder management for publishing
- **FlexPathwayPluginTests/** - Plugin test suite

## Technology Stack
- C# .NET
- Pathway publishing API integration
- Plugin architecture

## Dependencies
- Depends on: Pathway SDK, LexText core, Cellar (data model)
- Used by: LexText export and publishing features

## Build Information
- C# class library plugin project
- Build via: `dotnet build FlexPathwayPlugin.csproj`
- Includes test suite

## Testing
- Run tests: `dotnet test FlexPathwayPlugin/FlexPathwayPluginTests/`
- Tests cover plugin integration and export

## Entry Points
- Plugin interface for Pathway integration
- Export and publishing workflows

## Related Folders
- **LexText/LexTextDll/** - Core LexText functionality
- **Transforms/** - XSLT transforms used for export
- **FXT/** - Transform tools for data conversion
