---
last-reviewed: 2025-10-31
last-reviewed-tree: 94d512906652acdf5115402f933d29a3f5eb2c6cdf0779b74e815687fc5c1569
status: draft
---

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
- COM (Component Object Model) infrastructure
- Template metaprogramming (extensive use)
- Windows API (primary platform)
- Cross-platform support (conditional compilation)

## Dependencies

### Upstream (consumes)
- **Windows APIs**: COM, file I/O, registry
- **Standard C++ library**: Basic types, string
- Minimal external dependencies (self-contained low-level library)

### Downstream (consumed by)
- **Kernel/**: Core services built on Generic
- **views/**: Native rendering engine using collections and smart pointers
- **All FieldWorks native C++ components**: Universal dependency

## Interop & Contracts
- **IUnknown**: COM interface base
- **IDispatch**: Automation interface support
- **ISupportErrorInfo**: Rich error information
- COM ABI compatibility (binary interface standard)

## Threading & Performance
- **COM threading**: Collections and smart pointers follow COM threading rules
- **Reference counting**: ComSmartPtr ensures proper COM lifetime management
- **Template overhead**: Compile-time; runtime efficient
- **Performance**: Low-level optimized collections

## Config & Feature Flags
- **FwSettings**: Application settings management
- Conditional compilation for platform differences (#ifdef WIN32, etc.)

## Build Information
- **No project file**: Header-only templates built into consuming projects
- **Compiled components**: Some .cpp files compiled into libraries
- **Build**: Included via consuming projects' build systems
- **Headers**: Included by Kernel, views, and other native components

## Interfaces and Data Models

- **ComSmartPtr<T>** (ComSmartPtr.h)
  - Purpose: Automatic COM interface pointer lifetime management
  - Inputs: Interface pointer (any COM interface)
  - Outputs: Smart pointer with automatic AddRef/Release
  - Notes: Template class; use ComSmartPtr<IFoo> fooPtr;

- **ComHashMap<K,V>** (ComHashMap.h)
  - Purpose: Hash map collection for COM environments
  - Inputs: Key type K, Value type V
  - Outputs: Hash-based key-value storage
  - Notes: Template class; efficient lookup

- **ComVector<T>** (ComVector.h)
  - Purpose: Dynamic array for COM-compatible objects
  - Inputs: Element type T
  - Outputs: Resizable array
  - Notes: Template class; like std::vector

- **DataStream** (DataStream.h)
  - Purpose: Abstract binary I/O stream
  - Inputs: Binary data
  - Outputs: Serialized/deserialized data
  - Notes: Base class for FileStrm and other streams

- **DispatchImpl** (DispatchImpl.h)
  - Purpose: Helper for implementing IDispatch
  - Inputs: Type info, method descriptors
  - Outputs: Working IDispatch implementation
  - Notes: Simplifies COM automation

## Entry Points
Header files included by consuming projects. No standalone executable.

## Test Index
No dedicated test project for Generic. Tested via consuming components (Kernel, views, etc.).

## Usage Hints
- **ComSmartPtr**: Always use for COM interface pointers to avoid leaks
- **ComHashMap/ComVector**: Use instead of STL in COM contexts for compatibility
- **DataStream**: Use for binary serialization/deserialization
- **FwSettings**: Centralized settings access
- Template-heavy: Long compile times but efficient runtime
- Header-only templates: Include appropriate headers in consuming projects

## Related Folders
- **Kernel/**: Core services using Generic
- **views/**: Rendering engine using Generic collections
- **DebugProcs/**: Debug utilities complement Generic

## References
- **Key headers**: ComSmartPtr.h (7K), ComHashMap.h (14K), ComMultiMap.h (9K), ComVector.h (21K), DataStream.h (5K), FileStrm.h (3K), DispatchImpl.h (6.5K), FwSettings.h (3K), and many more
- **Implementation files**: ComHashMap_i.cpp (50K), ComMultiMap_i.cpp (27K), ComVector.cpp (11K), DataStream.cpp (18K), FileStrm.cpp (27K), FwSettings.cpp (14.5K), and others
- **Total files**: 112 C++/H files
- **Total lines of code**: 44373
- **Output**: Compiled into consuming libraries/DLLs
- **Platform**: Primarily Windows (COM-centric), some cross-platform support