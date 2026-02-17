---
last-reviewed: 2025-10-31
last-reviewed-tree: f15feb0cd603130b05a5b4ed86279ca3efb5d796b8c7cedd6061230a7e245306
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Generic COPILOT summary

## Purpose
Generic low-level utility components and foundational helper classes for FieldWorks native C++ code. Provides COM smart pointers (ComSmartPtr), collection classes (ComHashMap, ComMultiMap, ComVector, BinTree), stream/IO utilities (DataStream, FileStrm, DataReader, DataWriter), COM infrastructure (DispatchImpl, CSupportErrorInfo, COMBase), string handling (Vector, StrAnsi, StrApp, StrUni), memory management (ModuleEntry), settings (FwSettings), and numerous other low-level helpers. Critical foundation for Kernel, views, and all native FieldWorks components. 44K+ lines of template-heavy C++ code.

## Architecture
C++ native library (header-only templates and implementation files). Heavy use of templates for generic collection classes and smart pointers. COM-centric design with IUnknown-based interfaces. Stream classes for binary data I/O. Vector and HashMap as foundational collections. Cross-platform considerations (Windows/Linux) via conditional compilation.

## Key Components
- **ComSmartPtr** (ComSmartPtr.h, 7K lines): COM smart pointer template
  - Automatic AddRef/Release for COM interface pointers
  - Template class for any COM interface
  - IntfNoRelease: Helper for avoiding AddRef/Release on borrowed pointers
- **ComHashMap** (ComHashMap.h/.cpp, 64K lines combined): Hash map collection
  - Template hash map for COM-compatible storage
  - Key-value pairs with hash-based lookup
- **ComMultiMap** (ComMultiMap.h/.cpp, 36K lines combined): Multi-value hash map
  - Hash map allowing multiple values per key
- **ComVector** (ComVector.h/.cpp, 31K lines combined): Dynamic array
  - Template vector/array class
  - COM-compatible dynamic array
- **BinTree** (BinTree.h/.cpp, 8.4K lines combined): Binary tree
  - Template binary tree data structure
- **DataStream** (DataStream.h/.cpp, 23K lines combined): Binary data streaming
  - Binary I/O stream abstraction
  - Read/write primitive types and structures
- **FileStrm** (FileStrm.h/.cpp, 30K lines combined): File stream
  - File-based stream implementation
  - Extends DataStream for file I/O
- **DataReader, DataWriter** (DataReader.h, DataWriter.h): Stream reader/writer interfaces
  - Interface abstractions for data I/O
- **DispatchImpl** (DispatchImpl.h, 6.5K lines): IDispatch implementation
  - Helper for implementing COM IDispatch (automation)
- **CSupportErrorInfo** (CSupportErrorInfo.h, 6K lines): COM error info support
  - ISupportErrorInfo implementation for rich COM errors
- **COMBase** (COMBase.h): COM base class utilities
- **FwSettings** (FwSettings.h/.cpp, 17K lines combined): Settings management
  - Read/write application settings (registry/config files)
- **Vector** (Vector.h/.cpp): STL-like vector
- **StrAnsi, StrApp, StrUni** (StrAnsi.h, StrApp.h, StrUni.h): String classes
  - ANSI, application, and Unicode string utilities
- **ModuleEntry** (ModuleEntry.h/.cpp): Module/DLL entry point helpers
- **Database** (Database.h): Database abstraction
- **DllModul** (DllModul.cpp): DLL module infrastructure
- **DecodeUtf8** (DecodeUtf8_i.c): UTF-8 decoding
- **Debug** (Debug.cpp): Debug utilities
- Many more utility headers and implementation files

## Technology Stack
- C++ native code

## Dependencies
- Upstream: COM, file I/O, registry
- Downstream: Core services built on Generic

## Interop & Contracts
- **IUnknown**: COM interface base

## Threading & Performance
- **COM threading**: Collections and smart pointers follow COM threading rules

## Config & Feature Flags
- **FwSettings**: Application settings management

## Build Information
- **No project file**: Header-only templates built into consuming projects

## Interfaces and Data Models
DataStream, DispatchImpl.

## Entry Points
Header files included by consuming projects. No standalone executable.

## Test Index
No dedicated test project for Generic. Tested via consuming components (Kernel, views, etc.).

## Usage Hints
- **ComSmartPtr**: Always use for COM interface pointers to avoid leaks

## Related Folders
- **Kernel/**: Core services using Generic

## References
See `.cache/copilot/diff-plan.json` for file details.
