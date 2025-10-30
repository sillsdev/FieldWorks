---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LCMBrowser

## Purpose
Development tool for exploring and debugging the FieldWorks data model (LCM). 
Provides a browser interface for navigating object relationships, inspecting properties, 
and understanding the structure of linguistic databases. Primarily used during development 
and troubleshooting rather than in production scenarios.

## Key Components
### Key Classes
- **RealListChooser**
- **LCMInspectorList**
- **TsStringRunInfo**
- **TextProps**
- **TextStrPropInfo**
- **TextIntPropInfo**
- **LCMBrowserForm**
- **ClassPropertySelector**
- **BrowserProjectId**
- **ModelWnd**

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

## Code Evidence
*Analysis based on scanning 9 source files*

- **Classes found**: 14 public classes
- **Namespaces**: LCMBrowser

## Interfaces and Data Models

- **BrowserProjectId** (class)
  - Path: `BrowserProjectId.cs`
  - Public class implementation

- **CustomFields** (class)
  - Path: `CustomFields.cs`
  - Public class implementation

- **LCMClass** (class)
  - Path: `LCMClassList.cs`
  - Public class implementation

- **LCMClassList** (class)
  - Path: `LCMClassList.cs`
  - Public class implementation

- **LCMClassProperty** (class)
  - Path: `LCMClassList.cs`
  - Public class implementation

- **LCMInspectorList** (class)
  - Path: `LCMInspectorList.cs`
  - Public class implementation

- **TextIntPropInfo** (class)
  - Path: `LCMInspectorList.cs`
  - Public class implementation

- **TextProps** (class)
  - Path: `LCMInspectorList.cs`
  - Public class implementation

- **TextStrPropInfo** (class)
  - Path: `LCMInspectorList.cs`
  - Public class implementation

- **TsStringRunInfo** (class)
  - Path: `LCMInspectorList.cs`
  - Public class implementation

## References

- **Project files**: LCMBrowser.csproj
- **Target frameworks**: net462
- **Key C# files**: BrowserProjectId.cs, ClassPropertySelector.Designer.cs, ClassPropertySelector.cs, CustomFields.cs, LCMBrowserForm.cs, LCMClassList.cs, LCMInspectorList.cs, ModelWnd.Designer.cs, ModelWnd.cs, RealListChooser.cs
- **Source file count**: 16 files
- **Data file count**: 6 files
