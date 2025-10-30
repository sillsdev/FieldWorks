---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FwUtils

## Purpose
General FieldWorks utilities library. Provides a wide range of utility functions, helpers, and extension methods used throughout the FieldWorks codebase.

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
