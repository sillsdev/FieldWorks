---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Generic

## Purpose
Generic/shared components that don't fit a single application. Provides low-level utility classes, algorithms, and helper functions used throughout the FieldWorks codebase.

## Key Components
### Key Classes
- **SilComTypeInfoHolder**
- **SilDispatchImpl**
- **CSupportErrorInfo**
- **CSupportErrorInfo2**
- **CSupportErrorInfo3**
- **CSupportErrorInfoN**
- **Point**
- **Rect**
- **UnicodeString8**
- **Mutex**

## Technology Stack
- C++ native code
- Generic algorithms and data structures
- Cross-cutting utilities

## Dependencies
- Depends on: Kernel (low-level services)
- Used by: Almost all FieldWorks components (AppCore, Common, views, etc.)

## Build Information
- Native C++ project (vcxproj)
- Includes comprehensive test suite
- Build with Visual Studio or MSBuild

## Entry Points
- Provides utility classes and functions
- Header-only or library components used throughout codebase

## Related Folders
- **Kernel/** - Lower-level infrastructure that Generic builds upon
- **AppCore/** - Application-level code that uses Generic utilities
- **Common/** - Managed code that interfaces with Generic components
- **views/** - Native views that use Generic utilities

## Code Evidence
*Analysis based on scanning 114 source files*

- **Classes found**: 20 public classes

## Interfaces and Data Models

- **BaseOfComVector** (class)
  - Path: `ComVector.h`
  - Public class implementation

- **CSupportErrorInfo** (class)
  - Path: `CSupportErrorInfo.h`
  - Public class implementation

- **CSupportErrorInfo2** (class)
  - Path: `CSupportErrorInfo.h`
  - Public class implementation

- **CSupportErrorInfo3** (class)
  - Path: `CSupportErrorInfo.h`
  - Public class implementation

- **CSupportErrorInfoN** (class)
  - Path: `CSupportErrorInfo.h`
  - Public class implementation

- **ComBool** (class)
  - Path: `UtilCom.h`
  - Public class implementation

- **DataReader** (class)
  - Path: `DataReader.h`
  - Public class implementation

- **DataReaderRgb** (class)
  - Path: `DataReader.h`
  - Public class implementation

- **DataReaderStrm** (class)
  - Path: `DataReader.h`
  - Public class implementation

- **Mutex** (class)
  - Path: `Mutex.h`
  - Public class implementation

- **MutexLock** (class)
  - Path: `Mutex.h`
  - Public class implementation

- **Point** (class)
  - Path: `UtilRect.h`
  - Public class implementation

- **Rect** (class)
  - Path: `UtilRect.h`
  - Public class implementation

- **SilComTypeInfoHolder** (class)
  - Path: `DispatchImpl.h`
  - Public class implementation

- **SilDispatchImpl** (class)
  - Path: `DispatchImpl.h`
  - Public class implementation

- **StrAnsiStream** (class)
  - Path: `StringStrm.h`
  - Public class implementation

- **StrEnumFORMATETC** (class)
  - Path: `UtilCom.h`
  - Public class implementation

- **StringDataObject** (class)
  - Path: `UtilCom.h`
  - Public class implementation

- **UnicodeString8** (class)
  - Path: `UnicodeString8.h`
  - Public class implementation

- **_Lock_Unknown** (class)
  - Path: `UtilCom.h`
  - Public class implementation

## References

- **Project files**: Generic.vcxproj, TestGeneric.vcxproj
- **Key C++ files**: DataStream.cpp, FileStrm.cpp, GpHashMap_i.cpp, ModuleEntry.cpp, ResourceStrm.cpp, StackDumperWin32.cpp, StringTable.cpp, UtilSil.cpp, UtilXml.cpp, Vector_i.cpp
- **Key headers**: CSupportErrorInfo.h, ComVector.h, DataReader.h, DispatchImpl.h, LinkedList.h, Mutex.h, UnicodeString8.h, UtilCom.h, UtilPersist.h, UtilRect.h
- **Source file count**: 114 files
- **Data file count**: 0 files
