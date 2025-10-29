# LCMBrowser

## Purpose
LCM/Cellar model browser tooling. Provides a development and diagnostic tool for exploring the FieldWorks Language and Culture Model (LCM), viewing data structures, and debugging data model issues.

## Key Components
- **LCMBrowser.csproj** - Model browser application

## Technology Stack
- C# .NET WinForms
- Data model visualization
- Reflection and introspection

## Dependencies
- Depends on: Cellar (LCM data model), Common (UI infrastructure)
- Used by: Developers for model exploration and debugging

## Build Information
- C# WinForms application
- Development/diagnostic tool
- Build with MSBuild or Visual Studio

## Entry Points
- Standalone application for browsing LCM data
- Launch to explore FieldWorks data structures

## Related Folders
- **Cellar/** - Core LCM data model that LCMBrowser visualizes
- **DbExtend/** - Schema extensions browsed by LCMBrowser
- **FdoUi/** - Data object UI components (related visualization)
