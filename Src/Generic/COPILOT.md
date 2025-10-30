---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Generic

## Purpose
Generic utility components and low-level helper classes that don't fit into more
specific categories. Includes fundamental algorithms, data structures, and utility functions
used across the codebase. Provides building blocks for higher-level functionality.

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

## Architecture
C++ native library with 47 implementation files and 67 headers. Contains 1 subprojects: Generic.

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Threading model: UI thread marshaling, synchronization.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
Test projects: TestGeneric. 16 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Project files:
  - Src\Generic\Generic.vcxproj
  - Src\Generic\Test\TestGeneric.vcxproj
- Key C++ files:
  - Src\Generic\BinTree_i.cpp
  - Src\Generic\ComHashMap_i.cpp
  - Src\Generic\ComMultiMap_i.cpp
  - Src\Generic\ComVector.cpp
  - Src\Generic\DataStream.cpp
  - Src\Generic\Debug.cpp
  - Src\Generic\DecodeUtf8_i.c
  - Src\Generic\DllModul.cpp
  - Src\Generic\FileStrm.cpp
  - Src\Generic\FwSettings.cpp
  - Src\Generic\GenericFactory.cpp
  - Src\Generic\GpHashMap_i.cpp
  - Src\Generic\HashMap.cpp
  - Src\Generic\HashMap_i.cpp
  - Src\Generic\MakeDir.cpp
  - Src\Generic\ModuleEntry.cpp
  - Src\Generic\MultiMap_i.cpp
  - Src\Generic\OleStringLiteral.cpp
  - Src\Generic\ResourceStrm.cpp
  - Src\Generic\Set_i.cpp
  - Src\Generic\StackDumper.cpp
  - Src\Generic\StackDumperWin32.cpp
  - Src\Generic\StackDumperWin64.cpp
  - Src\Generic\StrUtil.cpp
  - Src\Generic\StringStrm.cpp
- Key headers:
  - Src\Generic\BinTree.h
  - Src\Generic\COMBase.h
  - Src\Generic\CSupportErrorInfo.h
  - Src\Generic\ComHashMap.h
  - Src\Generic\ComMultiMap.h
  - Src\Generic\ComSmartPtr.h
  - Src\Generic\ComSmartPtrImpl.h
  - Src\Generic\ComVector.h
  - Src\Generic\CompileStringTable.h
  - Src\Generic\DataReader.h
  - Src\Generic\DataStream.h
  - Src\Generic\DataWriter.h
  - Src\Generic\Database.h
  - Src\Generic\DispatchImpl.h
  - Src\Generic\FileStrm.h
  - Src\Generic\FwSettings.h
  - Src\Generic\GenSmartPtr.h
  - Src\Generic\GenericFactory.h
  - Src\Generic\GenericResource.h
  - Src\Generic\GpHashMap.h
  - Src\Generic\HashMap.h
  - Src\Generic\LinkedList.h
  - Src\Generic\MakeDir.h
  - Src\Generic\ModuleEntry.h
  - Src\Generic\MultiMap.h
