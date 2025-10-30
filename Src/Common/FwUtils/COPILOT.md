---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FwUtils

## Purpose
General FieldWorks utilities library containing wide-ranging helper functions.
Provides utilities for image handling (ManagedPictureFactory), registry access (IFwRegistryHelper),
XML serialization (XmlSerializationHelper), audio conversion (WavConverter), exception handling,
and numerous other cross-cutting concerns. Most comprehensive utility collection in FieldWorks.

## Key Components
### Key Classes
- **ManagedPictureFactory**
- **XmlSerializationHelper**
- **WavConverter**
- **InstallationException**
- **FlexHelpProvider**
- **Benchmark**
- **TimeRecorder**
- **ComponentsExtensionMethods**
- **ConsoleProgress**
- **MessageBoxUtils**

### Key Interfaces
- **IFwRegistryHelper**
- **IMessageBox**
- **IClipboard**
- **IChecksDataSource**
- **IProjectSpecificSettingsKeyProvider**
- **IFocusablePanePortion**
- **IHelpTopicProvider**
- **ITextToken**

## Technology Stack
- C# .NET
- Extension methods pattern
- Utility and helper patterns

## Dependencies
- Depends on: Minimal (mostly .NET framework)
- Used by: All Common subprojects and FieldWorks applications

## Build Information
- C# class library project
- Build via: `dotnet build FwUtils.csproj`
- Foundation library for Common

## Entry Points
- Static utility methods and extension methods
- Helper classes for common operations
- Infrastructure support utilities

## Related Folders
- **Common/Framework/** - Uses FwUtils extensively
- **Common/Filters/** - Uses utility functions
- **Common/FieldWorks/** - FieldWorks-specific utilities building on FwUtils
- Used by virtually all FieldWorks components

## Code Evidence
*Analysis based on scanning 121 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 11 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.FwUtils, SIL.FieldWorks.Common.FwUtils.Attributes, SIL.FieldWorks.Common.FwUtils.Pathway, SIL.FieldWorks.Common.FwUtils.Properties, SIL.FieldWorks.FwCoreDlgs

## Interfaces and Data Models

- **IChecksDataSource** (interface)
  - Path: `IChecksDataSource.cs`
  - Public interface definition

- **IClipboard** (interface)
  - Path: `ClipboardUtils.cs`
  - Public interface definition

- **IFwRegistryHelper** (interface)
  - Path: `IFwRegistryHelper.cs`
  - Public interface definition

- **IMessageBox** (interface)
  - Path: `MessageBoxUtils.cs`
  - Public interface definition

- **AlphaOutline** (class)
  - Path: `AlphaOutline.cs`
  - Public class implementation

- **Benchmark** (class)
  - Path: `Benchmark.cs`
  - Public class implementation

- **ComponentsExtensionMethods** (class)
  - Path: `ComponentsExtensionMethods.cs`
  - Public class implementation

- **ConsoleProgress** (class)
  - Path: `ConsoleProgress.cs`
  - Public class implementation

- **FlexHelpProvider** (class)
  - Path: `FlexHelpProvider.cs`
  - Public class implementation

- **FwAppArgs** (class)
  - Path: `FwLinkArgs.cs`
  - Public class implementation

- **FwLinkArgs** (class)
  - Path: `FwLinkArgs.cs`
  - Public class implementation

- **InstallationException** (class)
  - Path: `InstallationException.cs`
  - Public class implementation

- **ManagedPictureFactory** (class)
  - Path: `ManagedPictureFactory.cs`
  - Public class implementation

- **Manager** (class)
  - Path: `MessageBoxUtils.cs`
  - Public class implementation

- **MeasurementUtils** (class)
  - Path: `MeasurementUtils.cs`
  - Public class implementation

- **MessageBoxUtils** (class)
  - Path: `MessageBoxUtils.cs`
  - Public class implementation

- **Property** (class)
  - Path: `Property.cs`
  - Public class implementation

- **StartupException** (class)
  - Path: `StartupException.cs`
  - Public class implementation

- **StyleMarkup** (class)
  - Path: `StyleMarkupInfo.cs`
  - Public class implementation

- **StyleMarkupInfo** (class)
  - Path: `StyleMarkupInfo.cs`
  - Public class implementation

- **ThreadHelper** (class)
  - Path: `ThreadHelper.cs`
  - Public class implementation

- **TimeRecorder** (class)
  - Path: `Benchmark.cs`
  - Public class implementation

- **WavConverter** (class)
  - Path: `WavConverter.cs`
  - Public class implementation

- **XmlSerializationHelper** (class)
  - Path: `XmlSerializationHelper.cs`
  - Public class implementation

- **ImportExportStep** (enum)
  - Path: `ImportExportStep.cs`

- **MsrSysType** (enum)
  - Path: `MeasurementUtils.cs`

- **SaveFile** (enum)
  - Path: `WavConverter.cs`

- **StyleTypes** (enum)
  - Path: `StyleMarkupInfo.cs`

- **Typ** (enum)
  - Path: `FwUpdater.cs`

- **UseTypes** (enum)
  - Path: `StyleMarkupInfo.cs`

## References

- **Project files**: FwUtils.csproj, FwUtilsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: Benchmark.cs, ComponentsExtensionMethods.cs, ConsoleProgress.cs, FlexHelpProvider.cs, IFwRegistryHelper.cs, ImportExportStep.cs, InstallationException.cs, ManagedPictureFactory.cs, WavConverter.cs, XmlSerializationHelper.cs
- **XML data/config**: strings-en.xml, strings-en.xml
- **Source file count**: 126 files
- **Data file count**: 6 files

## Architecture
C# library with 126 source files. Contains 1 subprojects: FwUtils.

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
Config files: app.config.

## Test Index
Test projects: FwUtilsTests. 27 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.
