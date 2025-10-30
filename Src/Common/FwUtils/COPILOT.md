---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common/FwUtils

## Purpose
General FieldWorks utilities library. Provides a wide range of utility functions, helpers, and extension methods used throughout the FieldWorks codebase.

## Key Components
- **FwUtils.csproj** - Main utilities library
- **AccessibleNameCreator.cs** - Accessibility support
- **AlphaOutline.cs** - Outline numbering utilities
- **Benchmark.cs** - Performance measurement
- **CachePair.cs** - Caching utilities
- **CharacterCategorizer.cs** - Unicode character categorization
- **ClipboardUtils.cs** - Clipboard operations
- **ComponentsExtensionMethods.cs** - Extension methods for common types
- Many more utility classes and helpers


## Key Classes/Interfaces
- **ManagedPictureFactory**
- **IFwRegistryHelper**
- **SaveFile**
- **InstallationException**
- **FlexHelpProvider**
- **Benchmark**
- **TimeRecorder**
- **ConsoleProgress**

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


## References
- **Project Files**: FwUtils.csproj
- **Key C# Files**: AccessibleNameCreator.cs, ActivationContextHelper.cs, AlphaOutline.cs, Benchmark.cs, CachePair.cs, CharEnumeratorForByteArray.cs, CharacterCategorizer.cs, ClipboardUtils.cs
